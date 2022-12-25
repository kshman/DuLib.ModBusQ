namespace Du.ModBusQ.Suppliment;

internal class TransferDesc
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

	public byte Exception { get; set; }
	public byte Error { get; set; }

	public ushort Crc { get; set; }

	public ushort[]? Coils { get; set; }
	public ushort[]? Registers { get; set; }

	protected TransferDesc(byte[] arr)
		: this(DateTime.Now, arr) { }

	protected TransferDesc(DateTime issue, byte[] arr)
	{
		Issue = issue;

		Transaction = GetUshort(arr, 0);
		Protocol = GetUshort(arr, 2);
		Length = GetUshort(arr, 4);

		Identifier = arr[6];
		Function = arr[7];

		Address = GetUshort(arr, 8);
		if (Function <= (byte)ModBusFunction.ReadInputRegisters)
			Quantity = GetUshort(arr, 10);
		else if (Function == (byte)ModBusFunction.WriteSingleCoil)
		{
			Coils = new ushort[1];
			Coils[0] = GetUshort(arr, 10);
		}
		else if (Function==(byte)ModBusFunction.WriteSingleRegister)
		{
			Registers = new ushort[1];
			Registers[0] = GetUshort(arr, 10);
		}
		else if (Function==(byte)ModBusFunction.WriteMultipleCoils)
		{
			//
		}
	}

	internal static ushort GetUshort(byte[] bytes, int offset)
	{
		var bs = new[] { bytes[offset + 1], bytes[offset] };
		return BitConverter.ToUInt16(bs);
	}
}

internal class Request : TransferDesc
{

	//
	public Request(byte[] arr)
		: base(arr)
	{

	}
}

internal class Response : TransferDesc
{
	//
	public Response(byte[] arr)
		: base(arr)
	{

	}
}
