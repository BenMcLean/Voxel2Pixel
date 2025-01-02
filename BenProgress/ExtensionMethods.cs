using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

namespace BenProgress;

public static class ExtensionMethods
{
	#region PLINQ
	/// <summary>
	/// Parallelizes the execution of a Select query while preserving the order of the source sequence.
	/// </summary>
	public static List<TResult> Parallelize<TSource, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, TResult> selector) => [.. source
			.Select((element, index) => (element, index))
			.AsParallel()
			.Select(sourceTuple => (result: selector(sourceTuple.element), sourceTuple.index))
			.OrderBy(resultTuple => resultTuple.index)
			.AsEnumerable()
			.Select(resultTuple => resultTuple.result)];
	#endregion PLINQ
	public static async IAsyncEnumerable<TResult> ToProgressResults<TSource, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, CancellationToken?, IProgress<Progress>, Task<TResult>> selector,
		[EnumeratorCancellation] CancellationToken cancellationToken = default,
		IProgress<Progress> progress = null)
	{
		using ProgressTaskGroup<TResult> group = new(cancellationToken, progress);
		group.Add(source
			.Select(item => new DelegateProgressTask<TResult>(
				(ct, p) => selector(item, ct, p))));
		while (group.HasPendingResults)
			await foreach (TResult result in group.GetResultsAsync())
				yield return result;
	}
	public static async IAsyncEnumerable<TResult> ToProgressResults<TSource, TResult>(
		this IAsyncEnumerable<TSource> source,
		Func<TSource, CancellationToken?, IProgress<Progress>, Task<TResult>> selector,
		[EnumeratorCancellation] CancellationToken cancellationToken = default,
		IProgress<Progress> progress = null)
	{
		using ProgressTaskGroup<TResult> group = new(cancellationToken, progress);
		await foreach (TSource item in source.WithCancellation(cancellationToken))
			group.Add(new DelegateProgressTask<TResult>(
				(ct, p) => selector(item, ct, p)));
		while (group.HasPendingResults)
			await foreach (TResult result in group.GetResultsAsync())
				yield return result;
	}
}
