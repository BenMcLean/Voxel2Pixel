using System;
using System.Threading.Tasks;
using System.Threading;

namespace BenProgress;

public readonly record struct Progress(double? Double = null, string String = null)
{
	public Progress(string String = null, double? Double = null) : this(Double, String) { }
	public static async Task UpdateAsync(
		CancellationToken? cancellationToken = null,
		IProgress<Progress> progress = null,
		double? @double = null,
		string @string = null)
	{
		progress?.Report(new Progress(@double, @string));
		cancellationToken?.ThrowIfCancellationRequested();
		await Task.Yield();
	}
	public static async Task<DateTimeOffset> UpdateAsync(
		DateTimeOffset? lastCheck,
		CancellationToken? cancellationToken = null,
		IProgress<Progress> progress = null,
		double? @double = null,
		string @string = null,
		int minMillisecondsBetweenUpdates = 100)
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (lastCheck.HasValue && (now - lastCheck.Value).TotalMilliseconds < minMillisecondsBetweenUpdates)
			return lastCheck.Value;
		await UpdateAsync(cancellationToken, progress, @double, @string);
		return now;
	}
}
