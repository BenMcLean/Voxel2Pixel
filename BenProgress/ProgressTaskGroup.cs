using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BenProgress;

public sealed class ProgressTaskGroup<T>
{
	private readonly List<(IProgressTask<T> Task, double Progress)> _tasks = [];
	private readonly IProgress<Progress> _progress;
	private readonly object _lock = new();
	public ProgressTaskGroup(IProgress<Progress> progress = null, int initialCapacity = 4)
	{
		_progress = progress;
		_tasks.Capacity = initialCapacity;
	}
	public void AddTask(IProgressTask<T> task)
	{
		lock (_lock)
		{
			_tasks.Add((task, 0d));
		}
	}
	private void UpdateProgressAndReport(int taskIndex, Progress? taskProgress = null)
	{
		if (_progress is null) return;
		if (taskProgress?.Double is double progressValue)
			_tasks[taskIndex] = (_tasks[taskIndex].Task, progressValue);
		_progress.Report(new Progress(
			Double: _tasks.Average(t => t.Progress),
			String: string.IsNullOrWhiteSpace(taskProgress?.String) ? null : $"Task {taskIndex + 1}: {taskProgress.Value.String}"));
	}
	public async IAsyncEnumerable<T> ExecuteAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (_tasks.Count == 0) yield break;
		Task<T>[] runningTasks = new Task<T>[_tasks.Count];// Create array to track task completion and results
		for (int i = 0; i < _tasks.Count; i++)// Start all tasks
			runningTasks[i] = ProcessTask(
				task: _tasks[i].Task,
				taskIndex: i,
				cancellationToken: cancellationToken,
				taskProgress: _progress is null ? null :
					new Progress<Progress>(progress =>
					{
						lock (_lock)
						{
							UpdateProgressAndReport(i, progress);
						}
					}));
		foreach (Task<T> task in runningTasks) // Yield results in order as they complete
			yield return await task;
	}
	private async Task<T> ProcessTask(
		IProgressTask<T> task,
		int taskIndex,
		CancellationToken cancellationToken,
		IProgress<Progress> taskProgress)
	{
		try
		{
			return await task.ExecuteAsync(cancellationToken, taskProgress);
		}
		finally
		{
			lock (_lock)
			{
				_tasks[taskIndex] = (_tasks[taskIndex].Task, 1d);
				UpdateProgressAndReport(taskIndex);
			}
		}
	}
}
