using System.Net;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 TCP/IP 클라이언트 베이스
/// </summary>
public abstract class ModBusClientIp : IModBusClient
{
	/// <summary>대상 서버의 주소</summary>
	public IPAddress Address { get; set; }
	/// <summary>대상 서버의 연결 포트</summary>
	public int Port { get; set; }

	/// <inheritdoc/>
	public abstract ModBusConnection ConnectionType { get; }
	/// <inheritdoc/>
	public abstract bool IsConnected { get; protected set; }

	/// <inheritdoc/>
	public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(3);
	/// <inheritdoc/>
	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(5);

	public ModBusTraceMasks TraceMask { get; set; } = ModBusTraceMasks.None;

	/// <summary>데이터를 받았을 때 처리 핸들러</summary>
	public event EventHandler<ModBusBufferedEventArgs>? AfterRead;
	/// <summary>데이터를 보냈을 때 처리 핸들러</summary>
	public event EventHandler<ModBusBufferedEventArgs>? AfterWrite;
	/// <summary>커넥션이 변경됐을 때 처리 핸들러</summary>
	public event EventHandler<ModBusStateChangedEventArgs>? ConnectionChanged;

	/// <summary>
	/// IP 기반 클라이언트의 기본 생성자입니다. 대상 주소와 포트를 지정합니다.
	/// </summary>
	/// <param name="addr">대상 서버의 IP 주소입니다.</param>
	/// <param name="port">대상 서버의 포트 번호입니다.</param>
	protected ModBusClientIp(IPAddress addr, int port)
	{
		Address = addr;
		Port = port;
	}

	/// <summary>
	/// 문자열 주소를 사용하여 클라이언트를 초기화합니다. 내부적으로 <see cref="IPAddress.Parse(string)"/>를 사용합니다.
	/// </summary>
	/// <param name="address">연결할 대상의 IP 주소 문자열입니다. 예: "192.168.0.10"</param>
	/// <param name="port">연결할 대상의 포트 번호입니다.</param>
	protected ModBusClientIp(string address, int port)
		: this(IPAddress.Parse(address), port) { }

	/// <summary>
	/// 기본 생성자: 루프백 주소와 기본 포트(502)로 초기화합니다.
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
	public abstract short[] ReadHoldingRegisters(int devId, int startAddress, int readCount);
	/// <inheritdoc/>
	public abstract short[] ReadInputRegisters(int devId, int startAddress, int readCount);
	/// <inheritdoc/>
	public abstract byte[] ReadRawHoldingRegisters(int devId, int startAddress, int readCount);
	/// <inheritdoc/>
	public abstract byte[] ReadRawInputRegisters(int devId, int startAddress, int readCount);

	/// <inheritdoc/>
	public abstract void WriteSingleCoil(int devId, int startAddress, bool value);
	/// <inheritdoc/>
	public abstract void WriteSingleRegister(int devId, int startAddress, short value);
	/// <inheritdoc/>
	public abstract void WriteMultipleCoils(int devId, int startAddress, bool[] values);
	/// <inheritdoc/>
	public abstract void WriteMultipleRegisters(int devId, int startAddress, short[] values);

	/// <summary>
	/// 데이터를 수신한 후 호출되는 내부 헬퍼입니다.
	/// </summary>
	/// <param name="e">수신된 데이터 정보를 포함하는 이벤트 인수입니다.</param>
	protected virtual void OnAfterRead(ModBusBufferedEventArgs e)
	{
		AfterRead?.Invoke(this, e);
	}

	/// <summary>
	/// 데이터를 송신한 후 호출되는 내부 헬퍼입니다.
	/// </summary>
	/// <param name="e">송신한 데이터 정보를 포함하는 이벤트 인수입니다.</param>
	protected virtual void OnAfterWrite(ModBusBufferedEventArgs e)
	{
		AfterWrite?.Invoke(this, e);
	}

	/// <summary>
	/// 연결 상태가 변경되었을 때 호출되는 내부 헬퍼입니다.
	/// </summary>
	/// <param name="e">연결 상태 변경 정보를 포함하는 이벤트 인수입니다.</param>
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
