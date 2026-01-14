using System.Net;

namespace Du.ModBusQ;

/// <summary>
/// 버퍼를 사용할 때 발생하는 이벤트 인수
/// </summary>
/// <remarks>
/// 새 인스턴스를 만들어요
/// </remarks>
/// <param name="buffer"></param>
public class ModBusBufferedEventArgs(IReadOnlyList<byte> buffer) : EventArgs
{
	/// <summary>버퍼</summary>
	public IReadOnlyList<byte> Buffer { get; init; } = buffer;
}

/// <summary>
/// 상태가 바꼈을 때 발생하는 이벤트 인수
/// </summary>
/// <remarks>
/// 새 인스턴스를 만들어요
/// </remarks>
/// <param name="state"></param>
public class ModBusStateChangedEventArgs(bool state) : EventArgs
{
	/// <summary>상태</summary>
	public bool State { get; init; } = state;

	/// <summary>커넥션 상태</summary>
	public bool IsConnected => State;
}

/// <summary>
/// 서버의 클라이언트 이벤트
/// </summary>
public class ModBusClientEventArgs(IPEndPoint remoteEp) : EventArgs
{
	public IPAddress Address { get; init; } = remoteEp.Address;
	public int Port { get; init; } = remoteEp.Port;
}

/// <summary>
/// 서버의 주소 기반 데이터 처리 Write~ 시리즈
/// </summary>
public class ModBusAddressEventArgs(int devId, int address, int count) : EventArgs
{
	public int DeviceId { get; init; } = devId;
	public int Address { get; init; } = address;
	public int Count { get; init; } = count;
}
