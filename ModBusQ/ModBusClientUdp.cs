using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Du.ModBusQ.Supplement;
using Du.Properties;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 UDP 클라이언트
/// </summary>
public class ModBusClientUdp : ModBusClientIp
{
	private readonly ILogger? _logger;

	private uint _transactions;

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Udp;

	/// <inheritdoc/>
	public override bool IsConnected { get; protected set; } = true;

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="addr">연결할 대상의 IP 주소입니다.</param>
	/// <param name="port">연결할 대상의 포트 번호입니다.</param>
	/// <param name="logger">로깅을 위한 <see cref="ILogger"/> 인스턴스(선택).</param>
	public ModBusClientUdp(IPAddress addr, int port, ILogger? logger = null)
		: base(addr, port)
	{
		_logger = logger;
	}

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="address">연결할 대상의 IP 주소 문자열(예: "192.168.0.10").</param>
	/// <param name="port">연결할 대상의 포트 번호입니다.</param>
	/// <param name="logger">로깅을 위한 <see cref="ILogger"/> 인스턴스(선택).</param>
	public ModBusClientUdp(string address, int port, ILogger? logger = null)
		: this(IPAddress.Parse(address), port, logger)
	{
	}

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="logger">로깅을 위한 <see cref="ILogger"/> 인스턴스(선택).</param>
	public ModBusClientUdp(ILogger? logger = null)
	{
		_logger = logger;
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
	}

	/// <inheritdoc/>
	public override void Open()
	{
		_logger?.MethodEnter(MethodNameOpen);

		InternalOpen();

		_logger?.MethodLeave(MethodNameOpen);
	}

	/// <inheritdoc cref="Open()"/>
	/// <seealso cref="ModBusClientUdp(IPAddress, int, ILogger?)"/>
	public void Open(IPAddress address, int port)
	{
		_logger?.MethodEnter(MethodNameOpen);

		Address = address;
		Port = port;
		InternalOpen();

		_logger?.MethodLeave(MethodNameOpen);
	}

	/// <inheritdoc cref="Open()"/>
	/// <seealso cref="ModBusClientUdp(string, int, ILogger?)"/>
	public void Open(string address, int port)
	{
		_logger?.MethodEnter(MethodNameOpen);

		Address = IPAddress.Parse(address);
		Port = port;
		InternalOpen();

		_logger?.MethodLeave(MethodNameOpen);
	}

	private void InternalOpen()
	{
		if (CanInvokeConnectionChanged)
			OnConnectionChanged(new ModBusStateChangedEventArgs(true));
	}

	/// <inheritdoc/>
	public override void Close()
	{
		_logger?.MethodEnter(MethodNameClose);

		if (CanInvokeConnectionChanged)
			OnConnectionChanged(new ModBusStateChangedEventArgs(false));

		_logger?.MethodLeave(MethodNameClose);
	}

	private byte[] InternalTransfer(byte[] buffer, ModBusFunction channel)
	{
		using var udp = new UdpClient();
		var ep = new IPEndPoint(Address, Port);
		udp.Send(buffer, buffer.Length, ep);
		if (CanInvokeAfterWrite)
			OnAfterWrite(new ModBusBufferedEventArgs(buffer));

		if (udp.Client.LocalEndPoint is not IPEndPoint lep)
			throw new UnreachableException(Resources.UdpUnreachableDestination);

		udp.Client.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
		var rep = new IPEndPoint(Address, lep.Port);
		var readBuffer = udp.Receive(ref rep);
		if (CanInvokeAfterRead)
			OnAfterRead(new ModBusBufferedEventArgs(readBuffer));

		ThrowIf.ReadError(readBuffer, channel);

		return readBuffer;
	}

	/// <inheritdoc/>
	public override bool[] ReadCoils(int devId, int startAddress, int readCount)
	{
		_logger?.MethodEnter(MethodNameReadCoils);
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
				var n = (int)bs[checked(9 + (i / 8))];
				var m = (int)(Math.Pow(2, i % 8));
				ret[i] = Convert.ToBoolean((n & m) / m);
			}

