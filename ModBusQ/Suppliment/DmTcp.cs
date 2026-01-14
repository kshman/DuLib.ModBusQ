using System.Net;
using System.Net.Sockets;

namespace Du.ModBusQ.Suppliment;

internal static class DmTcp
{
	internal static void StreamWrite(this NetworkStream nst, byte[] buffer)
	{
		nst.Write(buffer, 0, buffer.Length);
	}

	internal static byte[] StreamRead(this NetworkStream nst, int count, out int read)
	{
		var buffer = new byte[count];
		read = nst.Read(buffer, 0, count);
		return buffer;
	}

	internal static byte[] BuildTcpBuffer(
		int id, uint transaction,
		int address, int countOrValue,
		ModBusFunction function, int length,
		int size = 12)
	{
		// 이거 엔디안 체크 해야하는거 아님....
		var btrn = BitConverter.GetBytes(transaction);
		var blen = BitConverter.GetBytes(length);
		var bsa = BitConverter.GetBytes(address);
		var bcnv = BitConverter.GetBytes(countOrValue);

		var bs = new byte[]
		{
			// 트랜잭션
			btrn[1], btrn[0],
			// 프로토콜
			0, 0,
			// 길이
			blen[1], blen[0],
			// 유닛 아이디
			(byte)id,
			// 기능
			(byte)function,
			// 시작 주소
			bsa[1], bsa[0],
			// 갯수 또는 값
			bcnv[1], bcnv[0],
		};

		if (size > 12)
			Array.Resize(ref bs, size);

		return bs;
	}
}

internal class UdpClientState(IPEndPoint endPoint, byte[] data)
{
	public IPEndPoint EndPoint { get; set; } = endPoint;
	public byte[] Buffer { get; set; } = data;
}

internal class TcpClientState
{
	public byte[] Buffer { get; init; }
	public NetworkStream Stream { get; init; }
	public long AliveTick { get; private set; }
	public IPEndPoint RemoteEndPoint { get; set; }

	public TcpClientState(TcpClient tcp)
	{
		Buffer = new byte[tcp.ReceiveBufferSize];
		Stream = tcp.GetStream();
		RemoteEndPoint = (tcp.Client.RemoteEndPoint as IPEndPoint)!; // 이게 널일리가 없음

		Invalidate();
	}

	public void Invalidate()
		=> AliveTick = DateTime.Now.Ticks;

	public void MarkDisconnected()
		=> AliveTick = 0;
}

internal class Transfer
{
	public ushort Transaction { get; protected init; }
	public ushort Protocol { get;  protected init; }
	protected ushort Length { get; init; }

	public byte Identifier { get;  protected init; }
	public byte Function { get;  protected init; }
	protected byte Count { get; init; }

	public ushort Crc { get; set; }

	public ushort Address { get; protected init; }
	public ushort Quantity { get; protected init; }
	public ModBusErrorCode Error { get; protected init; }
}

internal class Request : Transfer
{
	public ushort[] Data { get; set; }

	public int QuantityForBool => Quantity % 8 == 0 ? Quantity / 8 : Quantity / 8 + 1;
	public int QuantityForUshort => 2 * Quantity;

	public Request(byte[] arr)
	{
		Transaction = GetUshort(arr, 0);
		Protocol = GetUshort(arr, 2);
		Length = GetUshort(arr, 4);

		Identifier = arr[6];
		Function = arr[7];

		Address = GetUshort(arr, 8);
		Quantity = GetUshort(arr, 10);

		switch ((ModBusFunction)Function)
		{
			case ModBusFunction.ReadCoils:
			case ModBusFunction.ReadDiscreteInputs:
			case ModBusFunction.ReadHoldingRegisters:
			case ModBusFunction.ReadInputRegisters:
				Data = [];
				break;
			
			case ModBusFunction.WriteSingleCoil:
			case ModBusFunction.WriteSingleRegister:
				Data = [Quantity];
				break;
			
			case ModBusFunction.WriteMultipleCoils:
				Count = arr[12];
				Data = new ushort[Count % 2 == 0 ? Count / 2 : (Count / 2) + 1];
				Buffer.BlockCopy(arr, 13, Data, 0, Count);
				break;
			
			case ModBusFunction.WriteMultipleRegisters:
				Count = arr[12];
				Data = new ushort[Quantity];
				for (var i = 0; i < Quantity; i++)
					Data[i] = GetUshort(arr, 13 + i * 2);
				break;

			case ModBusFunction.EncapsulatedInterface:
			default:
				Data = [];
				break;
		}
	}

	private static ushort GetUshort(byte[] bytes, int offset)
	{
		var bs = new[] { bytes[offset + 1], bytes[offset] };
		return BitConverter.ToUInt16(bs);
	}
}

internal class Response : Transfer
{
	public byte[] Buffer { get; init; }

	public Response(Request req, ModBusErrorCode error)
	{
		Transaction = req.Transaction;
		Protocol = req.Protocol;
		Identifier = req.Identifier;
		Function = req.Function;
		Address = req.Address;
		Length = 3;
		Error = error;

		Buffer = new byte[9];
		BuildBuffer();
	}

	public Response(Request req, int quantity, int maxQuantity = 2000)
	{
		Transaction = req.Transaction;
		Protocol = req.Protocol;
		Identifier = req.Identifier;
		Function = req.Function;
		Address = req.Address;

		if (req.Address + 1 + req.Quantity > ushort.MaxValue)
			Error = ModBusErrorCode.IllegalDataAddress;
		if (req.Quantity == 0 || req.Quantity > maxQuantity)
			Error = ModBusErrorCode.IllegalDataValue;

		if (Error != ModBusErrorCode.NoError)
		{
			Length = 3;
			Buffer = new byte[9];
		}
		else
		{
			Count = (byte)quantity;
			Length = (ushort)(9 + quantity - 6);
			Buffer = new byte[9 + quantity];
		}

		BuildBuffer();
	}

	private void BuildBuffer()
	{
		// 0,1 트랜잭션
		SetUshort(0, Transaction);
		// 2,3 프로토콜
		SetUshort(2, Protocol);
		// 4,5 길이
		SetUshort(4, Length);
		// 6 아이디
		Buffer[6] = Identifier;

		if (Error == ModBusErrorCode.NoError)
		{
			// 7 기능
			Buffer[7] = Function;
			// 8 바이트 개수
			Buffer[8] = Count;
		}
		else
		{
			// 7 기능
			Buffer[7] = (byte)(Function + 128);
			// 8 바이트 개수 또는 오류
			Buffer[8] = (byte)Error;
		}
	}

	public void AddWriteResponse(ushort value)
	{
		// 8,9 주소
		SetUshort(8, Address);
		// 10,11 값
		SetUshort(10, value);
	}

	private void SetUshort(int offset, ushort value)
	{
		var bs = BitConverter.GetBytes(value);
		Buffer[offset + 0] = bs[1];
		Buffer[offset + 1] = bs[0];
	}
}

