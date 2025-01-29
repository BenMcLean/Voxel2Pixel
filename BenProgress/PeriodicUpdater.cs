using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace BenProgress;

public class PeriodicUpdater
{
	protected readonly Stopwatch Stopwatch = new();
	public int Milliseconds { get; set; } = 100;
	public CancellationToken? CancellationToken { get; set; } = null;
	public IProgress<Progress> Progress { get; set; } = null;
	public Task UpdateAsync(
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string @string,
		double? @double = null) => UpdateAsync(@double: @double, format: @string);
	public async Task UpdateAsync(
		double? @double = null,
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = null,
		params object[] args)
	{
		Stopwatch sw = Stopwatch;
		if (!sw.IsRunning)
			sw.Start();
		else if (sw.ElapsedMilliseconds < Milliseconds)
			return;
		else
			sw.Reset();
		await BenProgress.Progress.UpdateAsync(
			cancellationToken: CancellationToken,
			progress: Progress,
			@double: @double,
			format: format,
			args: args);
	}
}
