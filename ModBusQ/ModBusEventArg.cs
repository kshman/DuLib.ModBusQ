using System.Net;
using System.Net.Sockets;

namespace Du.ModBusQ;

/// <summary>
/// 보내거나 받을 때 발생하는 이벤트 인수
/// </summary>
public class ModBusReadWriteEventArgs : EventArgs
{
	/// <summary>버퍼</summary>
	public IReadOnlyList<byte> Buffer { get; init; }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="buffer"></param>
	public ModBusReadWriteEventArgs(IReadOnlyList<byte> buffer)
	{
		Buffer = buffer;
	}
}

/// <summary>
/// 접속 상태가 바꼈을 때 발생하는 이벤트 인수
/// </summary>
public class ModBusConnectionChangedEventArgs : EventArgs
{
	/// <summary>커넥션 상태</summary>
	public bool IsConnected { get; init; }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="isConnected"></param>
	public ModBusConnectionChangedEventArgs(bool isConnected)
	{
		IsConnected = isConnected;
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
public class ModBusAddressCountEventArgs : EventArgs
{
	public int Address { get; init; }
	public int Count { get; init; }

	public ModBusAddressCountEventArgs(int address, int count)
	{
		Address = address;
		Count = count;
	}
}
