using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace BenProgress;

public class PeriodicUpdater(ProgressContext? ProgressContext)
{
	protected readonly Stopwatch Stopwatch = new();
	public Task UpdateAsync(
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string @string,
		double? @double = null) => UpdateAsync(@double: @double, format: @string);
	public async Task UpdateAsync(
		double? @double = null,
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = null,
		params object[] args)
	{
		if (ProgressContext is not ProgressContext context)
			return;
		Stopwatch sw = Stopwatch;
		if (!sw.IsRunning)
			sw.Start();
		else if (sw.ElapsedMilliseconds < context.Milliseconds)
			return;
		else
			sw.Reset();
		await context.UpdateAsync(
			@double: @double,
			format: format,
			args: args);
	}
	public Task ForceUpdateAsync(
		double? @double = null,
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? format = null,
		params object[] args) =>
		ProgressContext?.UpdateAsync(
			@double: @double,
			format: format,
			args: args);
	public Task ForceUpdateAsync(
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? @string = null,
		double? @double = null) =>
		ForceUpdateAsync(
			@double: @double,
			format: @string);
}
