using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Du.ModBusQ.Supplement;
using Du.Properties;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 TCP 클라이언트
/// </summary>
public class ModBusClientTcp : ModBusClientIp
{
	/// <summary>로깅을 위한 <see cref="ILogger"/> 인스턴스(선택).</summary>
	private readonly ILogger? _logger;

	/// <summary>TCP 연결을 관리하는 TcpClient 인스턴스입니다.</summary>
	private TcpClient? _conn;
	/// <summary>연결된 TCP의 네트워크 스트림입니다.</summary>
	private NetworkStream? _stream;

	/// <summary>트랜잭션 식별자 카운터입니다.</summary>
	private uint _transactions;

	/// <inheritdoc/>
	public override ModBusConnection ConnectionType => ModBusConnection.Tcp;

	/// <inheritdoc/>
	public override bool IsConnected
	{
		get => _conn != null && _stream != null && field;
		protected set;
	}

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="addr">연결할 대상의 IP 주소입니다.</param>
	/// <param name="port">연결할 대상의 포트 번호입니다.</param>
	/// <param name="logger">로깅을 위한 <see cref="ILogger"/> 인스턴스(선택).</param>
	public ModBusClientTcp(IPAddress addr, int port, ILogger? logger = null)
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
	public ModBusClientTcp(string address, int port, ILogger? logger = null)
		: this(IPAddress.Parse(address), port, logger)
	{
	}

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="logger">로깅을 위한 <see cref="ILogger"/> 인스턴스(선택).</param>
	public ModBusClientTcp(ILogger? logger = null)
	{
		_logger = logger;
	}

	/// <inheritdoc/>
	/// <param name="disposing">관리되는 리소스를 해제할지 여부를 나타냅니다. (true면 관리된 리소스를 해제합니다.)</param>
	protected override void Dispose(bool disposing)
	{
		if (!disposing)
			return;

		_conn?.Dispose();
		_stream?.Dispose();
	}

	/// <inheritdoc/>
	public override void Open()
	{
		_logger?.MethodEnter(MethodNameOpen);

		InternalOpen();

		_logger?.MethodLeave(MethodNameOpen);
	}

	/// <inheritdoc cref="Open()"/>
	/// <seealso cref="ModBusClientTcp(IPAddress, int, ILogger?)"/>
	public void Open(IPAddress address, int port)
	{
		_logger?.MethodEnter(MethodNameOpen);

		Address = address;
		Port = port;
		InternalOpen();

		_logger?.MethodLeave(MethodNameOpen);
	}

	/// <inheritdoc cref="Open()"/>
	/// <seealso cref="ModBusClientTcp(string, int, ILogger?)"/>
	public void Open(string address, int port)
	{
		_logger?.MethodEnter(MethodNameOpen);

		Address = IPAddress.Parse(address);
		Port = port;
		InternalOpen();

		_logger?.MethodLeave(MethodNameOpen);
	}

	/// <summary>
	/// 실제 연결을 여는 내부 메서드입니다.
	/// </summary>
	private void InternalOpen()
	{
		_conn = new TcpClient();

		var res = _conn.BeginConnect(Address, Port, null, null);
		if (!res.AsyncWaitHandle.WaitOne(ConnectionTimeout))
			throw new ModBusConnectionException(ModBusErrorCode.ConnectionTimeout, Resources.ConnectionTimeout);

		try
		{
			_conn.EndConnect(res);
			_stream = _conn.GetStream();
			_stream.ReadTimeout = (int)ReceiveTimeout.TotalMilliseconds;
		}
		catch (Exception ex)
		{
			throw new ModBusConnectionException(ModBusErrorCode.ConnectionFail, Resources.ConnectionFail, ex);
		}

		IsConnected = true;
		if (CanInvokeConnectionChanged)
			OnConnectionChanged(new ModBusStateChangedEventArgs(true));
	}

	/// <inheritdoc/>
	public override void Close()
	{
		_logger?.MethodEnter(MethodNameClose);
		InternalClose();
		_logger?.MethodLeave(MethodNameClose);
	}

	/// <summary>
	/// 실제 연결을 닫는 내부 메서드입니다.
	/// </summary>
	private void InternalClose()
	{
		try
		{
			_stream?.Close();
			_conn?.Close();
		}
		catch
		{
			// 예외가 발생했는데, 어짜피 끊는거니깐 무시합시다
		}

		IsConnected = false;
		if (CanInvokeConnectionChanged)
			OnConnectionChanged(new ModBusStateChangedEventArgs(false));
	}

