using System.Net.Sockets;
using Du.ModBusQ.Suppliment;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// TCP 서버
/// </summary>
public class ModBusServerTcp : ModBusServerIp
{
	private readonly Lock _lock = new();

	private CancellationTokenSource? _cts;
	private Task _task_listen = Task.CompletedTask;

	private TcpListener? _listener;
	private readonly List<TcpClientState> _clients = [];

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Tcp;

	/// <summary>연결 유지 시간</summary>
	public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromHours(1);

  /// <summary>
  /// TCP 서버 인스턴스를 생성합니다.
  /// </summary>
  /// <param name="port">서버가 바인드할 로컬 포트 번호입니다. 기본값은 502입니다.</param>
  /// <param name="logger">로깅을 위해 사용할 선택적 <see cref="Microsoft.Extensions.Logging.ILogger"/> 인스턴스입니다. null을 허용합니다.</param>
  public ModBusServerTcp(int port = 502, ILogger? logger = null) :
		base(port, logger)
	{
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
			Stop();
	}

	/// <inheritdoc/>
	public override void Start()
	{
		if (IsRunning)
			return;

		_logger?.MethodEnter(MethodNameStart);

		IsRunning = true;
		StartTime = DateTime.Now;

		_cts = new CancellationTokenSource();
		_task_listen = Task.Run(() =>
		{
			_listener = new TcpListener(Address, Port);
			_listener.Start();
			_listener.BeginAcceptTcpClient(InternalAcceptTcpCallback, null);
		}, _cts.Token);

		_logger?.MethodLeave(MethodNameStart);
	}

	/// <inheritdoc/>
	public override void Stop()
	{
		if (!IsRunning)
			return;

		_logger?.MethodEnter(MethodNameStop);

		IsRunning = false;

		_listener?.Stop();

		_cts?.Cancel();
		_task_listen.Wait();

		lock (_lock)
		{
			foreach (var c in _clients)
				c.Stream.Close();
			_clients.Clear();
		}

		_logger?.MethodLeave(MethodNameStop);
	}

	/// <summary>
	/// 비동기적으로 수락된 TCP 클라이언트에 대한 콜백입니다.
	/// 수락된 클라이언트에 대해 읽기 준비를 설정하고 접속 이벤트를 발생시킵니다.
	/// </summary>
	/// <param name="res">비동기 작업의 상태를 나타내는 <see cref="IAsyncResult"/> 인스턴스입니다.</param>
	private void InternalAcceptTcpCallback(IAsyncResult res)
	{
		TcpClientState? state = null;

		try
		{
			var tcp = _listener!.EndAcceptTcpClient(res);
			_listener.BeginAcceptTcpClient(InternalAcceptTcpCallback, null);
			tcp.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;

			state = new TcpClientState(tcp);

			InternalCheckClient(state);

			state.Stream.ReadTimeout = (int)ReceiveTimeout.TotalMilliseconds;
			state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, InternalReadCallback, state);
		}
		catch (ObjectDisposedException)
		{
			// 개체가 제거됨
		}
		catch (SocketException)
		{
			// 소켓 오류
		}
		catch (IOException)
		{
			// IO 오류
		}
		catch (Exception ex)
		{
			// 훔
			System.Diagnostics.Debug.WriteLine(ex);
		}

		if (state != null)
			OnClientConnected(new ModBusClientEventArgs(state.RemoteEndPoint));
	}

	/// <summary>
	/// TCP 클라이언트의 스트림에서 비동기 읽기가 완료되었을 때 호출되는 콜백입니다.
	/// 읽기된 데이터가 없거나 오류가 발생하면 클라이언트 연결을 정리합니다.
	/// </summary>
	/// <param name="res">비동기 작업의 상태를 나타내는 <see cref="IAsyncResult"/> 인스턴스입니다.</param>
	/// <exception cref="ArgumentNullException">res.AsyncState가 <see cref="TcpClientState"/>가 아닐 경우 발생합니다.</exception>
	private void InternalReadCallback(IAsyncResult res)
	{
		if (res.AsyncState is not TcpClientState state)
		{
			// 아니 널일리가 없는데
			throw new ArgumentNullException(nameof(res));
		}

		// 시간 리플레시, 안끊기게
		state.Invalidate();

		// 읽기 끝
		try
		{
			var count = state.Stream.EndRead(res);
			if (count == 0)
				throw new IOException();
		}
		catch (IOException)
		{
			// 끊겻을 것이다
			InternalClientDisconnect(state);
			return;
		}
		catch (Exception)
		{
			// 이건 뭐 끊어야지
			InternalClientDisconnect(state);
			return;
		}

		// 버퍼를 처리함
		var bs = HandleRequestBuffer(state.Buffer);
		state.Stream.Write(bs, 0, bs.Length);

		// 다시 읽기 시작
		try
		{
			state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, InternalReadCallback, state);
		}
		catch (Exception)
		{
			// 역시나 끊겼겠지
			InternalClientDisconnect(state);
		}
	}

	/// <summary>
	/// 내부적으로 클라이언트 목록을 관리합니다. 새로운 클라이언트를 목록에 추가하고,
	/// 연결 유지시간을 초과한 클라이언트는 제거합니다.
	/// </summary>
	/// <param name="state">체크할 대상인 <see cref="TcpClientState"/> 객체입니다.</param>
	private void InternalCheckClient(TcpClientState state)
	{
		lock (_lock)
		{
			if (IsRunning && !_clients.Contains(state))
				_clients.Add(state);

			var ticks = DateTime.Now.Ticks;
			_clients.RemoveAll(c => (ticks - c.AliveTick) > KeepAliveTimeout.TotalMilliseconds);
		}
	}

	/// <summary>
	/// 지정한 클라이언트를 연결 해제 처리합니다. 이미 해제되었거나 처리 중이면 아무 작업도 하지 않습니다.
	/// 연결 해제 시 관련 이벤트를 호출하고 내부 목록을 정리합니다.
	/// </summary>
	/// <param name="state">해제할 대상인 <see cref="TcpClientState"/> 객체입니다.</param>
	private void InternalClientDisconnect(TcpClientState state)
	{
		if (state.AliveTick == 0)
		{
			// 이미 지웠거나, 작업에 진행 중이란 이야기 이므로 더 안함
			return;
		}

		state.MarkDisconnected();

		lock (_lock)
		{
			var nth = _clients.IndexOf(state);
			if (nth >= 0)
			{
				// 여기 있다는 것은 접속 때 메시지를 보냈다는 이야기
				OnClientDisconnected(new ModBusClientEventArgs(state.RemoteEndPoint));

				_clients.RemoveAt(nth);
			}

			// 겸사 겸사 다른 애들도 검사
			var ticks = DateTime.Now.Ticks;
			var timeout = KeepAliveTimeout.TotalMilliseconds;
			var expired = _clients.Where(c => (ticks - c.AliveTick) > timeout).ToList();

			foreach (var c in expired)
			{
				c.MarkDisconnected();
				OnClientDisconnected(new ModBusClientEventArgs(c.RemoteEndPoint));
				_clients.Remove(c);
			}
		}
	}

	// Logger method name constants for this class
	private const string MethodNameStart = "ModBusServerTcp.Start";
	private const string MethodNameStop = "ModBusServerTcp.Stop";
}
