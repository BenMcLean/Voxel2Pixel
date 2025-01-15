using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;

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
		/// At least one task failed
		/// </summary>
		Faulted,
		/// <summary>
		/// Group has been disposed
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
	public ProgressTaskGroup<T> Add(params IProgressTask<T>[] tasks) => Add(tasks.AsEnumerable());
	public ProgressTaskGroup<T> Add(IEnumerable<IProgressTask<T>> tasks)
	{
		lock (_stateLock)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(ProgressTaskGroup<T>));
			if (_firstError is not null)
				throw _firstError;
			foreach (IProgressTask<T> task in tasks)
			{
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
							if (_firstError is not null) return t.Result;
							if (!t.IsFaulted)
							{
								Interlocked.Increment(ref _completedTaskCount);
								_taskProgress[index] = 1d;
								if (CompletedTaskCount == TotalTaskCount && _currentState == TaskGroupState.Running)
									_currentState = TaskGroupState.Completed;
							}
							else if (_firstError is null)
							{
								_firstError = t.Exception?.InnerException;
								_currentState = TaskGroupState.Faulted;
								_groupCancellation.Cancel();
							}
							UpdateProgressAndReport();
						}
						return t.Result;
					}, TaskContinuationOptions.None);
				_pendingResults.Enqueue((executingTask, index));
			}
			UpdateProgressAndReport();
		}
		return this;
	}
	[RequiresLock("_stateLock")]
	private void UpdateProgressAndReport(int? taskIndex = null, Progress? taskProgress = null)
	{
		if (progress is null) return;
		_lastReportedProgress = _taskProgress.Values.Average();
		progress.Report(new Progress(
			Double: _lastReportedProgress,
			String: string.IsNullOrWhiteSpace(taskProgress?.String) ? null :
				$"Task {taskIndex + 1}: {taskProgress.Value.String}"));
	}
	public async IAsyncEnumerable<T> GetResultsAsync(bool includeNewTasks = false)
	{
		await _getResultsLock.WaitAsync();
		try
		{
			Dictionary<int, T> completedResults = [];
			int baseIndex = 0, maxStartingIndex = TotalTaskCount - 1;
			while (!_isDisposed
				&& _pendingResults.TryPeek(out (Task<T> Task, int Index) nextTask)
				&& (includeNewTasks || nextTask.Index <= maxStartingIndex)
				&& await nextTask.Task is T result)
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
