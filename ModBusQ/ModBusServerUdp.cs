using Du.ModBusQ.Suppliment;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace Du.ModBusQ;

/// <summary>
/// 새 인스턴스를 만들어요
/// </summary>
/// <param name="port"></param>
/// <param name="logger"></param>
public class ModBusServerUdp(int port = 502, ILogger? logger = null) : ModBusServerIp(port, logger)
{
	private UdpClient? _udp;
	private bool _rehash;

	private CancellationTokenSource? _cts;
	private Task _task_listen = Task.CompletedTask;

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Udp;
	public new int Port
	{
		get => base.Port;
		set
		{
			base.Port = value;
			_rehash = true;
		}
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

		_lg?.MethodEnter("ModBusServerUdp.Start");

		IsRunning = true;
		StartTime = DateTime.Now;

		_cts = new CancellationTokenSource();
		_task_listen = Task.Run(() =>
		{
			while (!_cts.Token.IsCancellationRequested)
			{
				if (_udp == null || _rehash)
				{
					_udp = new UdpClient(new IPEndPoint(Address, Port));
					_udp.Client.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;

					_rehash = false;
				}

				try
				{
					var ep = new IPEndPoint(IPAddress.Any, Port);
					var bs = _udp.Receive(ref ep);

					Task.Run(() => InternalReceived(new UdpClientState(ep, bs)));
				}
				catch (SocketException)
				{
					// 이건 IOCP 강제 종료일것임
				}
				catch(Exception)
				{
					// 헐랭
				}
			}
		}, _cts.Token);
		
		_lg?.MethodLeave("ModBusServerUdp.Start");
	}

	/// <inheritdoc/>
	public override void Stop()
	{
		if (!IsRunning)
			return;

		_lg?.MethodEnter("ModBusServerUdp.Stop");

		IsRunning = false;

		_cts?.Cancel();
		_udp?.Close();
		_task_listen.Wait();
		
		_lg?.MethodLeave("ModBusServerUdp.Stop");
	}

	private void InternalReceived(UdpClientState state)
	{
		// 버퍼를 처리함
		var bs = HandleRequestBuffer(state.Buffer);
		_udp?.Send(bs, bs.Length, state.EndPoint);
	}
}
