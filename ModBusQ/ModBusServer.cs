using System.Collections.Concurrent;
using Du.ModBusQ.Suppliment;
using Du.Properties;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 서버 기본
/// </summary>
public abstract class ModBusServer : IModBusServer
{
	/// <summary></summary>
	protected readonly ILogger? _lg;

	private readonly ConcurrentDictionary<int, Device> _devices = new();
	private int _func_enable = int.MaxValue;

	/// <inheritdoc/>
	public abstract ModBusConnection ConnectionType { get; }

	/// <inheritdoc/>
	public virtual bool IsRunning { get; protected set; }

	/// <inheritdoc/>
	public DateTime StartTime { get; protected set; }

	/// <inheritdoc/>
	public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

	/// <inheritdoc/>
	public TimeSpan ReceiveTimeout { get; set; } = Timeout.InfiniteTimeSpan;

	/// <summary></summary>
	public event EventHandler<ModBusAddressEventArgs>? CoilsChanged;

	/// <summary></summary>
	public event EventHandler<ModBusAddressEventArgs>? HoldingRegistersChanged;

	/// <summary></summary>
	public event EventHandler<ModBusClientEventArgs>? ClientConnected;

	/// <summary></summary>
	public event EventHandler<ModBusClientEventArgs>? ClientDisconnected;

	/// <summary></summary>
	protected void OnCoilsChanged(ModBusAddressEventArgs e) => CoilsChanged?.Invoke(this, e);

	/// <summary></summary>
	protected void OnHoldingRegistersChanged(ModBusAddressEventArgs e) => HoldingRegistersChanged?.Invoke(this, e);

	/// <summary></summary>
	protected void OnClientConnected(ModBusClientEventArgs e) => ClientConnected?.Invoke(this, e);

	/// <summary></summary>
	protected void OnClientDisconnected(ModBusClientEventArgs e) => ClientDisconnected?.Invoke(this, e);

	/// <summary>
	/// 새 인스턴스를 만들지는 못해요!
	/// </summary>
	/// <param name="logger"></param>
	protected ModBusServer(ILogger? logger)
	{
		_lg = logger;

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
	/// Dispose 패턴의 구현
	/// </summary>
	/// <param name="disposing"></param>
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
	/// 기능을 사용하는지 켬끔
	/// </summary>
	/// <param name="function"></param>
	/// <param name="value"></param>
	public void SetFunctionEnable(ModBusFunction function, bool value)
	{
		if ((int)function > 30)
			return;

		if (value)
			_func_enable |= 1 << (int)function;
		else
			_func_enable &= ~1 << (int)function;
	}

	/// <summary>
	/// 기능 사용하는지 확인
	/// </summary>
	/// <param name="function"></param>
	/// <returns></returns>
	public bool IsFunctionEnable(ModBusFunction function)
	{
		return (_func_enable & (1 << (int)function)) != 0;
	}

	/// <summary>
	/// 요청을 처리하고 버퍼를 받는다
	/// </summary>
	/// <param name="buffer"></param>
	/// <returns></returns>
	protected byte[] HandleRequestBuffer(byte[] buffer)
	{
		_lg?.MethodEnter("ModBusServer.HandleRequestBuffer");
		try
		{
			var rsp = InternalHandleRequest(new Request(buffer));
			return rsp.Buffer;
		}
		finally
		{
			_lg?.MethodLeave("ModBusServer.HandleRequestBuffer");
		}
	}

	/// <summary>
	/// 요청을 처리하고 요청처리 오브젝트를 받는다
	/// </summary>
	/// <param name="buffer"></param>
	/// <returns></returns>
	protected object HandleRequest(byte[] buffer)
	{
		_lg?.MethodEnter("ModBusServer.HandleRequest");
		try
		{
			return InternalHandleRequest(new Request(buffer));
		}
		finally
		{
			_lg?.MethodLeave("ModBusServer.HandleRequest");
		}
	}

	//
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

	//
	private Response InternalReadCoils(Device dev, Request req)
	{
		_lg?.MethodEnter("ModBusServer.ReadCoils");
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
			_lg?.MethodLeave("ModBusServer.ReadCoils");
		}
	}

