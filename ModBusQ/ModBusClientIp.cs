using System.Net;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 TCP/IP 클라이언트 베이스
/// </summary>
public abstract class ModBusClientIp : IModBusClient
{
	/// <summary>대상 서버의 주소</summary>
	public IPAddress Address { get; set; }
	/// <summary>대상 서버의 연결 포트/// </summary>
	public int Port { get; set; }

	/// <inheritdoc/>
	public abstract ModBusConnection ConnectionType { get; }
	/// <inheritdoc/>
	public abstract bool IsConnected { get; protected set; }

	/// <inheritdoc/>
	public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(3);
	/// <inheritdoc/>
	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(5);

	public ModBusTraceFlags TraceFlags { get; set; } = ModBusTraceFlags.None;

	/// <summary>데이터를 받았을 때 처리 핸들러</summary>
	public event EventHandler<ModBusBufferedEventArgs>? AfterRead;
	/// <summary>데이터를 보냈을 때 처리 핸들러</summary>
	public event EventHandler<ModBusBufferedEventArgs>? AfterWrite;
	/// <summary>커넥션이 변경됐을 때 처리 핸들러</summary>
	public event EventHandler<ModBusStateChangedEventArgs>? ConnectionChanged;

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="addr"></param>
	/// <param name="port"></param>
	protected ModBusClientIp(IPAddress addr, int port)
	{
		Address = addr;
		Port = port;
	}

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	/// <param name="address"></param>
	/// <param name="port"></param>
	protected ModBusClientIp(string address, int port)
		: this(IPAddress.Parse(address), port) { }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	protected ModBusClientIp()
	{
		Address = IPAddress.Loopback;
		Port = 502;
	}

	/// <summary>디스트럭터</summary>
	~ModBusClientIp()
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

	/// <inheritdoc/>
	public abstract void Open();
	/// <inheritdoc/>
	public abstract void Close();

	/// <inheritdoc/>
	public abstract bool[] ReadCoils(int devId, int startAddress, int readCount);
	/// <inheritdoc/>
	public abstract bool[] ReadDiscreteInputs(int devId, int startAddress, int readCount);
	/// <inheritdoc/>
	public abstract int[] ReadHoldingRegisters(int devId, int startAddress, int readCount);
	/// <inheritdoc/>
	public abstract int[] ReadInputRegisters(int devId, int startAddress, int readCount);

	/// <inheritdoc/>
	public abstract void WriteSingleCoil(int devId, int startAddress, bool value);
	/// <inheritdoc/>
	public abstract void WriteSingleRegister(int devId, int startAddress, int value);
	/// <inheritdoc/>
	public abstract void WriteMultipleCoils(int devId, int startAddress, bool[] values);
	/// <inheritdoc/>
	public abstract void WriteMultipleRegisters(int devId, int startAddress, int[] values);

	/// <summary>
	/// 읽고난 뒤 대리자 호출
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnAfterRead(ModBusBufferedEventArgs e)
	{
		AfterRead?.Invoke(this, e);
	}

	/// <summary>
	/// 쓰고난 뒤 대리자 호출
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnAfterWrite(ModBusBufferedEventArgs e)
	{
		AfterWrite?.Invoke(this, e);
	}

	/// <summary>
	/// 접속 상태가 바뀌면 호출
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnConnectionChanged(ModBusStateChangedEventArgs e)
	{
		ConnectionChanged?.Invoke(this, e);
	}

	/// <summary>인보크 테스트</summary>
	protected bool CanInvokeAfterRead => AfterRead != null;
	/// <summary>인보크 테스트</summary>
	protected bool CanInvokeAfterWrite=> AfterWrite != null;
	/// <summary>인보크 테스트</summary>
	protected bool CanInvokeConnectionChanged => ConnectionChanged != null;
}