			return ret;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadCoils);
		}
	}

	/// <inheritdoc/>
	public override bool[] ReadDiscreteInputs(int devId, int startAddress, int readCount)
	{
		_logger?.MethodEnter(MethodNameReadDiscreteInputs);
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
				var n = (int)bs[checked(9 + (i / 8))];
				var m = (int)(Math.Pow(2, i % 8));
				ret[i] = Convert.ToBoolean((n & m) / m);
			}

			return ret;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadDiscreteInputs);
		}
	}

	/// <inheritdoc/>
	public override short[] ReadHoldingRegisters(int devId, int startAddress, int readCount)
	{
		_logger?.MethodEnter(MethodNameReadHoldingRegisters);
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 125)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, readCount, ModBusFunction.ReadHoldingRegisters, 6);
			bs = InternalTransfer(bs, ModBusFunction.ReadHoldingRegisters);

			var ret = new short[readCount];
			var span = bs.AsSpan(9);
			for (var i = 0; i < readCount; i++)
				BinaryPrimitives.TryReadInt16BigEndian(span.Slice(i * 2, 2), out ret[i]);

			return ret;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadHoldingRegisters);
		}
	}

	/// <inheritdoc/>
	public override short[] ReadInputRegisters(int devId, int startAddress, int readCount)
	{
		_logger?.MethodEnter(MethodNameReadInputRegisters);
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 125)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, readCount, ModBusFunction.ReadInputRegisters, 6);
			bs = InternalTransfer(bs, ModBusFunction.ReadInputRegisters);

			var ret = new short[readCount];
			var span = bs.AsSpan(9);
			for (var i = 0; i < readCount; i++)
				BinaryPrimitives.TryReadInt16BigEndian(span.Slice(i * 2, 2), out ret[i]);

			return ret;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadInputRegisters);
		}
	}

	/// <inheritdoc/>
	public override byte[] ReadRawHoldingRegisters(int devId, int startAddress, int readCount)
	{
		_logger?.MethodEnter(MethodNameReadRawHoldingRegisters);
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 125)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, readCount, ModBusFunction.ReadHoldingRegisters, 6);
			bs = InternalTransfer(bs, ModBusFunction.ReadHoldingRegisters);

			var ret = new byte[readCount];
			Array.Copy(bs, 9, ret, 0, readCount);

			return ret;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadRawHoldingRegisters);
		}
	}

	/// <inheritdoc/>
	public override byte[] ReadRawInputRegisters(int devId, int startAddress, int readCount)
	{
		_logger?.MethodEnter(MethodNameReadRawInputRegisters);
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 125)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, readCount, ModBusFunction.ReadInputRegisters, 6);
			bs = InternalTransfer(bs, ModBusFunction.ReadInputRegisters);

			var ret = new byte[readCount];
			Array.Copy(bs, 9, ret, 0, readCount);

			return ret;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadRawInputRegisters);
		}
	}

	/// <inheritdoc/>
	public override void WriteSingleCoil(int devId, int startAddress, bool value)
	{
		_logger?.MethodEnter(MethodNameWriteSingleCoil);
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);

			var converted = !value ? 0 : 65280;
			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, converted, ModBusFunction.WriteSingleCoil, 6);
			InternalTransfer(bs, ModBusFunction.WriteSingleCoil);
		}
		finally
		{
			_logger?.MethodLeave(MethodNameWriteSingleCoil);
		}
	}

	/// <inheritdoc/>
	public override void WriteSingleRegister(int devId, int startAddress, short value)
	{
		_logger?.MethodEnter(MethodNameWriteSingleRegister);
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
			_logger?.MethodLeave(MethodNameWriteSingleRegister);
		}
	}

	/// <inheritdoc/>
	public override void WriteMultipleCoils(int devId, int startAddress, bool[] values)
	{
		_logger?.MethodEnter(MethodNameWriteMultipleCoils);
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);

			var count = (values.Length % 8) != 0
				? checked((byte)((values.Length / 8) + 1))
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
				bs[13 + (i / 8)] = b;
			}

			InternalTransfer(bs, ModBusFunction.WriteMultipleCoils);
		}
		finally
		{
			_logger?.MethodLeave(MethodNameWriteMultipleCoils);
		}
	}

	/// <inheritdoc/>
	public override void WriteMultipleRegisters(int devId, int startAddress, short[] values)
	{
		_logger?.MethodEnter(MethodNameWriteMultipleRegisters);
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
				bs[13 + (i * 2)] = tb[1];
				bs[14 + (i * 2)] = tb[0];
			}

			InternalTransfer(bs, ModBusFunction.WriteMultipleRegisters);
		}
		finally
		{
			_logger?.MethodLeave(MethodNameWriteMultipleRegisters);
		}
	}

	// 메소드 이름
	private const string MethodNameOpen = "ModBusClientUdp.Open";
	private const string MethodNameClose = "ModBusClientUdp.Close";
	private const string MethodNameReadCoils = "ModBusClientUdp.ReadCoils";
	private const string MethodNameReadDiscreteInputs = "ModBusClientUdp.ReadDiscreteInputs";
	private const string MethodNameReadHoldingRegisters = "ModBusClientUdp.ReadHoldingRegisters";
	private const string MethodNameReadInputRegisters = "ModBusClientUdp.ReadInputRegisters";
	private const string MethodNameReadRawHoldingRegisters = "ModBusClientUdp.ReadRawHoldingRegisters";
	private const string MethodNameReadRawInputRegisters = "ModBusClientUdp.ReadRawInputRegisters";
	private const string MethodNameWriteSingleCoil = "ModBusClientUdp.WriteSingleCoil";
	private const string MethodNameWriteSingleRegister = "ModBusClientUdp.WriteSingleRegister";
	private const string MethodNameWriteMultipleCoils = "ModBusClientUdp.WriteMultipleCoils";
	private const string MethodNameWriteMultipleRegisters = "ModBusClientUdp.WriteMultipleRegisters";
}
