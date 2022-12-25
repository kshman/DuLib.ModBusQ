using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Du.ModBusQ.Suppliment;
using Du.Properties;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 UDP 클라이언트
/// </summary>
public class ModBusClientUdp : ModBusClientIp
{
	private readonly ILogger? _lg;

	private uint _transactions;

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Udp;

	/// <inheritdoc/>
	public override bool IsConnected
	{
		get => true;
		protected set => throw new NotImplementedException();
	}

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="addr"></param>
	/// <param name="port"></param>
	/// <param name="logger"></param>
	public ModBusClientUdp(IPAddress addr, int port, ILogger? logger = null)
		: base(addr, port)
	{
		_lg = logger;
	}

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="address"></param>
	/// <param name="port"></param>
	/// <param name="logger"></param>
	public ModBusClientUdp(string address, int port, ILogger? logger = null)
		: this(IPAddress.Parse(address), port, logger) { }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="logger"></param>
	public ModBusClientUdp(ILogger? logger = null)
	{
		_lg = logger;
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
	}

	/// <inheritdoc/>
	public override void Open()
	{
		_lg?.MethodEnter("ModBusClientUdp.Open");

		InternalOpen();

		_lg?.MethodLeave("ModBusClientUdp.Open");
	}

	/// <inheritdoc cref="Open()"/>
	/// <seealso cref="ModBusClientUdp(IPAddress, int, ILogger?)"/>
	public void Open(IPAddress address, int port)
	{
		_lg?.MethodEnter("ModBusClientUdp.Open");

		Address = address;
		Port = port;
		InternalOpen();

		_lg?.MethodLeave("ModBusClientUdp.Open");
	}

	/// <inheritdoc cref="Open()"/>
	/// <seealso cref="ModBusClientUdp(string, int, ILogger?)"/>
	public void Open(string address, int port)
	{
		_lg?.MethodEnter("ModBusClientUdp.Open");

		Address = IPAddress.Parse(address);
		Port = port;
		InternalOpen();

		_lg?.MethodLeave("ModBusClientUdp.Open");
	}

	private void InternalOpen()
	{
		if (CanInvokeConnectionChanged)
			OnConnectionChanged(new ModBusConnectionChangedEventArgs(true));
	}

	/// <inheritdoc/>
	public override void Close()
	{
		_lg?.MethodEnter("ModBusClientUdp.Close");

		if (CanInvokeConnectionChanged)
			OnConnectionChanged(new ModBusConnectionChangedEventArgs(false));

		_lg?.MethodLeave("ModBusClientUdp.Close");
	}

	private byte[] InternalTransfer(byte[] send_buffer, ModBusFunction channel)
	{
		var udp = new UdpClient();
		var ep = new IPEndPoint(Address, Port);
		udp.Send(send_buffer, send_buffer.Length, ep);
		if (CanInvokeAfterWrite)
			OnAfterWrite(new ModBusReadWriteEventArgs(send_buffer));

		if (udp.Client.LocalEndPoint is not IPEndPoint lep)
			throw new UnreachableException(Resources.UdpUnreachableDestination);

		udp.Client.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
		var rep = new IPEndPoint(Address, lep.Port);
		var read_buffer = udp.Receive(ref rep);
		if (CanInvokeAfterRead)
			OnAfterRead(new ModBusReadWriteEventArgs(read_buffer));

		ThrowIf.ReadError(read_buffer, channel);

		return read_buffer;
	}