	//
	private Response InternalReadDiscreteInputs(Device dev, Request req)
	{
		_lg?.MethodEnter("ModBusServer.ReadDiscreteInputs");
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
			_lg?.MethodLeave("ModBusServer.ReadDiscreteInputs");
		}
	}

	//
	private Response InternalReadHoldingRegisters(Device dev, Request req)
	{
		_lg?.MethodEnter("ModBusServer.ReadHoldingRegisters");
		try
		{
			var rsp = new Response(req, req.QuantityForUshort, 125);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var r = dev.GetHoldingRegister(req.Address + i);
					var bs = BitConverter.GetBytes(r);
					rsp.Buffer[9 + i * 2 + 0] = bs[1];
					rsp.Buffer[9 + i * 2 + 1] = bs[0];
				}
			}

			return rsp;
		}
		finally
		{
			_lg?.MethodLeave("ModBusServer.ReadHoldingRegisters");
		}
	}

	//
	private Response InternalReadInputRegisters(Device dev, Request req)
	{
		_lg?.MethodEnter("ModBusServer.ReadInputRegisters");
		try
		{
			var rsp = new Response(req, req.QuantityForUshort, 125);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var r = dev.GetInputRegister(req.Address + i);
					var bs = BitConverter.GetBytes(r);
					rsp.Buffer[9 + i * 2 + 0] = bs[1];
					rsp.Buffer[9 + i * 2 + 1] = bs[0];
				}
			}

			return rsp;
		}
		finally
		{
			_lg?.MethodLeave("ModBusServer.ReadInputRegisters");
		}
	}

	//
	private Response InternalWriteSingleCoil(Device dev, Request req)
	{
		_lg?.MethodEnter("ModBusServer.WriteSingleCoil");
		try
		{
			var value = req.Data[0];

			if (value > 0 && value != 65280)
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
			_lg?.MethodLeave("ModBusServer.WriteSingleCoil");
		}
	}

	//
	private Response InternalWriteSingleRegister(Device dev, Request req)
	{
		_lg?.MethodEnter("ModBusServer.WriteSingleRegister");
		try
		{
			var rsp = new Response(req, 3, 128);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				dev.SetHoldingRegister(req.Address, req.Data[0]);

				rsp.AddWriteResponse(req.Data[0]);

				OnHoldingRegistersChanged(new ModBusAddressEventArgs(dev.Id, req.Address, 1));
			}

			return rsp;
		}
		finally
		{
			_lg?.MethodLeave("ModBusServer.WriteSingleRegister");
		}
	}

	//
	private Response InternalWriteMultipleCoils(Device dev, Request req)
	{
		_lg?.MethodEnter("ModBusServer.WriteMultipleCoils");
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
			_lg?.MethodLeave("ModBusServer.WriteMultipleCoils");
		}
	}

	//
	private Response InternalWriteMultipleRegister(Device dev, Request req)
	{
		_lg?.MethodEnter("ModBusServer.WriteMultipleRegister");
		try
		{
			var rsp = new Response(req, 3);

			if (rsp.Error == ModBusErrorCode.NoError)
			{
				for (var i = 0; i < req.Quantity; i++)
				{
					var value = req.Data[i];
					dev.SetHoldingRegister(req.Address + i, value);
				}

				rsp.AddWriteResponse(req.Quantity);

				OnHoldingRegistersChanged(new ModBusAddressEventArgs(dev.Id, req.Address, req.Quantity));
			}

			return rsp;
		}
		finally
		{
			_lg?.MethodLeave("ModBusServer.WriteMultipleRegister");
		}
	}

	public bool AddDevice(int devId)
	{
		return _devices.TryAdd(devId, new Device(devId));
	}

	public bool RemoveDevice(int devId)
	{
		return _devices.TryRemove(devId, out _);
	}

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

	public void SetHoldingRegisters(int devId, int address, params int[] values)
	{
		if (values.Length == 0)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(values));
		if (address < 0 || address + values.Length > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));

		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));

		device.SetHoldingRegisters(address, values);
	}

	public bool GetCoil(int devId, int address)
	{
		if (address<0 || address>65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));
		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));
		return device.GetCoil(address);
	}

	public bool GetDiscreteInput(int devId, int address)
	{
		if (address < 0 || address > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));
		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));
		return device.GetDiscreteInput(address);
	}

	public int GetHoldingRegister(int devId, int address)
	{
		if (address < 0 || address > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));
		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));
		return device.GetHoldingRegister(address);
	}

	public int GetInputRegister(int devId, int address)
	{
		if (address < 0 || address > 65535)
			throw new ArgumentException(Resources.ExceptionArgument, nameof(address));
		if (!_devices.TryGetValue(devId, out var device))
			throw new ArgumentException(Resources.ExceptionArgument, nameof(devId));
		return device.GetInputRegister(address);
	}
}
