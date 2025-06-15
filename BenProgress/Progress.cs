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
		params object[] args) : this(
			String: format is null ? null : string.Format(format, args),
			Double: @double) { }
	public static async Task UpdateAsync(
		CancellationToken? cancellationToken = null,
		IProgress<Progress> progress = null,
		bool yield = false,
		double? @double = null,
		[StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = null,
		params object[] args)
	{
		cancellationToken?.ThrowIfCancellationRequested();
		if (@double is not null || !string.IsNullOrWhiteSpace(format))
			progress?.Report(new Progress(@double, format, args));
		if (yield)
			await Task.Yield();
	}
}
