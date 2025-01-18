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
	public static async IAsyncEnumerable<TResult> Parallelize<TSource, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, CancellationToken?, IProgress<Progress>, Task<TResult>> selector,
		[EnumeratorCancellation] CancellationToken cancellationToken = default,
		IProgress<Progress> progress = null)
	{
		using ProgressTaskGroup<TResult> group = new(cancellationToken, progress);
		group.Add(source.Select(item => new ProgressTask<TResult>(
			(ct, p) => selector(item, ct, p))));
		await foreach (TResult result in group.GetResultsAsync())
			yield return result;
	}
	public static async IAsyncEnumerable<TResult> Parallelize<TSource, TResult>(
		this IAsyncEnumerable<TSource> source,
		Func<TSource, CancellationToken?, IProgress<Progress>, Task<TResult>> selector,
		[EnumeratorCancellation] CancellationToken cancellationToken = default,
		IProgress<Progress> progress = null)
	{
		using ProgressTaskGroup<TResult> group = new(cancellationToken, progress);
		await foreach (TSource item in source.WithCancellation(cancellationToken))
			group.Add(new ProgressTask<TResult>(
				(ct, p) => selector(item, ct, p)));
		await foreach (TResult result in group.GetResultsAsync())
			yield return result;
	}
	public static IAsyncEnumerable<TResult> Parallelize<TSource, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, CancellationToken?, Task<TResult>> selector,
		CancellationToken cancellationToken = default) =>
		source.Parallelize(
			selector: (item, ct, p) => selector(item, ct),
			cancellationToken: cancellationToken);
	public static IAsyncEnumerable<TResult> Parallelize<TSource, TResult>(
		this IAsyncEnumerable<TSource> source,
		Func<TSource, CancellationToken?, Task<TResult>> selector,
		CancellationToken cancellationToken = default) =>
		source.Parallelize(
			selector: (item, ct, p) => selector(item, ct),
			cancellationToken: cancellationToken);
	public static IAsyncEnumerable<TResult> Parallelize<TSource, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, IProgress<Progress>, Task<TResult>> selector,
		IProgress<Progress> progress = null) =>
		source.Parallelize(
			selector: (item, ct, p) => selector(item, p),
			progress: progress);
	public static IAsyncEnumerable<TResult> Parallelize<TSource, TResult>(
		this IAsyncEnumerable<TSource> source,
		Func<TSource, IProgress<Progress>, Task<TResult>> selector,
		IProgress<Progress> progress = null) =>
		source.Parallelize(
			selector: (item, ct, p) => selector(item, p),
			progress: progress);
	public static IAsyncEnumerable<TResult> Parallelize<TSource, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, Task<TResult>> selector) =>
		source.Parallelize((item, ct, p) => selector(item));
	public static IAsyncEnumerable<TResult> Parallelize<TSource, TResult>(
		this IAsyncEnumerable<TSource> source,
		Func<TSource, Task<TResult>> selector) =>
		source.Parallelize((item, ct, p) => selector(item));
}
