using System.Net;
using System.Net.Sockets;
using Du.ModBusQ.Suppliment;
using Du.Properties;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 TCP 클라이언트
/// </summary>
public class ModBusClientTcp : ModBusClientIp
{
	private readonly ILogger? _lg;

	private TcpClient? _conn;
	private NetworkStream? _ntst; // TCP stream
	private bool _is_conn;

	private uint _transactions;

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Tcp;

	/// <inheritdoc/>
	public override bool IsConnected
	{
		get => _conn != null && _ntst != null && _is_conn;
		protected set => _is_conn = value;
	}

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="addr"></param>
	/// <param name="port"></param>
	/// <param name="logger"></param>
	public ModBusClientTcp(IPAddress addr, int port, ILogger? logger = null)
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
	public ModBusClientTcp(string address, int port, ILogger? logger = null)
		: this(IPAddress.Parse(address), port, logger) { }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="logger"></param>
	public ModBusClientTcp(ILogger? logger = null)
	{
		_lg = logger;
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		if (!disposing)
			return;

		_conn?.Dispose();
		_ntst?.Dispose();
	}

	/// <inheritdoc/>
	public override void Open()
	{

		_lg?.MethodEnter("ModBusClientTcp.Open");

		InternalOpen();

		_lg?.MethodLeave("ModBusClientTcp.Open");
	}

	/// <inheritdoc cref="Open()"/>
	/// <seealso cref="ModBusClientTcp(IPAddress, int, ILogger?)"/>
	public void Open(IPAddress address, int port)
	{
		_lg?.MethodEnter("ModBusClientTcp.Open");

		Address = address;
		Port = port;
		InternalOpen();

		_lg?.MethodLeave("ModBusClientTcp.Open");
	}

	/// <inheritdoc cref="Open()"/>
	/// <seealso cref="ModBusClientTcp(string, int, ILogger?)"/>
	public void Open(string address, int port)
	{
		_lg?.MethodEnter("ModBusClientTcp.Open");

		Address = IPAddress.Parse(address);
		Port = port;
		InternalOpen();

		_lg?.MethodLeave("ModBusClientTcp.Open");
	}

	//
	private void InternalOpen()
	{
		_conn = new TcpClient();

		var res = _conn.BeginConnect(Address, Port, null, null);
		if (!res.AsyncWaitHandle.WaitOne(ConnectionTimeout))
			throw new ModBusConnectionException(Resources.ConnectionTimeout);

		try
		{
			_conn.EndConnect(res);
			_ntst = _conn.GetStream();
			_ntst.ReadTimeout = (int)ReceiveTimeout.TotalMilliseconds;
		}
		catch (Exception ex)
		{
			throw new ModBusConnectionException(Resources.ConnectionFail, ex);
		}

		IsConnected = true;
		if (CanInvokeConnectionChanged)
			OnConnectionChanged(new ModBusStateChangedEventArgs(true));
	}

	/// <inheritdoc/>
	public override void Close()
	{
		_lg?.MethodEnter("ModBusClientTcp.Close");

		_ntst?.Close();
		_conn?.Close();

		IsConnected = false;
		if (CanInvokeConnectionChanged)
			OnConnectionChanged(new ModBusStateChangedEventArgs(false));

		_lg?.MethodLeave("ModBusClientTcp.Close");
	}

	//
	private byte[] InternalTransfer(byte[] send_buffer, ModBusFunction function, int size = 2100)
	{
#pragma warning disable CS8604
		try
		{
			_ntst.StreamWrite(send_buffer);
			if (CanInvokeAfterWrite)
				OnAfterWrite(new ModBusBufferedEventArgs(send_buffer));

			var read_buffer = _ntst.StreamRead(size, out var len);
			if (CanInvokeAfterRead)
			{
				var copy = new byte[len];
				Array.Copy(read_buffer, 0, copy, 0, len);
				OnAfterRead(new ModBusBufferedEventArgs(copy));
			}

			ThrowIf.ReadError(read_buffer, function);

			return read_buffer;
		}
		catch (Exception ex)
		{
			// 뭐지 뭐가 문제지
			_is_conn = false;
			_lg?.ProbablyDisconnected("ModBusClientTcp.InternalTransfer", ex.Message);
			throw;
		}
#pragma warning restore CS8604
	}

