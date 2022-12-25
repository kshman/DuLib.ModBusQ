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
	private CancellationTokenSource _ctsrc = new();

	private TcpListener? _listener;
	private readonly List<TcpClientState> _clients = new();
	private int _ccount;

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Tcp;
	/// <inheritdoc/>
	public override bool IsRunning { get; protected set; }
	/// <inheritdoc/>
	public override DateTime StartTime { get; protected set; }

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
	}

	/// <inheritdoc/>
	public override void Start()
	{
		if (IsRunning)
			return;

		_listener = new TcpListener(Address, Port);
		_listener.Start();
		_listener.BeginAcceptTcpClient(new AsyncCallback(InternalAcceptTcpCallback), null);
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
				OnClientConnected(new ModBusClientEventArgs(count, state.RemoveEndPoint));
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

		state.Invalidate();

		int count;
		try
		{
			count = state.Stream.EndRead(res);
			if (count == 0)
				throw new IOException();
		}
		catch
		{
			// 끊겻을 것이다
			return;
		}

		InternalHandleRequest(state);

		try
		{
			state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, new AsyncCallback(InternalReadCallback), state);
		}
		catch
		{
			// 역시나 끊겼겠지
		}
	}

	private int InternalCheckClient(TcpClientState state)
	{
		lock (_lock)
		{
			bool b = false;
			foreach (var _ in from c in _clients where state.Equals(c) select new { })
				b = true;

			var ticks = DateTime.Now.Ticks;
			_clients.RemoveAll(c => (ticks - c.AliveTick) > 40000000); // 얼라이브 시간 조정할것

			if (!b)
				_clients.Add(state);

			return _clients.Count;
		}
	}

	/// <inheritdoc/>
	public override void Stop()
	{
		_ctsrc.Cancel();

		_listener?.Stop();

		foreach (var c in _clients)
			c.Stream.Close();
		_clients.Clear();

		_ctsrc = new CancellationTokenSource();
	}

	//
	private void InternalHandleRequest(TcpClientState state)
	{
		var rsp = HandleRequest(state.Buffer);
		state.Stream.Write(rsp.Buffer, 0, rsp.Buffer.Length);
	}
}
