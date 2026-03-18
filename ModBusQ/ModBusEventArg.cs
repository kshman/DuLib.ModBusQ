using System.Net;

namespace Du.ModBusQ;

/// <summary>
/// 버퍼 사용 관련 이벤트 인수입니다. 수신 또는 전송된 바이트 버퍼를 전달합니다.
/// </summary>
/// <param name="buffer">이벤트와 함께 전달되는 바이트 버퍼입니다.</param>
public class ModBusBufferedEventArgs(IReadOnlyList<byte> buffer) : EventArgs
{
	/// <summary>이벤트와 함께 전달된 바이트 버퍼입니다.</summary>
	public IReadOnlyList<byte> Buffer { get; init; } = buffer;
}

/// <summary>
/// 연결 상태 변경 시 전달되는 이벤트 인수입니다.
/// </summary>
/// <param name="state">현재 연결 상태를 나타냅니다(true면 연결됨).</param>
public class ModBusStateChangedEventArgs(bool state) : EventArgs
{
	/// <summary>현재 연결 상태를 나타냅니다.</summary>
	public bool IsConnected => state;
}

/// <summary>
/// 서버에서 클라이언트 관련 이벤트에 전달되는 인수입니다. 원격 엔드포인트 정보를 포함합니다.
/// </summary>
/// <param name="remoteEp">원격 클라이언트의 엔드포인트 정보입니다.</param>
public class ModBusClientEventArgs(IPEndPoint remoteEp) : EventArgs
{
	/// <summary>클라이언트의 IP 주소입니다.</summary>
	public IPAddress Address { get; init; } = remoteEp.Address;
	/// <summary>클라이언트의 포트 번호입니다.</summary>
	public int Port { get; init; } = remoteEp.Port;
}

/// <summary>
/// 주소 기반 데이터 변경(예: Write) 이벤트에 전달되는 인수입니다.
/// </summary>
/// <param name="devId">디바이스 식별자입니다.</param>
/// <param name="address">시작 주소입니다.</param>
/// <param name="count">변경된 항목의 개수입니다.</param>
public class ModBusAddressEventArgs(int devId, int address, int count) : EventArgs
{
	/// <summary>대상 디바이스 식별자입니다.</summary>
	public int DeviceId { get; init; } = devId;
	/// <summary>작업의 시작 주소입니다.</summary>
	public int Address { get; init; } = address;
	/// <summary>작업에서 영향을 받은 항목 수입니다.</summary>
	public int Count { get; init; } = count;
}
