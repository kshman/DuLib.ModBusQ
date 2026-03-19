using Du.Properties;

namespace Du.ModBusQ.Supplement;

internal static class LockerOfReaderWriter
{
	// 잠금 획득과 해제를 IDisposable 패턴으로 래핑하여 편리하게 사용할 수 있도록 확장 메서드를 제공합니다.
	extension(ReaderWriterLockSlim l)
	{
		internal IDisposable GetReadLock(int millisecondsTimeout = -1)
		{
			return !l.TryEnterReadLock(millisecondsTimeout) ?
				throw new TimeoutException(Resources.ExceptionEnterRead) :
				new DisposableRwLocks(l, DisposableRwLocks.Mode.Read);
		}

		internal IDisposable GetReadLock(TimeSpan timeSpan)
		{
			return !l.TryEnterReadLock(timeSpan) ?
				throw new TimeoutException(Resources.ExceptionEnterRead) :
				new DisposableRwLocks(l, DisposableRwLocks.Mode.Read);
		}

		internal IDisposable GetUpgradableReadLock(int millisecondsTimeout = -1)
		{
			return !l.TryEnterUpgradeableReadLock(millisecondsTimeout) ?
				throw new TimeoutException(Resources.ExceptionEnterUpgradableRead) :
				new DisposableRwLocks(l, DisposableRwLocks.Mode.UpgradableRead);
		}

		internal IDisposable GetUpgradableReadLock(TimeSpan timeSpan)
		{
			return !l.TryEnterUpgradeableReadLock(timeSpan) ?
				throw new TimeoutException(Resources.ExceptionEnterUpgradableRead) :
				new DisposableRwLocks(l, DisposableRwLocks.Mode.UpgradableRead);
		}

		internal IDisposable GetWriteLock(int millisecondsTimeout = -1)
		{
			return !l.TryEnterWriteLock(millisecondsTimeout) ?
				throw new TimeoutException(Resources.ExceptionEnterWrite) :
				new DisposableRwLocks(l, DisposableRwLocks.Mode.Write);
		}

		internal IDisposable GetWriteLock(TimeSpan timeSpan)
		{
			return !l.TryEnterWriteLock(timeSpan) ?
				throw new TimeoutException(Resources.ExceptionEnterWrite) :
				new DisposableRwLocks(l, DisposableRwLocks.Mode.Write);
		}
	}
}

internal sealed class DisposableRwLocks : IDisposable
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
	private bool _disposed;

	internal DisposableRwLocks(ReaderWriterLockSlim l, Mode m)
	{
		_l = l ?? throw new ArgumentNullException(nameof(l));
		_m = m;
	}

	public void Dispose()
	{
		Dispose(true);
		// 소멸자가 없으면 아래 줄은 하지 않아도 좋다고 함
		//GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			switch (_m)
			{
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

				case Mode.None:
				default:
					break;
			}
		}

		_m = Mode.None;
		_disposed = true;
	}
}
