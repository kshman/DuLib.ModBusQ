using Du.ModBusQ.Suppliment;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 서버 기본
/// </summary>
public abstract class ModBusServer : IModBusServer
{
	/// <summary></summary>
	protected readonly ILogger? _lg;

	private readonly Dictionary<int, bool> _coils = new();
	private readonly Dictionary<int, bool> _discrete_inputs = new();
	private readonly Dictionary<int, short> _hold_registers = new();
	private readonly Dictionary<int, short> _input_registers = new();
	private int _func_enable = int.MaxValue;

	/// <inheritdoc/>
	public abstract ModBusConnection ConnectionType { get; }
	/// <inheritdoc/>
	public abstract bool IsRunning { get; protected set; }

	/// <inheritdoc/>
	public abstract DateTime StartTime { get; protected set; }

	/// <inheritdoc/>
	public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
	/// <inheritdoc/>
	public TimeSpan ReceiveTimeout { get; set; } = Timeout.InfiniteTimeSpan;

	/// <summary></summary>
	public EventHandler<ModBusAddressCountEventArgs>? CoilsChanged;
	/// <summary></summary>
	public EventHandler<ModBusAddressCountEventArgs>? HoldingRegistersChanged;
	/// <summary></summary>
	public EventHandler<ModBusClientEventArgs>? ClientConnected;
	/// <summary></summary>
	public EventHandler<ModBusClientEventArgs>? ClientDisconnected;

	/// <summary></summary>
	protected bool CanInvokeCoilsChanged => CoilsChanged != null;
	/// <summary></summary>
	protected bool CanInvokeHoldingRegistersChanged => HoldingRegistersChanged != null;
	/// <summary></summary>
	protected bool CanInvokeClientConnected => ClientConnected != null;
	/// <summary></summary>
	protected bool CanInvokeClientDisconnected => ClientDisconnected != null;

	/// <summary></summary>
	protected void OnCoilsChanged(ModBusAddressCountEventArgs e) => CoilsChanged?.Invoke(this, e);
	/// <summary></summary>
	protected void OnHoldingRegistersChanged(ModBusAddressCountEventArgs e) => HoldingRegistersChanged?.Invoke(this, e);
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
	/// 요청을 핸들링 한다
	/// </summary>
	/// <param name="Buffer"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	protected Response HandleRequest(byte[] Buffer)
	{
		var req = new Request(Buffer);

		if (!IsFunctionEnable((ModBusFunction)req.Function))
			return new Response(req, ModBusErrorCode.IllegalFunction);

		return ((ModBusFunction)req.Function) switch
		{
			ModBusFunction.ReadCoils => InternalReadCoils(req),
			ModBusFunction.ReadDiscreteInputs => InternalReadDiscreteInputs(req),
			ModBusFunction.ReadHoldingRegisters => InternalReadHoldingRegisters(req),
			ModBusFunction.ReadInputRegisters => InternalReadInputRegisters(req),
			ModBusFunction.WriteSingleCoil => InternalWriteSingleCoil(req),
			ModBusFunction.WriteSingleRegister => InternalWriteSingleRegister(req),
			ModBusFunction.WriteMultipleCoils => InternalWriteMultipleCoils(req),
			ModBusFunction.WriteMultipleRegisters => InternalWriteMultipleRegister(req),
			_ => new Response(req, ModBusErrorCode.IllegalFunction),
		};
	}

	//
	private Response InternalReadCoils(Request req)
	{
		var rsp = new Response(req, req.QuantityForBool);

		if (rsp.Error == ModBusErrorCode.NoError)
		{
			for (var i = 0; i < rsp.Count; i++)
			{
				byte mask = 0;

				for (var p = 0; p < 8; p++)
				{
					var seq = i * 8 + p;
					if (_coils.TryGetValue(req.Address + seq, out var b) && b)
						mask = (byte)(mask | 1 << p);
					if (seq + 1 >= rsp.Length)
						break;
				}

				rsp.Buffer[9 + i] = mask;
			}
		}

		return rsp;
	}

