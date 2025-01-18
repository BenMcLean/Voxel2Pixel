using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;

namespace BenProgress;

public readonly record struct Progress([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string String = null, double? Double = null)
{
	public Progress(
		double? @double = null,
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = null,
		params object[] args) : this(String: string.Format(format, args), Double: @double) { }
	public static async Task UpdateAsync(
		CancellationToken? cancellationToken = null,
		IProgress<Progress> progress = null,
		double? @double = null,
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = null,
		params object[] args)
	{
		progress?.Report(new Progress(@double, format, args));
		cancellationToken?.ThrowIfCancellationRequested();
		await Task.Yield();
	}
	public static async Task<DateTimeOffset> UpdateAsync(
		DateTimeOffset? lastCheck,
		int minMillisecondsBetweenUpdates = 100,
		CancellationToken? cancellationToken = null,
		IProgress<Progress> progress = null,
		double? @double = null,
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = null,
		params object[] args)
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (lastCheck.HasValue && (now - lastCheck.Value).TotalMilliseconds < minMillisecondsBetweenUpdates)
			return lastCheck.Value;
		await UpdateAsync(cancellationToken, progress, @double, format, args);
		return now;
	}
}
