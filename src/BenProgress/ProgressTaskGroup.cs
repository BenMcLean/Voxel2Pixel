using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BenProgress;

public sealed class ProgressTaskGroup<T>(ProgressContext? progressContext = null) : IDisposable
{
	public ProgressContext? ProgressContext => progressContext;
	public ProgressTaskGroup(
		CancellationToken? cancellationToken = null,
		IProgress<Progress> progress = null,
		int milliseconds = BenProgress.ProgressContext.DefaultMilliseconds,
		bool yield = false) : this(new ProgressContext(
			CancellationToken: cancellationToken,
			Progress: progress,
			Milliseconds: milliseconds,
			Yield: yield))
	{ }
	private readonly CancellationTokenSource _groupCancellation =
		CancellationTokenSource.CreateLinkedTokenSource(
			progressContext?.CancellationToken ?? CancellationToken.None);
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
	#region Adding
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
				UpdateProgressAndReport(index, new Progress(0d));
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
							UpdateProgressAndReport(index, new Progress(1d));
							return t.Result;
						}
					}, TaskContinuationOptions.None);
				_pendingResults.Enqueue((executingTask, index));
			}
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
		throw _isDisposed ? new ObjectDisposedException(nameof(ProgressTaskGroup<T>))
			: _firstError ?? new InvalidOperationException($"Cannot add tasks in state {_currentState}.");
	}
	/// <summary>
	/// Adds functions to be executed as tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(IEnumerable<Func<CancellationToken?, IProgress<Progress>, Task<T>>> taskFunctions) =>
		taskFunctions is null ?
			throw new ArgumentNullException(nameof(taskFunctions))
			: Add(taskFunctions.Select(func => new ProgressTask<T>(func)));
	/// <summary>
	/// Adds functions to be executed as tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(params Func<CancellationToken?, IProgress<Progress>, Task<T>>[] taskFunctions) =>
		Add(taskFunctions.AsEnumerable());
	/// <summary>
	/// Attempts to add functions to be executed as tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(IEnumerable<Func<CancellationToken?, IProgress<Progress>, Task<T>>> taskFunctions) =>
		taskFunctions is null ?
			throw new ArgumentNullException(nameof(taskFunctions))
			: TryAdd(taskFunctions.Select(func => new ProgressTask<T>(func)));
	/// <summary>
	/// Attempts to add functions to be executed as tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(params Func<CancellationToken?, IProgress<Progress>, Task<T>>[] taskFunctions) =>
		TryAdd(taskFunctions.AsEnumerable());
	/// <summary>
	/// Adds functions that accept ProgressContext to be executed as tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(IEnumerable<Func<ProgressContext?, Task<T>>> taskFunctions) =>
		taskFunctions is null ?
			throw new ArgumentNullException(nameof(taskFunctions))
			: Add(taskFunctions.Select(func => new ProgressTask<T>(func)));
	/// <summary>
	/// Adds functions that accept ProgressContext to be executed as tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(params Func<ProgressContext?, Task<T>>[] taskFunctions) =>
		Add(taskFunctions.AsEnumerable());
	/// <summary>
	/// Attempts to add functions that accept ProgressContext to be executed as tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(IEnumerable<Func<ProgressContext?, Task<T>>> taskFunctions) =>
		taskFunctions is null ?
			throw new ArgumentNullException(nameof(taskFunctions))
			: TryAdd(taskFunctions.Select(func => new ProgressTask<T>(func)));
	/// <summary>
	/// Attempts to add functions that accept ProgressContext to be executed as tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(params Func<ProgressContext?, Task<T>>[] taskFunctions) =>
		TryAdd(taskFunctions.AsEnumerable());
	/// <summary>
	/// Adds functions that accept ProgressContext with custom settings to be executed as tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(IEnumerable<Func<ProgressContext?, Task<T>>> taskFunctions, int milliseconds = BenProgress.ProgressContext.DefaultMilliseconds, bool yield = false) =>
		taskFunctions is null ?
			throw new ArgumentNullException(nameof(taskFunctions))
			: Add(taskFunctions.Select(func => new ProgressTask<T>(func, milliseconds, yield)));
	/// <summary>
	/// Adds functions that accept ProgressContext with custom settings to be executed as tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(int milliseconds, bool yield, params Func<ProgressContext?, Task<T>>[] taskFunctions) =>
		Add(taskFunctions.AsEnumerable(), milliseconds, yield);
	/// <summary>
	/// Adds functions that accept ProgressContext with custom milliseconds setting to be executed as tasks to the group. Throws if the group is in an error state or disposed.
	/// </summary>
	public ProgressTaskGroup<T> Add(int milliseconds, params Func<ProgressContext?, Task<T>>[] taskFunctions) =>
		Add(taskFunctions.AsEnumerable(), milliseconds, false);
	/// <summary>
	/// Attempts to add functions that accept ProgressContext with custom settings to be executed as tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(IEnumerable<Func<ProgressContext?, Task<T>>> taskFunctions, int milliseconds = BenProgress.ProgressContext.DefaultMilliseconds, bool yield = false) =>
		taskFunctions is null ?
			throw new ArgumentNullException(nameof(taskFunctions))
			: TryAdd(taskFunctions.Select(func => new ProgressTask<T>(func, milliseconds, yield)));
	/// <summary>
	/// Attempts to add functions that accept ProgressContext with custom settings to be executed as tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(int milliseconds, bool yield, params Func<ProgressContext?, Task<T>>[] taskFunctions) =>
		TryAdd(taskFunctions.AsEnumerable(), milliseconds, yield);
	/// <summary>
	/// Attempts to add functions that accept ProgressContext with custom milliseconds setting to be executed as tasks to the group. Returns false if the group is in an error state or disposed.
	/// </summary>
	public bool TryAdd(int milliseconds, params Func<ProgressContext?, Task<T>>[] taskFunctions) =>
		TryAdd(taskFunctions.AsEnumerable(), milliseconds, false);
	#endregion Adding
	[RequiresLock("_stateLock")]
	private void UpdateProgressAndReport(int taskIndex, Progress? taskProgress = null)
	{
		if (progressContext?.Progress is null) return;
		string progressMessage = string.IsNullOrWhiteSpace(taskProgress?.String) ? null :
			$"Task {taskIndex + 1}: {taskProgress.Value.String}";
		double? currentProgress = null;
		if (taskProgress?.Double is not null)
		{// Only recalculate aggregate when a task reports a number
			double aggregateProgress = _taskProgress.Values.Average();
			if (aggregateProgress > _lastReportedProgress + 1e-10)
				currentProgress = _lastReportedProgress = aggregateProgress;
		}
		// Report outside lock (report if there's a message or numeric progress)
		if (currentProgress is not null || progressMessage is not null)
			Task.Run(() => progressContext.Value.UpdateAsync(currentProgress, progressMessage));
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
