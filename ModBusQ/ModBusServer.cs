using System.Collections.Concurrent;
using Du.ModBusQ.Supplement;
using Du.Properties;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 서버 기본
/// </summary>
public abstract class ModBusServer : IModBusServer
{
	/// <summary>
	/// 로깅에 사용되는 <see cref="ILogger"/> 인스턴스입니다.
	/// </summary>
	protected readonly ILogger? _logger;

	private readonly ConcurrentDictionary<int, Device> _devices = new();
	private int _funcEnable = int.MaxValue;

	/// <inheritdoc/>
	public abstract ModBusConnection ConnectionType { get; }

	/// <inheritdoc/>
	public virtual bool IsRunning { get; protected set; }

	/// <inheritdoc/>
	public DateTime StartTime { get; protected set; }

	/// <inheritdoc/>
	public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(3);

	/// <inheritdoc/>
	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(5);

	/// <inheritdoc/>
	public ModBusTraceMasks TraceMask { get; set; } = ModBusTraceMasks.None;

	/// <summary>
	/// 코일(디지털 출력) 값이 변경되었을 때 발생하는 이벤트입니다.
	/// </summary>
	public event EventHandler<ModBusAddressEventArgs>? CoilsChanged;

	/// <summary>
	/// 홀딩 레지스터(아날로그 출력) 값이 변경되었을 때 발생하는 이벤트입니다.
	/// </summary>
	public event EventHandler<ModBusAddressEventArgs>? HoldingRegistersChanged;

	/// <summary>
	/// 클라이언트가 서버에 연결되었을 때 발생하는 이벤트입니다.
	/// </summary>
	public event EventHandler<ModBusClientEventArgs>? ClientConnected;

	/// <summary>
	/// 클라이언트 연결이 종료되었을 때 발생하는 이벤트입니다.
	/// </summary>
	public event EventHandler<ModBusClientEventArgs>? ClientDisconnected;

	/// <summary>
	/// 코일 변경 이벤트를 호출합니다.
	/// </summary>
	/// <param name="e">이벤트 인자</param>
	protected void OnCoilsChanged(ModBusAddressEventArgs e) => CoilsChanged?.Invoke(this, e);

	/// <summary>
	/// 홀딩 레지스터 변경 이벤트를 호출합니다.
	/// </summary>
	/// <param name="e">이벤트 인자</param>
	protected void OnHoldingRegistersChanged(ModBusAddressEventArgs e) => HoldingRegistersChanged?.Invoke(this, e);

	/// <summary>
	/// 클라이언트 연결 이벤트를 호출합니다.
	/// </summary>
	/// <param name="e">이벤트 인자</param>
	protected void OnClientConnected(ModBusClientEventArgs e) => ClientConnected?.Invoke(this, e);

	/// <summary>
	/// 클라이언트 연결 종료 이벤트를 호출합니다.
	/// </summary>
	/// <param name="e">이벤트 인자</param>
	protected void OnClientDisconnected(ModBusClientEventArgs e) => ClientDisconnected?.Invoke(this, e);

	/// <summary>
	/// ModBus 서버의 기본 인스턴스를 초기화합니다. 서브클래스는 이 생성자를 호출하여
	/// 로거 인스턴스를 전달할 수 있습니다.
	/// </summary>
	/// <param name="logger">로깅을 위해 사용할 선택적 <see cref="Microsoft.Extensions.Logging.ILogger"/> 인스턴스입니다. null을 허용합니다.</param>
	protected ModBusServer(ILogger? logger)
	{
		_logger = logger;

		// 디바이스 두개 만들어 놓기
		AddDevice(0);
		AddDevice(1);
	}

