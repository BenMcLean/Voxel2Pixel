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
	private readonly ConcurrentQueue<(Task<T> Task, int Index)> _pendingResults = [];
	private readonly Dictionary<int, double> _taskProgress = [];
	private readonly object _progressLock = new();
	private int _nextTaskIndex;
	private bool _isDisposed;
	public bool HasPendingResults => !_pendingResults.IsEmpty;
	public ProgressTaskGroup<T> Add(params IProgressTask<T>[] tasks) => Add(tasks.AsEnumerable());
	public ProgressTaskGroup<T> Add(IEnumerable<IProgressTask<T>> tasks)
	{
		lock (_progressLock)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(ProgressTaskGroup<T>));
			foreach (IProgressTask<T> task in tasks)
			{
				int index = Interlocked.Increment(ref _nextTaskIndex) - 1;
				// Start executing the task immediately
				Task<T> executingTask = task.ExecuteAsync(
					cancellationToken: cancellationToken,
					progress: new Progress<Progress>(progress =>
					{
						lock (_progressLock)
						{
							if (progress.Double is double progressValue)
								_taskProgress[index] = progressValue;
							UpdateProgressAndReport(index, progress);
						}
					}));
				_pendingResults.Enqueue((executingTask, index));
				_taskProgress[index] = 0d;
			}
			UpdateProgressAndReport();
		}
		return this;
	}
	private void UpdateProgressAndReport(int? taskIndex = null, Progress? taskProgress = null)
	{
		if (progress is null) return;
		double overallProgress = _taskProgress.Values.Average();
		progress.Report(new Progress(
			Double: overallProgress,
			String: string.IsNullOrWhiteSpace(taskProgress?.String) ? null :
				   $"Task {taskIndex + 1}: {taskProgress.Value.String}"));
	}
	/// <summary>
	/// Asynchronously retrieves results from tasks in the order they were added.
	/// This method is guaranteed to yield results from all tasks that were added
	/// before it was called. It may also yield results from tasks added while it
	/// is running, but will stop yielding once all currently known tasks are complete.
	///
	/// To ensure all results are retrieved, check HasPendingResults after this method
	/// completes and call GetResultsAsync again if it returns true.
	/// </summary>
	public async IAsyncEnumerable<T> GetResultsAsync()
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
				// Return any buffered results that are now ready
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
	public void Dispose()
	{
		lock (_progressLock)
		{
			if (_isDisposed) return;
			_isDisposed = true;
		}
	}
}
