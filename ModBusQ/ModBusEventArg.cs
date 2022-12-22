namespace Du.ModBusQ;

/// <summary>
/// 보내거나 받을 때 발생하는 이벤트 인수
/// </summary>
public class ModBusReadWriteEventArg : EventArgs
{
	/// <summary>버퍼</summary>
	public IReadOnlyList<byte> Buffer { get; init; }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="buffer"></param>
	public ModBusReadWriteEventArg(IReadOnlyList<byte> buffer)
	{
		Buffer = buffer;
	}

	/*
	public byte[] Buffer { get; init; }

	public ModBusReadWriteEventArg(byte[] buffer)
	{
		Buffer = (byte[])buffer.Clone();
	}

	public ModBusReadWriteEventArg(byte[] buffer, int length)
	{
		Buffer = new byte[length];
		Array.Copy(buffer, 0, Buffer, 0, length);
	}
	*/
}

/// <summary>
/// 접속 상태가 바꼈을 때 발생하는 이벤트 인수
/// </summary>
public class ModBusConnectionChangedEventArg : EventArgs
{
	/// <summary>커넥션 상태</summary>
	public bool IsConnected { get; init; }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="isConnected"></param>
	public ModBusConnectionChangedEventArg(bool isConnected)
	{
		IsConnected = isConnected;
	}
}