	/// <inheritdoc/>
	public override bool[] ReadCoils(int devId, int startAddress, int readCount)
	{
		_lg?.MethodEnter("ModBusClientTcp.ReadCoils");
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 2000)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);
			if (_conn == null || !_conn.Client.Connected || _ntst == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			_lg?.MethodLeave("ModBusClientTcp.ReadCoils");
		}
	}

	/// <inheritdoc/>
	public override bool[] ReadDiscreteInputs(int devId, int startAddress, int readCount)
	{
		_lg?.MethodEnter("ModBusClientTcp.ReadDiscreteInputs");
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 2000)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);
			if (_conn == null || !_conn.Client.Connected || _ntst == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			_lg?.MethodLeave("ModBusClientTcp.ReadDiscreteInputs");
		}
	}

	/// <inheritdoc/>
	public override int[] ReadHoldingRegisters(int devId, int startAddress, int readCount)
	{
		_lg?.MethodEnter("ModBusClientTcp.ReadHoldingRegisters");
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 125)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);
			if (_conn == null || !_conn.Client.Connected || _ntst == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			_lg?.MethodLeave("ModBusClientTcp.ReadHoldingRegisters");
		}
	}

	/// <inheritdoc/>
	public override int[] ReadInputRegisters(int devId, int startAddress, int readCount)
	{
		_lg?.MethodEnter("ModBusClientTcp.ReadInputRegisters");
		try
		{
			if (startAddress < 0 || (startAddress + readCount) > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (readCount is < 0 or > 125)
				throw new ArgumentOutOfRangeException(nameof(readCount), Resources.CountOutOfRange);
			if (_conn == null || !_conn.Client.Connected || _ntst == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			_lg?.MethodLeave("ModBusClientTcp.ReadInputRegisters");
		}
	}

	/// <inheritdoc/>
	public override void WriteSingleCoil(int devId, int startAddress, bool value)
	{
		_lg?.MethodEnter("ModBusClientTcp.WriteSingleCoil");
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (_conn == null || !_conn.Client.Connected || _ntst == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

			var cvalue = !value ? 0 : 65280;
			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, cvalue, ModBusFunction.WriteSingleCoil, 6);

			InternalTransfer(bs, ModBusFunction.WriteSingleCoil);
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientTcp.WriteSingleCoil");
		}
	}

	/// <inheritdoc/>
	public override void WriteSingleRegister(int devId, int startAddress, int value)
	{
		_lg?.MethodEnter("ModBusClientTcp.WriteSingleRegister");
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (_conn == null || !_conn.Client.Connected || _ntst == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

			var bs = DmTcp.BuildTcpBuffer(devId, checked(++_transactions),
				startAddress, value, ModBusFunction.WriteSingleRegister, 6);

			InternalTransfer(bs, ModBusFunction.WriteSingleRegister);
		}
		finally
		{
			_lg?.MethodLeave("ModBusClientTcp.WriteSingleRegister");
		}
	}

	/// <inheritdoc/>
	public override void WriteMultipleCoils(int devId, int startAddress, bool[] values)
	{
		_lg?.MethodEnter("ModBusClientTcp.WriteMultipleCoils");
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (_conn == null || !_conn.Client.Connected || _ntst == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			_lg?.MethodLeave("ModBusClientTcp.WriteMultipleCoils");
		}
	}

	/// <inheritdoc/>
	public override void WriteMultipleRegisters(int devId, int startAddress, int[] values)
	{
		_lg?.MethodEnter("ModBusClientTcp.WriteMultipleRegisters");
		try
		{
			if (startAddress is < 0 or > 65535)
				throw new ArgumentOutOfRangeException(nameof(startAddress), Resources.AddressOutOfRange);
			if (_conn == null || !_conn.Client.Connected || _ntst == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			_lg?.MethodLeave("ModBusClientTcp.WriteMultipleRegisters");
		}
	}
}
