using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenProgress;

public interface IProgressTask<T>
{
	Task<T> ExecuteAsync(CancellationToken? cancellationToken = null, IProgress<Progress> progress = null);
}
public sealed class ProgressTask<T> : IProgressTask<T>
{
	private readonly Func<CancellationToken?, IProgress<Progress>, Task<T>> _work;
	public ProgressTask(Func<CancellationToken?, IProgress<Progress>, Task<T>> work) =>
		_work = work ?? throw new ArgumentNullException(nameof(work));
	public ProgressTask(Func<ProgressContext?, Task<T>> work)
	{
		if (work is null)
			throw new ArgumentNullException(nameof(work));
		_work = (cancellationToken, progress) =>
			work(new(
				CancellationToken: cancellationToken,
				Progress: progress,
				Milliseconds: ProgressContext.DefaultMilliseconds,
				Yield: false));
	}
	public ProgressTask(Func<ProgressContext?, Task<T>> work, int milliseconds = ProgressContext.DefaultMilliseconds, bool yield = false)
	{
		if (work is null)
			throw new ArgumentNullException(nameof(work));
		_work = (cancellationToken, progress) => work(new ProgressContext(
				CancellationToken: cancellationToken,
				Progress: progress,
				Milliseconds: milliseconds,
				Yield: yield));
	}
	public Task<T> ExecuteAsync(CancellationToken? cancellationToken = null, IProgress<Progress> progress = null) =>
		_work(cancellationToken, progress);
}
