using System;
using System.Threading.Tasks;
using System.Threading;

namespace BenProgress;

public readonly record struct Progress(double? Double = null, string String = null);
public interface IProgressTask<T>
{
	Task<T> ExecuteAsync(CancellationToken? cancellationToken = null, IProgress<Progress> progress = null);
}
public sealed class DelegateProgressTask<T>(Func<CancellationToken?, IProgress<Progress>, Task<T>> work) : IProgressTask<T>
{
	public Task<T> ExecuteAsync(
		CancellationToken? cancellationToken = null,
		IProgress<Progress> progress = null) =>
		work(cancellationToken, progress);
}