	/// <summary>
	/// 요청 바이트 배열을 전송하고 서버로부터 응답을 수신하여 반환합니다.
	/// 내부적으로 쓰기/읽기 단계를 분리하여 처리합니다.
	/// </summary>
	/// <param name="buffer">전송할 요청 바이트 배열입니다.</param>
	/// <param name="function">요청에 해당하는 ModBus 기능 코드입니다. 응답 검사에 사용됩니다.</param>
	/// <param name="size">읽기 시 최대 버퍼 크기입니다.</param>
	/// <returns>서버로부터 수신한 응답 바이트 배열입니다.</returns>
	private byte[] InternalTransfer(byte[] buffer, ModBusFunction function, int size = 2100)
	{
		if (_stream == null)
			throw new ModBusConnectionException(Resources.GetConnectionFirst);
		if (buffer.Length == 0)
			return [];
		if (buffer.Length > 2600)
			throw new ArgumentOutOfRangeException(nameof(buffer), Resources.BufferTooLarge);

		InternalTransferOnWrite(_stream, buffer);
		return InternalTransferOnRead(_stream, function, size);
	}

	// InternalTransfer에서 송신 부분
	private void InternalTransferOnWrite(NetworkStream stream, byte[] buffer)
	{
		try
		{
			stream.StreamWrite(buffer);
		}
		catch (Exception ex)
		{
			InternalClose();

			// 접속 끊김
			if (ex is OperationCanceledException or ObjectDisposedException or SocketException ||
				ex.InnerException is SocketException)
			{
				_logger?.WriteError(MethodNameInternalTransfer, ex.Message);
				throw new ModBusException(ModBusErrorCode.Disconnected, ex.Message);
			}

			// 뭐지 뭐가 문제지
			_logger?.ProbablyDisconnected(MethodNameInternalTransfer, ex.Message);
			throw;
		}

		if (CanInvokeAfterWrite)
			OnAfterWrite(new ModBusBufferedEventArgs(buffer));
	}

	// InternalTransfer에서 수신 부분
	private byte[] InternalTransferOnRead(NetworkStream stream, ModBusFunction function, int size)
	{
		byte[]? buffer;
		int len;

		try
		{
			buffer = stream.StreamRead(size, out len);
			if (buffer.Length == 0)
				throw new OperationCanceledException("0바이트 수신, 연결이 끊어진 것으로 간주합니다.");
		}
		catch (Exception ex)
		{
			// 접속 끊김
			if (ex is OperationCanceledException or ObjectDisposedException or SocketException ||
				ex.InnerException is SocketException)
			{
				InternalClose();
				_logger?.ReadError(MethodNameInternalTransfer, ex.Message);
				throw new ModBusException(ModBusErrorCode.Disconnected, ex.Message);
			}

			// 뭐지 뭐가 문제지? 아무튼 끊긴거로 처리하지 않음
			_logger?.ReadError(MethodNameInternalTransfer, ex.Message);
			throw;
		}

		if (CanInvokeAfterRead)
		{
			var copy = new byte[len];
			Array.Copy(buffer, 0, copy, 0, len);
			OnAfterRead(new ModBusBufferedEventArgs(copy));
		}

		ThrowIf.ReadError(buffer, function);

		return buffer;
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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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
			if (!(_conn?.Client.Connected ?? false) || _stream == null)
				throw new ModBusConnectionException(Resources.GetConnectionFirst);

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

	// 메소드 이름이 여러번 나와서 상수로 빼봄
	private const string MethodNameOpen = "ModBusClientTcp.Open";
	private const string MethodNameClose = "ModBusClientTcp.Close";
	private const string MethodNameReadCoils = "ModBusClientTcp.ReadCoils";
	private const string MethodNameReadDiscreteInputs = "ModBusClientTcp.ReadDiscreteInputs";
	private const string MethodNameReadHoldingRegisters = "ModBusClientTcp.ReadHoldingRegisters";
	private const string MethodNameReadInputRegisters = "ModBusClientTcp.ReadInputRegisters";
	private const string MethodNameReadRawHoldingRegisters = "ModBusClientTcp.ReadRawHoldingRegisters";
	private const string MethodNameReadRawInputRegisters = "ModBusClientTcp.ReadRawInputRegisters";
	private const string MethodNameWriteSingleCoil = "ModBusClientTcp.WriteSingleCoil";
	private const string MethodNameWriteSingleRegister = "ModBusClientTcp.WriteSingleRegister";
	private const string MethodNameWriteMultipleCoils = "ModBusClientTcp.WriteMultipleCoils";
	private const string MethodNameWriteMultipleRegisters = "ModBusClientTcp.WriteMultipleRegisters";
	private const string MethodNameInternalTransfer = "ModBusClientTcp.InternalTransfer";
}
