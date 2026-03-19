using Du.ModBusQ.Supplement;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace Du.ModBusQ;

/// <summary>
/// UDP 기반의 ModBus 서버 구현입니다. 지정한 포트에서 UDP 패킷을 수신하여
/// 요청을 처리하고 응답을 전송합니다.
/// </summary>
public class ModBusServerUdp : ModBusServerIp
{
	private UdpClient? _udp;
	private bool _rehash;

	private CancellationTokenSource? _cts;
	private Task _taskListen = Task.CompletedTask;

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Udp;

	/// <summary>
	/// 서버가 바인드할 로컬 포트입니다. 값을 변경하면 내부 리스닝 소켓이 재생성됩니다.
	/// </summary>
	/// <remarks>
	/// 포트 변경은 런타임 중에도 가능하며 변경 시 내부에서 <see cref="_rehash"/> 플래그를 설정하여
	/// 리스닝 루프가 새 소켓을 생성하도록 합니다.
	/// </remarks>
	public new int Port
	{
		get => base.Port;
		set
		{
			base.Port = value;
			_rehash = true;
		}
	}

	/// <summary>
	/// UDP 기반의 ModBus 서버 구현입니다. 지정한 포트에서 UDP 패킷을 수신하여
	/// 요청을 처리하고 응답을 전송합니다.
	/// </summary>
	/// <param name="port">서버가 수신할 로컬 포트 번호입니다. 기본값은 502입니다.</param>
	/// <param name="logger">로깅을 위해 사용할 선택적 <see cref="Microsoft.Extensions.Logging.ILogger"/> 인스턴스입니다. null을 허용합니다.</param>
	public ModBusServerUdp(int port = 502, ILogger? logger = null) :
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
		_taskListen = Task.Run(() =>
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
				catch
				{
					// 헐랭
				}
			}
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

		_cts?.Cancel();
		_udp?.Close();
		_taskListen.Wait();

		_logger?.MethodLeave(MethodNameStop);
	}

	/// <summary>
	/// 수신된 UDP 패킷을 처리하고 응답을 전송합니다.
	/// </summary>
	/// <param name="state">수신된 패킷과 송신자 정보가 포함된 상태 객체입니다.</param>
	/// <remarks>
	/// 요청 처리는 HandleRequestBuffer(byte[]) 메소드를 호출하여 수행하며,
	/// 반환된 바이트 배열을 원래 송신자에게 UDP로 전송합니다.
	/// </remarks>
	private void InternalReceived(UdpClientState state)
	{
		// 버퍼를 처리함
		var bs = HandleRequestBuffer(state.Buffer);
		_udp?.Send(bs, bs.Length, state.EndPoint);
	}

	// Logger method name constants for this class
	private const string MethodNameStart = "ModBusServerUdp.Start";
	private const string MethodNameStop = "ModBusServerUdp.Stop";
}
