using Du.Properties;

namespace Du.ModBusQ.Suppliment;
#pragma warning disable S3881 // "IDisposable" should be implemented correctly

internal static class LockerOfReaderWriter
{
	internal static IDisposable GetReadLock(this ReaderWriterLockSlim l, int millisecondsTimeout = -1)
	{
		if (!l.TryEnterReadLock(millisecondsTimeout))
			throw new TimeoutException(Resources.ExceptionEnterRead);
		return new DisposableRwLocks(l, DisposableRwLocks.Mode.Read);
	}

	internal static IDisposable GetReadLock(this ReaderWriterLockSlim l, TimeSpan timeSpan)
	{
		if (!l.TryEnterReadLock(timeSpan))
			throw new TimeoutException(Resources.ExceptionEnterRead);
		return new DisposableRwLocks(l, DisposableRwLocks.Mode.Read);
	}

	internal static IDisposable GetUpgradableReadLock(this ReaderWriterLockSlim l, int millisecondsTimeout = -1)
	{
		if (!l.TryEnterUpgradeableReadLock(millisecondsTimeout))
			throw new TimeoutException(Resources.ExceptionEnterUpgradableRead);
		return new DisposableRwLocks(l, DisposableRwLocks.Mode.UpgradableRead);
	}

	internal static IDisposable GetUpgradableReadLock(this ReaderWriterLockSlim l, TimeSpan timeSpan)
	{
		if (!l.TryEnterUpgradeableReadLock(timeSpan))
			throw new TimeoutException(Resources.ExceptionEnterUpgradableRead);
		return new DisposableRwLocks(l, DisposableRwLocks.Mode.UpgradableRead);
	}

	internal static IDisposable GetWriteLock(this ReaderWriterLockSlim l, int millisecondsTimeout = -1)
	{
		if (!l.TryEnterWriteLock(millisecondsTimeout))
			throw new TimeoutException(Resources.ExceptionEnterWrite);
		return new DisposableRwLocks(l, DisposableRwLocks.Mode.Write);
	}

	internal static IDisposable GetWriteLock(this ReaderWriterLockSlim l, TimeSpan timeSpan)
	{
		if (!l.TryEnterWriteLock(timeSpan))
			throw new TimeoutException(Resources.ExceptionEnterWrite);
		return new DisposableRwLocks(l, DisposableRwLocks.Mode.Write);
	}
}

internal class DisposableRwLocks : IDisposable
{
	internal enum Mode
	{
		None,
		Read,
		UpgradableRead,
		Write,
	}

	private readonly ReaderWriterLockSlim _l;
	private Mode _m;

	public DisposableRwLocks(ReaderWriterLockSlim l, Mode m)
	{
		_l = l;
		_m = m;
	}

	public void Dispose()
	{
		switch (_m)
		{
			case Mode.None:
				return;

			case Mode.Read:
				_l.ExitReadLock();
				break;

			case Mode.UpgradableRead when _l.IsWriteLockHeld:
				_l.ExitWriteLock();
				break;

			case Mode.UpgradableRead:
				_l.ExitUpgradeableReadLock();
				break;

			case Mode.Write:
				_l.ExitWriteLock();
				break;

			default:
				_m = Mode.None;
				break;
		}
	}
}

#pragma warning restore S3881 // "IDisposable" should be implemented correctly

