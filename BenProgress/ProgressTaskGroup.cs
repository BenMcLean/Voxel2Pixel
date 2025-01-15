using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BenProgress;

public sealed class ProgressTaskGroup<T>(
	CancellationToken? cancellationToken = null,
	IProgress<Progress> progress = null) : IDisposable
{
	private readonly CancellationTokenSource _groupCancellation = CancellationTokenSource.CreateLinkedTokenSource(
		cancellationToken ?? CancellationToken.None);
	private readonly ConcurrentQueue<(Task<T> Task, int Index)> _pendingResults = [];
	private readonly Dictionary<int, double> _taskProgress = [];
	private readonly object _progressLock = new();
	private readonly SemaphoreSlim _getResultsLock = new(1);
	private int _nextTaskIndex;
	private bool _isDisposed;
	private Exception _firstError;
	public bool HasPendingResults => !_pendingResults.IsEmpty;
	public ProgressTaskGroup<T> Add(params IProgressTask<T>[] tasks) => Add(tasks.AsEnumerable());
	public ProgressTaskGroup<T> Add(IEnumerable<IProgressTask<T>> tasks)
	{
		lock (_progressLock)
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
				Task<T> executingTask = task.ExecuteAsync(
					cancellationToken: _groupCancellation.Token,
					progress: new Progress<Progress>(progress =>
					{
						lock (_progressLock)
						{
							if (progress.Double is double progressValue)
								_taskProgress[index] = progressValue;
							UpdateProgressAndReport(index, progress);
						}
					})).ContinueWith(t =>
					{
						if (t.IsFaulted && _firstError is null)
						{
							_firstError = t.Exception?.InnerException;
							_groupCancellation.Cancel();
						}
						lock (_progressLock)
						{
							_taskProgress[index] = 1d;
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
	/// <summary>
	/// Should only be called by code that has _progressLock
	/// </summary>
	private void UpdateProgressAndReport(int? taskIndex = null, Progress? taskProgress = null)
	{
		if (progress is null) return;
		progress.Report(new Progress(
			Double: _taskProgress.Values.Average(),
			String: string.IsNullOrWhiteSpace(taskProgress?.String) ? null :
				   $"Task {taskIndex + 1}: {taskProgress.Value.String}"));
	}
	/// <summary>
	/// Asynchronously retrieves results from tasks in the order they were added.
	/// This method is guaranteed to yield results from all tasks that were added
	/// before it was called. It may also yield results from tasks added while it
	/// is running, but will stop yielding once all currently known tasks are complete.
	///
	/// Multiple calls to this method are serialized to prevent concurrent enumeration.
	/// To ensure all results are retrieved, check HasPendingResults after this method
	/// completes and call GetResultsAsync again if it returns true.
	/// </summary>
	public async IAsyncEnumerable<T> GetResultsAsync()
	{
		await _getResultsLock.WaitAsync();
		try
		{
			Dictionary<int, T> completedResults = [];
			int nextResultIndex = 0;
			while (HasPendingResults)
			{
				if (!_pendingResults.TryPeek(out (Task<T> Task, int Index) nextTask))
					continue;
				T result = await nextTask.Task;
				if (nextTask.Index == nextResultIndex)
				{
					_pendingResults.TryDequeue(out _);
					yield return result;
					nextResultIndex++;
					while (completedResults.TryGetValue(nextResultIndex, out T bufferedResult))
					{
						completedResults.Remove(nextResultIndex);
						yield return bufferedResult;
						nextResultIndex++;
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
		lock (_progressLock)
		{
			if (_isDisposed) return;
			_isDisposed = true;
			_groupCancellation.Cancel();
			_groupCancellation.Dispose();
			_taskProgress.Clear();
			_getResultsLock.Dispose();
		}
	}
}
