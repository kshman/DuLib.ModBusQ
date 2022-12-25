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
/// 개수를 알려줄 때 필요한 이벤트
/// </summary>
public class ModBusCountEventArgs : EventArgs
{
	/// <summary>개수</summary>
	public int Count { get; init; }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="count"></param>
	public ModBusCountEventArgs(int count)
	{
		Count = count;
	}
}

public class ModBusClientEventArgs : EventArgs
{
	public int Count { get; init; }

	public IPAddress Address { get; init; }
	public int Port { get; init; }

	public ModBusClientEventArgs(int count, IPEndPoint remoteEp)
	{
		Count = count;

		Address = remoteEp.Address;
		Port = remoteEp.Port;
	}
}
