using System.Net;
using System.Net.Sockets;

namespace Du.ModBusQ;

/// <summary>
/// 버퍼를 사용할 때 발생하는 이벤트 인수
/// </summary>
public class ModBusBufferedEventArgs : EventArgs
{
	/// <summary>버퍼</summary>
	public IReadOnlyList<byte> Buffer { get; init; }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="buffer"></param>
	public ModBusBufferedEventArgs(IReadOnlyList<byte> buffer)
	{
		Buffer = buffer;
	}
}

/// <summary>
/// 상태가 바꼈을 때 발생하는 이벤트 인수
/// </summary>
public class ModBusStateChangedEventArgs : EventArgs
{
	/// <summary>상태</summary>
	public bool State { get; init; }

	/// <summary>커넥션 상태</summary>
	public bool IsConnected => State;

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="state"></param>
	public ModBusStateChangedEventArgs(bool state)
	{
		State = state;
	}
}

/// <summary>
/// 서버의 클라이언트 이벤트
/// </summary>
public class ModBusClientEventArgs : EventArgs
{
	public IPAddress Address { get; init; }
	public int Port { get; init; }

	public ModBusClientEventArgs(IPEndPoint remoteEp)
	{
		Address = remoteEp.Address;
		Port = remoteEp.Port;
	}
}

/// <summary>
/// 서버의 주소 기반 데이터 처리 Write~ 시리즈
/// </summary>
public class ModBusAddressEventArgs : EventArgs
{
	public int DeviceId { get; init; }
	public int Address { get; init; }
	public int Count { get; init; }

	public ModBusAddressEventArgs(int devId, int address, int count)
	{
		DeviceId = devId;
		Address = address;
		Count = count;
	}
}
