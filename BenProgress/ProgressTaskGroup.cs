using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BenProgress;

public sealed class ProgressTaskGroup<T> : IDisposable
{
	private readonly List<(IProgressTask<T> Task, double Progress)> _tasks = [];
	private readonly IProgress<Progress> _progress;
	private readonly object _progressLock = new();
	private readonly object _errorLock = new();
	private readonly CancellationTokenSource _internalCancellationTokenSource = new();
	private bool _isDisposed;
	private bool _hasReportedError;
	public ProgressTaskGroup(IProgress<Progress> progress = null, int initialCapacity = 4)
	{
		_progress = progress;
		_tasks.Capacity = initialCapacity;
	}
	public void AddTask(IProgressTask<T> task)
	{
		lock (_progressLock)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(ProgressTaskGroup<T>));
			_tasks.Add((task, 0d));
		}
	}
	/// <summary>
	/// Should only be called within _progressLock
	/// </summary>
	private void UpdateProgressAndReport(int taskIndex, Progress? taskProgress = null)
	{
		if (_progress is null) return;
		if (taskProgress?.Double is double progressValue)
			_tasks[taskIndex] = (_tasks[taskIndex].Task, progressValue);
		_progress.Report(new Progress(
			Double: _tasks.Average(t => t.Progress),
			String: string.IsNullOrWhiteSpace(taskProgress?.String) ? null : $"Task {taskIndex + 1}: {taskProgress.Value.String}"));
	}
	public async IAsyncEnumerable<T> ExecuteAllAsync(
	[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (_tasks.Count == 0) yield break;
		using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCancellationTokenSource.Token);
		TaskCompletionSource<object> errorTaskCompletionSource = new();
		foreach (Task<T> task in _tasks
			.Select((task, taskIndex) => task.Task
				.ExecuteAsync(
					cancellationToken: linkedCancellationTokenSource.Token,
					progress: _progress is null ? null : new Progress<Progress>(progress =>
					{
						lock (_progressLock)
						{
							UpdateProgressAndReport(taskIndex, progress);
						}
					}))
				.ContinueWith(task =>
				{
					lock (_errorLock)
					{
						if (_hasReportedError) return task.Result;// Only handle the first error
						if (task.IsFaulted)
						{
							_hasReportedError = true;
							Exception ex = task.Exception?.InnerExceptions.FirstOrDefault() ?? task.Exception;
							lock (_progressLock)
							{
								_progress?.Report(new Progress(String: $"Task exception {ex.GetType().Name}: {ex.Message}"));
							}
							_internalCancellationTokenSource.Cancel();
							errorTaskCompletionSource.TrySetException(ex);
						}
						else if (task.IsCanceled && cancellationToken.IsCancellationRequested)
						{
							_hasReportedError = true;
							lock (_progressLock)
							{
								_progress?.Report(new Progress(String: "Operation cancelled by request"));
							}
							errorTaskCompletionSource.TrySetException(
								new OperationCanceledException("Task group cancelled by request",
									innerException: null, cancellationToken));
						}
						else if (task.IsCompleted)
							lock (_progressLock)
							{
								UpdateProgressAndReport(taskIndex, new Progress(Double: 1d));
							}
					}
					return task.Result;
				}))
			.ToArray())
			yield return await task;
	}
	public void Dispose()
	{
		lock (_progressLock)
		{
			if (_isDisposed) return;
			_isDisposed = true;
			_internalCancellationTokenSource.Cancel();
			_internalCancellationTokenSource.Dispose();
		}
	}
}