	/// <summary>
	/// 디스트럭터
	/// </summary>
	~ModBusServer()
	{
		Dispose(false);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Dispose 패턴의 내부 구현입니다. 서브클래스는 관리/비관리 리소스 해제를 이 메서드에서 수행해야 합니다.
	/// </summary>
	/// <param name="disposing">관리되는 리소스를 해제할지 여부를 나타냅니다. true이면 관리 리소스도 해제합니다.</param>
	protected abstract void Dispose(bool disposing);

	/// <summary>
	/// 시작
	/// </summary>
	public abstract void Start();

	/// <summary>
	/// 정지
	/// </summary>
	public abstract void Stop();

	/// <summary>
	/// 특정 ModBus 기능 코드를 사용 가능 또는 사용 불가로 설정합니다.
	/// </summary>
	/// <param name="function">설정할 ModBus 기능 코드입니다.</param>
	/// <param name="value">해당 기능을 사용하려면 true, 사용하지 않으려면 false를 전달합니다.</param>
	public void SetFunctionEnable(ModBusFunction function, bool value)
	{
		if ((int)function > 30)
			return;

		if (value)
			_funcEnable |= 1 << (int)function;
		else
			_funcEnable &= ~1 << (int)function;
	}

	/// <summary>
	/// 특정 ModBus 기능 코드가 현재 사용 가능한지 여부를 반환합니다.
	/// </summary>
	/// <param name="function">확인할 ModBus 기능 코드입니다.</param>
	/// <returns>해당 기능이 사용 가능하면 true, 그렇지 않으면 false를 반환합니다.</returns>
	public bool IsFunctionEnable(ModBusFunction function)
	{
		return (_funcEnable & (1 << (int)function)) != 0;
	}

	/// <summary>
	/// 요청 바이트 배열을 처리하고 응답 바이트 배열을 반환합니다.
	/// </summary>
	/// <param name="buffer">수신된 요청 바이트 배열입니다.</param>
	/// <returns>처리된 응답을 담은 바이트 배열입니다.</returns>
	protected byte[] HandleRequestBuffer(byte[] buffer)
	{
		_logger?.MethodEnter(MethodNameHandleRequestBuffer);
		try
		{
			var rsp = InternalHandleRequest(new Request(buffer));
			return rsp.Buffer;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameHandleRequestBuffer);
		}
	}

	/// <summary>
	/// 요청 바이트 배열을 처리하고 내부 처리 결과 오브젝트를 반환합니다.
	/// </summary>
	/// <param name="buffer">처리할 요청 바이트 배열입니다.</param>
	/// <returns>요청 처리 결과를 나타내는 오브젝트(주로 <see cref="Response"/>)를 반환합니다.</returns>
	protected object HandleRequest(byte[] buffer)
	{
		_logger?.MethodEnter(MethodNameHandleRequest);
		try
		{
			return InternalHandleRequest(new Request(buffer));
		}
		finally
		{
			_logger?.MethodLeave(MethodNameHandleRequest);
		}
	}

	/// <summary>
	/// 요청을 내부에서 처리하고 적절한 <see cref="Response"/> 를 반환합니다.
	/// </summary>
	/// <param name="req">처리할 요청</param>
	/// <returns>요청 처리 결과 응답</returns>
	private Response InternalHandleRequest(Request req)
	{
		if (!_devices.TryGetValue(req.Identifier, out var device))
			return new Response(req, ModBusErrorCode.SlaveDeviceFailure);

		if (!IsFunctionEnable((ModBusFunction)req.Function))
			return new Response(req, ModBusErrorCode.IllegalFunction);

		return ((ModBusFunction)req.Function) switch
		{
			ModBusFunction.ReadCoils => InternalReadCoils(device, req),
			ModBusFunction.ReadDiscreteInputs => InternalReadDiscreteInputs(device, req),
			ModBusFunction.ReadHoldingRegisters => InternalReadHoldingRegisters(device, req),
			ModBusFunction.ReadInputRegisters => InternalReadInputRegisters(device, req),
			ModBusFunction.WriteSingleCoil => InternalWriteSingleCoil(device, req),
			ModBusFunction.WriteSingleRegister => InternalWriteSingleRegister(device, req),
			ModBusFunction.WriteMultipleCoils => InternalWriteMultipleCoils(device, req),
			ModBusFunction.WriteMultipleRegisters => InternalWriteMultipleRegister(device, req),
			_ => new Response(req, ModBusErrorCode.IllegalFunction),
		};
	}

	/// <summary>
	/// 코일 상태(읽기)를 처리합니다.
	/// </summary>
	/// <param name="dev">대상 디바이스</param>
	/// <param name="req">요청 정보</param>
	/// <returns>읽기 응답</returns>
	private Response InternalReadCoils(Device dev, Request req)
	{
		_logger?.MethodEnter(MethodNameReadCoils);
		try
		{
			var rsp = new Response(req, req.QuantityForBool);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var addr = req.Address + i;
					if (!dev.GetCoil(addr))
						continue;

					var n = i / 8;
					var d = i % 8;
					var mask = (byte)Math.Pow(2, d);
					rsp.Buffer[9 + n] = (byte)(rsp.Buffer[9 + n] | mask);
				}
			}

