using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace BenProgress;

public static class ExtensionMethods
{
	#region PLINQ
	/// <summary>
	/// Parallelizes the execution of a Select query while preserving the order of the source sequence.
	/// </summary>
	public static List<TResult> Parallelize<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => [.. source
		.Select((element, index) => (element, index))
		.AsParallel()
		.Select(sourceTuple => (result: selector(sourceTuple.element), sourceTuple.index))
		.OrderBy(resultTuple => resultTuple.index)
		.AsEnumerable()
		.Select(resultTuple => resultTuple.result)];
	#endregion PLINQ
	public static ProgressTaskGroup<TResult> ToProgressGroup<TSource, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, CancellationToken?, IProgress<Progress>, Task<TResult>> selector,
		IProgress<Progress> progress = null,
		int initialCapacity = 4)
	{
		ProgressTaskGroup<TResult> group = new(progress, initialCapacity);
		foreach (TSource item in source)
		{
			group.AddTask(new DelegateProgressTask<TResult>(
				(ct, p) => selector(item, ct, p)));
		}
		return group;
	}
	public static async Task<ProgressTaskGroup<TResult>> ToProgressGroup<TSource, TResult>(
		this IAsyncEnumerable<TSource> source,
		Func<TSource, CancellationToken?, IProgress<Progress>, Task<TResult>> selector,
		CancellationToken cancellationToken = default,
		IProgress<Progress> progress = null)
	{
		List<TSource> items = [];
		await foreach (TSource item in source.WithCancellation(cancellationToken))
		{
			items.Add(item);
		}
		return items.ToProgressGroup(selector, progress, items.Count);
	}
	public static ProgressTaskGroup<T> CreateProgressGroup<T>(
		int count,
		Func<int, CancellationToken?, IProgress<Progress>, Task<T>> indexedWork,
		IProgress<Progress> progress = null) =>
		Enumerable.Range(0, count).ToProgressGroup(indexedWork, progress, count);
}