	/// <inheritdoc/>
	public override bool[] ReadCoils(int devId, int startAddress, int readCount)
	{
		_lg?.MethodEnter("ModBusClientUdp.ReadCoils");
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 2000)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, readCount, ModBusFunction.ReadCoils, 6);
			bs = InternalTransfer(bs, ModBusFunction.ReadCoils);

			var ret = new bool[readCount];
			for (var i = 0; i < readCount; i++)
			{
				var n = (int)bs[checked(9 + i / 8)];
				var m = (int)(Math.Pow(2, i % 8));
				ret[i] = Convert.ToBoolean((n & m) / m);
			}

			return ret;
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientUdp.ReadCoils");
		}
	}

	/// <inheritdoc/>
	public override bool[] ReadDiscreteInputs(int devId, int startAddress, int readCount)
	{
		_lg?.MethodEnter("ModBusClientUdp.ReadDiscreteInputs");
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 2000)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, readCount, ModBusFunction.ReadDiscreteInputs, 6);
			bs = InternalTransfer(bs, ModBusFunction.ReadDiscreteInputs);

			var ret = new bool[readCount];
			for (var i = 0; i < readCount; i++)
			{
				var n = (int)bs[checked(9 + i / 8)];
				var m = (int)(Math.Pow(2, i % 8));
				ret[i] = Convert.ToBoolean((n & m) / m);
			}

			return ret;
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientUdp.ReadDiscreteInputs");
		}
	}

	/// <inheritdoc/>
	public override int[] ReadHoldingRegisters(int devId, int startAddress, int readCount)
	{
		_lg?.MethodEnter("ModBusClientUdp.ReadHoldingRegisters");
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 125)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, readCount, ModBusFunction.ReadHoldingRegisters, 6);
			bs = InternalTransfer(bs, ModBusFunction.ReadHoldingRegisters);

			var ret = new int[readCount];
			var tb = new byte[2];
			for (var i = 0; i < readCount; i++)
			{
				tb[0] = bs[checked(9 + i * 2 + 1)];
				tb[1] = bs[checked(9 + i * 2)];
				ret[i] = BitConverter.ToInt16(tb, 0);
			}

			return ret;
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientUdp.ReadHoldingRegisters");
		}
	}

	/// <inheritdoc/>
	public override int[] ReadInputRegisters(int devId, int startAddress, int readCount)
	{
		_lg?.MethodEnter("ModBusClientUdp.ReadInputRegisters");
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 125)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, readCount, ModBusFunction.ReadInputRegisters, 6);
			bs = InternalTransfer(bs, ModBusFunction.ReadInputRegisters);

			var ret = new int[readCount];
			var tb = new byte[2];
			for (var i = 0; i < readCount; i++)
			{
				tb[0] = bs[checked(9 + i * 2 + 1)];
				tb[1] = bs[checked(9 + i * 2)];
				ret[i] = BitConverter.ToInt16(tb, 0);
			}

			return ret;
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientUdp.ReadInputRegisters");
		}
	}

	/// <inheritdoc/>
	public override void WriteSingleCoil(int devId, int startAddress, bool value)
	{
		_lg?.MethodEnter("ModBusClientUdp.WriteSingleCoil");
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);

			var cvalue = !value ? 0 : 65280;
			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, cvalue, ModBusFunction.WriteSingleCoil, 6);
			InternalTransfer(bs, ModBusFunction.WriteSingleCoil);
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientUdp.WriteSingleCoil");
		}
	}

	/// <inheritdoc/>
	public override void WriteSingleRegister(int devId, int startAddress, int value)
	{
		_lg?.MethodEnter("ModBusClientUdp.WriteSingleRegister");
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, value, ModBusFunction.WriteSingleRegister, 6);
			InternalTransfer(bs, ModBusFunction.WriteSingleRegister);
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientUdp.WriteSingleRegister");
		}
	}

	/// <inheritdoc/>
	public override void WriteMultipleCoils(int devId, int startAddress, bool[] values)
	{
		_lg?.MethodEnter("ModBusClientUdp.WriteMultipleCoils");
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);

			var count = (values.Length % 8) != 0
				? checked((byte)(values.Length / 8 + 1))
				: checked((byte)(values.Length / 8));
			var size = 14 + count + 1;

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, values.Length, ModBusFunction.WriteMultipleCoils, 7 + count, size);
			bs[12] = count;

			byte b = 0;
			for (var i = 0; i < values.Length; i++)
			{
				if (i % 8 == 0)
					b = 0;
				b = (byte)((!values[i] ? 0 : 1) << (i % 8) | b);
				bs[13 + i / 8] = b;
			}

			InternalTransfer(bs, ModBusFunction.WriteMultipleCoils);
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientUdp.WriteMultipleCoils");
		}
	}

	/// <inheritdoc/>
	public override void WriteMultipleRegisters(int devId, int startAddress, int[] values)
	{
		_lg?.MethodEnter("ModBusClientUdp.WriteMultipleRegisters");
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);

			var count = (byte)(values.Length * 2);
			var size = 14 + count + 1;

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, values.Length, ModBusFunction.WriteMultipleRegisters, 7 + count, size);
			bs[12] = count;

			for (var i = 0; i < values.Length; i++)
			{
				var tb = BitConverter.GetBytes(values[i]);
				bs[13 + i * 2] = tb[1];
				bs[14 + i * 2] = tb[0];
			}

			InternalTransfer(bs, ModBusFunction.WriteMultipleRegisters);
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientUdp.WriteMultipleRegisters");
		}
	}
}