	//
	private Response InternalReadDiscreteInputs(Request req)
	{
		var rsp = new Response(req, req.QuantityForBool);

		if (rsp.Error == ModBusErrorCode.NoError)
		{
			for (var i = 0; i < rsp.Count; i++)
			{
				byte mask = 0;

				for (var p = 0; p < 8; p++)
				{
					var seq = i * 8 + p;
					if (_discrete_inputs.TryGetValue(req.Address + seq, out var b) && b)
						mask = (byte)(mask | 1 << p);
					if (seq + 1 >= rsp.Length)
						break;
				}

				rsp.Buffer[9 + i] = mask;
			}
		}

		return rsp;
	}

	//
	private Response InternalReadHoldingRegisters(Request req)
	{
		var rsp = new Response(req, req.QuantityForUshort, 125);

		if (rsp.Error == ModBusErrorCode.NoError)
		{
			for (var i = 0; i < req.Quantity; i++)
			{
				if (!_hold_registers.TryGetValue(req.Address + i, out var r))
					r = 0;
				var bs = BitConverter.GetBytes(r);
				rsp.Buffer[9 + i * 2 + 0] = bs[1];
				rsp.Buffer[9 + i * 2 + 1] = bs[0];
			}
		}

		return rsp;
	}

	//
	private Response InternalReadInputRegisters(Request req)
	{
		var rsp = new Response(req, req.QuantityForUshort, 125);

		if (rsp.Error == ModBusErrorCode.NoError)
		{
			for (var i = 0; i < req.Quantity; i++)
			{
				if (!_input_registers.TryGetValue(req.Address + i, out var r))
					r = 0;
				var bs = BitConverter.GetBytes(r);
				rsp.Buffer[9 + i * 2 + 0] = bs[1];
				rsp.Buffer[9 + i * 2 + 1] = bs[0];
			}
		}

		return rsp;
	}

	//
	private Response InternalWriteSingleCoil(Request req)
	{
		var value = req.Data[0];

		if (value > 0 && value != 65280)
			return new Response(req, ModBusErrorCode.IllegalDataValue);

		var rsp = new Response(req, 3, 128);

		if (rsp.Error == ModBusErrorCode.NoError)
		{
			_coils[req.Address] = value == 65280;

			rsp.AddWriteResponse(value);

			CoilsChanged?.Invoke(this, new ModBusAddressCountEventArgs(req.Address, 1));
		}

		return rsp;
	}

	//
	private Response InternalWriteSingleRegister(Request req)
	{
		var rsp = new Response(req, 3, 128);

		if (rsp.Error == ModBusErrorCode.NoError)
		{
			_hold_registers[req.Address] = (short)req.Data[0];

			rsp.AddWriteResponse(req.Data[0]);

			HoldingRegistersChanged?.Invoke(this, new ModBusAddressCountEventArgs(req.Address, 1));
		}

		return rsp;
	}

	//
	private Response InternalWriteMultipleCoils(Request req)
	{
		var rsp = new Response(req, 3, 2000);

		if (rsp.Error == ModBusErrorCode.NoError)
		{
			for (var i=0; i<req.Quantity; i++)
			{
				var n = 1 << i % 16;
				var b = (req.Data[i / 16] & n) != 0;
				_coils[req.Address + i] = b;
			}

			rsp.AddWriteResponse(req.Quantity);

			CoilsChanged?.Invoke(this, new ModBusAddressCountEventArgs(req.Address, req.Quantity));
		}

		return rsp;
	}

	//
	private Response InternalWriteMultipleRegister(Request req)
	{
		var rsp = new Response(req, 3, 2000);

		if (rsp.Error == ModBusErrorCode.NoError)
		{
			for (var i = 0; i < req.Quantity; i++)
			{
				var value = req.Data[i];
				_hold_registers[req.Address + i] = (short)value;
			}

			rsp.AddWriteResponse(req.Quantity);

			HoldingRegistersChanged?.Invoke(this, new ModBusAddressCountEventArgs(req.Address, req.Quantity));
		}

		return rsp;
	}
}
