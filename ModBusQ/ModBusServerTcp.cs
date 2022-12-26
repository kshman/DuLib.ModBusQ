using System.Net.Sockets;
using Du.ModBusQ.Suppliment;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// TCP 서버
/// </summary>
public class ModBusServerTcp : ModBusServerIp
{
	private readonly object _lock = new();

	private CancellationTokenSource? _cts;
	private Task _task_listen = Task.CompletedTask;

	private TcpListener? _listener;
	private readonly List<TcpClientState> _clients = new();
	private int _ccount;

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Tcp;
	/// <inheritdoc/>
	public override bool IsRunning { get; protected set; }
	/// <inheritdoc/>
	public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromHours(1);

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="port"></param>
	/// <param name="logger"></param>
	public ModBusServerTcp(int port = 502, ILogger? logger = null)
		: base(port, logger)
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

		IsRunning = true;
		StartTime = DateTime.Now;

		_cts = new CancellationTokenSource();
		_task_listen = Task.Run(() =>
		{
			_listener = new TcpListener(Address, Port);
			_listener.Start();
			_listener.BeginAcceptTcpClient(new AsyncCallback(InternalAcceptTcpCallback), null);
		}, _cts.Token);
	}

	/// <inheritdoc/>
	public override void Stop()
	{
		if (!IsRunning)
			return;

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
	}

	private void InternalAcceptTcpCallback(IAsyncResult res)
	{
		TcpClient tcp;
		try
		{
			tcp = _listener!.EndAcceptTcpClient(res);
			_listener.BeginAcceptTcpClient(new AsyncCallback(InternalAcceptTcpCallback), null);
			tcp.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;

			var state = new TcpClientState(tcp);

			var count = InternalCheckClient(state);
			if (CanInvokeClientConnected)
				OnClientConnected(new ModBusClientEventArgs(state.RemoteEndPoint));
			_ccount = count;

			state.Stream.ReadTimeout = (int)ReceiveTimeout.TotalMilliseconds;
			state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, new AsyncCallback(InternalReadCallback), state);
		}
		catch (Exception ex)
		{
			// 훔
			System.Diagnostics.Debug.WriteLine(ex);
		}
	}

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
		int count;
		try
		{
			count = state.Stream.EndRead(res);
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
			state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, new AsyncCallback(InternalReadCallback), state);
		}
		catch (Exception)
		{
			// 역시나 끊겼겠지
			InternalClientDisconnect(state);
		}
	}

	private int InternalCheckClient(TcpClientState state)
	{
		lock (_lock)
		{
			if (IsRunning && !_clients.Contains(state))
				_clients.Add(state);

			var ticks = DateTime.Now.Ticks;
			_clients.RemoveAll(c => (ticks - c.AliveTick) > KeepAliveTimeout.TotalMilliseconds);

			return _clients.Count;
		}
	}

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
				if (CanInvokeClientDisconnected)
					OnClientDisconnected(new ModBusClientEventArgs(state.RemoteEndPoint));

				_clients.RemoveAt(nth);
			}

			// 겸사 겸사 다른 애들도 검사
			var ticks = DateTime.Now.Ticks;
			// 아래 두줄을 합치려면...?
			var l = _clients.Where(c => (ticks - c.AliveTick) > KeepAliveTimeout.TotalMilliseconds);
			if (l.Any())
				_clients.RemoveAll(c => (ticks - c.AliveTick) > KeepAliveTimeout.TotalMilliseconds);

			foreach (var c in l)
			{
				c.MarkDisconnected();

				if (CanInvokeClientDisconnected)
					OnClientDisconnected(new ModBusClientEventArgs(c.RemoteEndPoint));
			}
		}
	}
}
