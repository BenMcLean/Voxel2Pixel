using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace BenProgress;

public sealed class ProgressTaskGroup<T>(
	CancellationToken? cancellationToken = null,
	IProgress<Progress> progress = null) : IDisposable
{
	private readonly CancellationTokenSource _groupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken ?? CancellationToken.None);
	private readonly ConcurrentQueue<(Task<T> Task, int Index)> _pendingResults = [];
	private readonly Dictionary<int, double> _taskProgress = [];
	private readonly object _stateLock = new(); // Single lock for all state changes
	private readonly SemaphoreSlim _getResultsLock = new(1);
	private int _nextTaskIndex, _completedTaskCount;
	private bool _isDisposed;
	private Exception _firstError;
	private double _lastReportedProgress;
	private volatile TaskGroupState _currentState = TaskGroupState.Ready;
	public enum TaskGroupState
	{
		/// <summary>
		/// Initial state, no tasks added yet
		/// </summary>
		Ready,
		/// <summary>
		/// Has active tasks
		/// </summary>
		Running,
		/// <summary>
		/// All tasks completed successfully
		/// </summary>
		Completed,
		/// <summary>
		/// At least one task failed. This is a terminal state.
		/// </summary>
		Faulted,
		/// <summary>
		/// Group has been disposed. This is a terminal state.
		/// </summary>
		Disposed,
	}
	public bool HasPendingResults => !_pendingResults.IsEmpty;
	public TaskGroupState State
	{
		get
		{
			lock (_stateLock)
			{
				return _currentState;
			}
		}
	}
	public Exception Error
	{
		get
		{
			lock (_stateLock)
			{
				return _firstError;
			}
		}
	}
	public double CurrentProgress
	{
		get
		{
			lock (_stateLock)
			{
				return _lastReportedProgress;
			}
		}
	}
	public int TotalTaskCount => Interlocked.CompareExchange(ref _nextTaskIndex, 0, 0);
	public int CompletedTaskCount => Interlocked.CompareExchange(ref _completedTaskCount, 0, 0);
	public bool IsCancellationRequested => _groupCancellation.Token.IsCancellationRequested;
	/// <summary>
	/// Attempts to add tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(params IProgressTask<T>[] tasks) => TryAdd(tasks.AsEnumerable());
	/// <summary>
	/// Attempts to add tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(IEnumerable<IProgressTask<T>> tasks)
	{
		if (tasks is null)
			throw new ArgumentNullException(nameof(tasks));
		lock (_stateLock)
		{
			if (_isDisposed || _firstError is not null || _currentState is TaskGroupState.Faulted or TaskGroupState.Disposed)
				return false;
			foreach (IProgressTask<T> task in tasks)
			{
				if (task is null)
					throw new ArgumentNullException(nameof(tasks));
				int index = Interlocked.Increment(ref _nextTaskIndex) - 1;
				_taskProgress[index] = 0d;
				UpdateProgressAndReport();
				if (_currentState == TaskGroupState.Ready)
					_currentState = TaskGroupState.Running;
				Task<T> executingTask = task.ExecuteAsync(
					cancellationToken: _groupCancellation.Token,
					progress: new Progress<Progress>(progress =>
					{
						lock (_stateLock)
						{
							if (progress.Double is double progressValue)
								_taskProgress[index] = progressValue;
							UpdateProgressAndReport(index, progress);
						}
					})).ContinueWith(t =>
					{
						lock (_stateLock)
						{
							Exception currentError = _firstError;
							if (currentError is not null)
								throw currentError;
							if (t.IsFaulted)
							{
								_firstError = t.Exception?.InnerException ?? t.Exception ?? (Exception)new ApplicationException("Task faulted with no inner exception");
								_currentState = TaskGroupState.Faulted;
								_groupCancellation.Cancel();
								throw _firstError;
							}
							Interlocked.Increment(ref _completedTaskCount);
							_taskProgress[index] = 1d;
							if (CompletedTaskCount == TotalTaskCount && _currentState == TaskGroupState.Running)
								_currentState = TaskGroupState.Completed;
							UpdateProgressAndReport();
							return t.Result;
						}
					}, TaskContinuationOptions.None);
				_pendingResults.Enqueue((executingTask, index));
			}
			UpdateProgressAndReport();
			return true;
		}
	}
	/// <summary>
	/// Adds tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(params IProgressTask<T>[] tasks) => Add(tasks.AsEnumerable());
	/// <summary>
	/// Adds tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(IEnumerable<IProgressTask<T>> tasks)
	{
		if (TryAdd(tasks)) return this;
		if (_isDisposed)
			throw new ObjectDisposedException(nameof(ProgressTaskGroup<T>));
		if (_firstError is not null)
			throw _firstError;
		throw new InvalidOperationException($"Cannot add tasks in state {_currentState}.");
	}
	[RequiresLock("_stateLock")]
	private void UpdateProgressAndReport(int? taskIndex = null, Progress? taskProgress = null)
	{
		if (progress is null) return;
		// Capture values under lock
		double currentProgress = _taskProgress.Values.Average();
		string progressMessage = string.IsNullOrWhiteSpace(taskProgress?.String) ? null :
			$"Task {taskIndex + 1}: {taskProgress.Value.String}";
		_lastReportedProgress = currentProgress;
		// Report outside lock
		Task.Run(() => progress.Report(new Progress(
			Double: currentProgress,
			String: progressMessage)));
	}
	public async IAsyncEnumerable<T> GetResultsAsync(bool includeNewTasks = false)
	{
		await _getResultsLock.WaitAsync();
		try
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(ProgressTaskGroup<T>));
			Dictionary<int, T> completedResults = [];
			int baseIndex = 0, maxStartingIndex = TotalTaskCount - 1;
			while (!_isDisposed
				&& !_groupCancellation.Token.IsCancellationRequested
				&& _pendingResults.TryPeek(out (Task<T> Task, int Index) nextTask)
				&& (includeNewTasks || nextTask.Index <= maxStartingIndex))
			{
				T result;
				try
				{
					result = await nextTask.Task;
				}
				catch
				{
					break;// If task failed, stop enumeration
				}
				if (nextTask.Index == baseIndex)
				{
					_pendingResults.TryDequeue(out _);
					yield return result;
					while (completedResults.TryGetValue(++baseIndex, out T bufferedResult))
					{
						completedResults.Remove(baseIndex);
						yield return bufferedResult;
					}
				}
				else
				{
					_pendingResults.TryDequeue(out _);
					completedResults[nextTask.Index] = result;
				}
			}
		}
		finally
		{
			_getResultsLock.Release();
		}
	}
	public void Dispose()
	{
		lock (_stateLock)
		{
			if (_isDisposed) return;
			_isDisposed = true;
			_currentState = TaskGroupState.Disposed;
			_groupCancellation.Cancel();
			_groupCancellation.Dispose();
			_taskProgress.Clear();
			_getResultsLock.Dispose();
		}
	}
}
