namespace Du.ModBusQ.Suppliment;

public class Transfer
{
	public DateTime Issue { get; set; }

	public ushort Transaction { get; set; }
	public ushort Protocol { get; set; }
	public ushort Length { get; set; }

	public byte Identifier { get; set; }
	public byte Function { get; set; }

	public ushort Address { get; set; }
	public ushort Quantity { get; set; }
	public byte Count { get; set; }

	public ModBusErrorCode Error { get; set; }

	public ushort Crc { get; set; }

	protected Transfer()
		: this(DateTime.Now) { }

	protected Transfer(DateTime issue)
	{
		Issue = issue;
	}
}

public class Request : Transfer
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
		if (Function <= (byte)ModBusFunction.ReadInputRegisters)
		{
			Quantity = GetUshort(arr, 10);
			Data = Array.Empty<ushort>();
		}
		else if (Function == (byte)ModBusFunction.WriteSingleCoil ||
			Function == (byte)ModBusFunction.WriteSingleRegister)
		{
			Data = new ushort[1];
			Data[0] = GetUshort(arr, 10);
		}
		else if (Function == (byte)ModBusFunction.WriteMultipleCoils)
		{
			Quantity = GetUshort(arr, 10);
			Count = arr[12];
			Data = new ushort[Count % 2 == 0 ? Count / 2 : Count / 2 + 1];
			Buffer.BlockCopy(arr, 13, Data, 0, Count);
		}
		else if (Function == (byte)ModBusFunction.WriteMultipleRegisters)
		{
			Quantity = GetUshort(arr, 10);
			Count = arr[12];
			Data = new ushort[Quantity];
			for (var i = 0; i < Quantity; i++)
				Data[i] = GetUshort(arr, 13 + i * 2);
		}
		else
		{
			Data = Array.Empty<ushort>();
		}
	}

	private static ushort GetUshort(byte[] bytes, int offset)
	{
		var bs = new[] { bytes[offset + 1], bytes[offset] };
		return BitConverter.ToUInt16(bs);
	}
}

public class Response : Transfer
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
