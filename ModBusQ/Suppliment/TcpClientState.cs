using System.Net;
using System.Net.Sockets;

namespace Du.ModBusQ.Suppliment;

internal class TcpClientState
{
	public TcpClient Tcp { get; init; }
	public byte[] Buffer { get; init; }
	public NetworkStream Stream { get; init; }
	public long AliveTick { get; set; }
	public IPEndPoint RemoveEndPoint { get; set; }

	public TcpClientState(TcpClient tcp)
	{
		Tcp = tcp;
		Buffer = new byte[tcp.ReceiveBufferSize];
		Stream = tcp.GetStream();
		RemoveEndPoint = (tcp.Client.RemoteEndPoint as IPEndPoint)!; // 이게 널일리가 없음
	}

	public void Invalidate()
	{
		AliveTick = DateTime.Now.Ticks;
	}
}