			return rsp;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadCoils);
		}
	}

	/// <summary>
	/// 디지털 입력(읽기)를 처리합니다.
	/// </summary>
	/// <param name="dev">대상 디바이스</param>
	/// <param name="req">요청 정보</param>
	/// <returns>읽기 응답</returns>
	private Response InternalReadDiscreteInputs(Device dev, Request req)
	{
		_logger?.MethodEnter(MethodNameReadDiscreteInputs);
		try
		{
			var rsp = new Response(req, req.QuantityForBool);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var addr = req.Address + i;
					if (!dev.GetDiscreteInput(addr))
						continue;

					var n = i / 8;
					var d = i % 8;
					var mask = (byte)Math.Pow(2, d);
					rsp.Buffer[9 + n] = (byte)(rsp.Buffer[9 + n] | mask);
				}
			}

			return rsp;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadDiscreteInputs);
		}
	}

	/// <summary>
	/// 홀딩 레지스터(읽기)를 처리합니다.
	/// </summary>
	/// <param name="dev">대상 디바이스</param>
	/// <param name="req">요청 정보</param>
	/// <returns>읽기 응답</returns>
	private Response InternalReadHoldingRegisters(Device dev, Request req)
	{
		_logger?.MethodEnter(MethodNameReadHoldingRegisters);
		try
		{
			var rsp = new Response(req, req.QuantityForUshort, 125);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var r = dev.GetHoldingRegister(req.Address + i);
					var bs = BitConverter.GetBytes(r);
					rsp.Buffer[9 + (i * 2) + 0] = bs[1];
					rsp.Buffer[9 + (i * 2) + 1] = bs[0];
				}
			}

			return rsp;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadHoldingRegisters);
		}
	}

	/// <summary>
	/// 입력 레지스터(읽기)를 처리합니다.
	/// </summary>
	/// <param name="dev">대상 디바이스</param>
	/// <param name="req">요청 정보</param>
	/// <returns>읽기 응답</returns>
	private Response InternalReadInputRegisters(Device dev, Request req)
	{
		_logger?.MethodEnter(MethodNameReadInputRegisters);
		try
		{
			var rsp = new Response(req, req.QuantityForUshort, 125);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var r = dev.GetInputRegister(req.Address + i);
					var bs = BitConverter.GetBytes(r);
					rsp.Buffer[9 + (i * 2) + 0] = bs[1];
					rsp.Buffer[9 + (i * 2) + 1] = bs[0];
				}
			}

			return rsp;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameReadInputRegisters);
		}
	}

	/// <summary>
	/// 단일 코일 쓰기 요청을 처리합니다.
	/// </summary>
	/// <param name="dev">대상 디바이스</param>
	/// <param name="req">요청 정보</param>
	/// <returns>쓰기 응답</returns>
	private Response InternalWriteSingleCoil(Device dev, Request req)
	{
		_logger?.MethodEnter(MethodNameWriteSingleCoil);
		try
		{
			var value = req.Data[0];

			if (value is > 0 and not 65280)
				return new Response(req, ModBusErrorCode.IllegalDataValue);

			var rsp = new Response(req, 3, 128);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				dev.SetCoil(req.Address, value != 0);

				rsp.AddWriteResponse(value);

				OnCoilsChanged(new ModBusAddressEventArgs(dev.Id, req.Address, 1));
			}

			return rsp;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameWriteSingleCoil);
		}
	}

	/// <summary>
	/// 단일 레지스터 쓰기 요청을 처리합니다.
	/// </summary>
	/// <param name="dev">대상 디바이스</param>
	/// <param name="req">요청 정보</param>
	/// <returns>쓰기 응답</returns>
	private Response InternalWriteSingleRegister(Device dev, Request req)
	{
		_logger?.MethodEnter(MethodNameWriteSingleRegister);
		try
		{
			var rsp = new Response(req, 3, 128);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				dev.SetHoldingRegister(req.Address, (short)req.Data[0]);

				rsp.AddWriteResponse(req.Data[0]);

				OnHoldingRegistersChanged(new ModBusAddressEventArgs(dev.Id, req.Address, 1));
			}

			return rsp;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameWriteSingleRegister);
		}
	}

	/// <summary>
	/// 다수의 코일을 한 번에 쓰는 요청을 처리합니다.
	/// </summary>
	/// <param name="dev">대상 디바이스</param>
	/// <param name="req">요청 정보</param>
	/// <returns>쓰기 응답</returns>
	private Response InternalWriteMultipleCoils(Device dev, Request req)
	{
		_logger?.MethodEnter(MethodNameWriteMultipleCoils);
		try
		{
			var rsp = new Response(req, 3);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var n = 1 << i % 16;
					var b = (req.Data[i / 16] & n) != 0;
					dev.SetCoil(req.Address + i, b);
				}

				rsp.AddWriteResponse(req.Quantity);

				OnCoilsChanged(new ModBusAddressEventArgs(dev.Id, req.Address, req.Quantity));
			}

			return rsp;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameWriteMultipleCoils);
		}
	}

	/// <summary>
	/// 다수의 레지스터를 한 번에 쓰는 요청을 처리합니다.
	/// </summary>
	/// <param name="dev">대상 디바이스</param>
	/// <param name="req">요청 정보</param>
	/// <returns>쓰기 응답</returns>
	private Response InternalWriteMultipleRegister(Device dev, Request req)
	{
		_logger?.MethodEnter(MethodNameWriteMultipleRegister);
		try
		{
			var rsp = new Response(req, 3);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var value = req.Data[i];
					dev.SetHoldingRegister(req.Address + i, (short)value);
				}

				rsp.AddWriteResponse(req.Quantity);

				OnHoldingRegistersChanged(new ModBusAddressEventArgs(dev.Id, req.Address, req.Quantity));
			}

			return rsp;
		}
		finally
		{
			_logger?.MethodLeave(MethodNameWriteMultipleRegister);
		}
	}

	public bool AddDevice(int devId)
	{
		return _devices.TryAdd(devId, new Device(devId));
	}

	/// <summary>
	/// 등록된 디바이스를 제거합니다.
	/// </summary>
	/// <param name="devId">디바이스 식별자</param>
	/// <returns>제거 성공 여부</returns>
	public bool RemoveDevice(int devId)
	{
		return _devices.TryRemove(devId, out _);
	}

	/// <summary>
	/// 디바이스의 코일 값을 설정합니다.
	/// </summary>
	/// <param name="devId">디바이스 식별자</param>
	/// <param name="address">시작 주소</param>
	/// <param name="values">설정할 값들</param>
	public void SetCoils(int devId, int address, params bool[] values)
	{
		if (values.Length == 0)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(values));
		if (address < 0 || address + values.Length > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));

		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));

		device.SetCoils(address, values);
	}

	/// <summary>
	/// 디바이스의 홀딩 레지스터 값을 설정합니다.
	/// </summary>
	/// <param name="devId">디바이스 식별자</param>
	/// <param name="address">시작 주소</param>
	/// <param name="values">설정할 값들</param>
	public void SetHoldingRegisters(int devId, int address, params short[] values)
	{
		if (values.Length == 0)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(values));
		if (address < 0 || address + values.Length > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));

		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));

		device.SetHoldingRegisters(address, values);
	}

	/// <summary>
	/// 지정된 디바이스의 코일 값을 읽어옵니다.
	/// </summary>
	/// <param name="devId">디바이스 식별자</param>
	/// <param name="address">주소</param>
	/// <returns>코일 값</returns>
	public bool GetCoil(int devId, int address)
	{
		if (address is < 0 or > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));
		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));
		return device.GetCoil(address);
	}

	/// <summary>
	/// 지정된 디바이스의 디지털 입력 값을 읽어옵니다.
	/// </summary>
	/// <param name="devId">디바이스 식별자</param>
	/// <param name="address">주소</param>
	/// <returns>입력 값</returns>
	public bool GetDiscreteInput(int devId, int address)
	{
		if (address is < 0 or > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));
		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));
		return device.GetDiscreteInput(address);
	}

	/// <summary>
	/// 지정된 디바이스의 홀딩 레지스터 값을 읽어옵니다.
	/// </summary>
	/// <param name="devId">디바이스 식별자</param>
	/// <param name="address">주소</param>
	/// <returns>레지스터 값</returns>
	public short GetHoldingRegister(int devId, int address)
	{
		if (address is < 0 or > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));
		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));
		return device.GetHoldingRegister(address);
	}

	/// <summary>
	/// 지정된 디바이스의 입력 레지스터 값을 읽어옵니다.
	/// </summary>
	/// <param name="devId">디바이스 식별자</param>
	/// <param name="address">주소</param>
	/// <returns>레지스터 값</returns>
	public short GetInputRegister(int devId, int address)
	{
		if (address is < 0 or > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));
		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));
		return device.GetInputRegister(address);
	}

	// Logger method name constants
	private const string MethodNameHandleRequestBuffer = "ModBusServer.HandleRequestBuffer";
	private const string MethodNameHandleRequest = "ModBusServer.HandleRequest";
	private const string MethodNameReadCoils = "ModBusServer.ReadCoils";
	private const string MethodNameReadDiscreteInputs = "ModBusServer.ReadDiscreteInputs";
	private const string MethodNameReadHoldingRegisters = "ModBusServer.ReadHoldingRegisters";
	private const string MethodNameReadInputRegisters = "ModBusServer.ReadInputRegisters";
	private const string MethodNameWriteSingleCoil = "ModBusServer.WriteSingleCoil";
	private const string MethodNameWriteSingleRegister = "ModBusServer.WriteSingleRegister";
	private const string MethodNameWriteMultipleCoils = "ModBusServer.WriteMultipleCoils";
	private const string MethodNameWriteMultipleRegister = "ModBusServer.WriteMultipleRegister";
}
