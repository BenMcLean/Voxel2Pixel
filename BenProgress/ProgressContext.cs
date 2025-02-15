using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace BenProgress;

public readonly record struct ProgressContext(
	CancellationToken? CancellationToken = null,
	IProgress<Progress> Progress = null,
	int Milliseconds = ProgressContext.DefaultMilliseconds,
	bool Yield = false)
{
	public const int DefaultMilliseconds = 100;
	public Task UpdateAsync(
		double? @double = null,
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? format = null,
		params object[] args) =>
		BenProgress.Progress.UpdateAsync(
			cancellationToken: CancellationToken,
			progress: Progress,
			yield: Yield,
			@double: @double,
			format: format,
			args: args);
	public Task UpdateAsync(
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? @string = null,
		double? @double = null) =>
		UpdateAsync(
			@double: @double,
			format: @string);
}
