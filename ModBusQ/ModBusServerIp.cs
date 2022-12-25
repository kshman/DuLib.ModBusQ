using System.Net;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스  TCP/IP 서버 기반 클래스
/// </summary>
public abstract class ModBusServerIp : IModBusServer
{
	/// <summary></summary>
	protected readonly Dictionary<int, bool> _coils = new();
	/// <summary></summary>
	protected readonly Dictionary<int, bool> _discrete_inputs = new();
	/// <summary></summary>
	protected readonly Dictionary<int, short> _hold_registers = new();
	/// <summary></summary>
	protected readonly Dictionary<int, short> _input_registers = new();

	/// <summary>리슨 주소</summary>
	public IPAddress Address { get; set; }
	/// <summary>리슨 포트</summary>
	public int Port { get; set; }

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
	public EventHandler<ModBusCountEventArgs>? CoilsChanged;
	/// <summary></summary>
	public EventHandler<ModBusCountEventArgs>? HoldingRegistersChanged;
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
	protected void OnCoilsChanged(ModBusCountEventArgs e) => CoilsChanged?.Invoke(this, e);
	/// <summary></summary>
	protected void OnHoldingRegistersChanged(ModBusCountEventArgs e) => HoldingRegistersChanged?.Invoke(this, e);
	/// <summary></summary>
	protected void OnClientConnected(ModBusClientEventArgs e) => ClientConnected?.Invoke(this, e);
	/// <summary></summary>
	protected void OnClientDisconnected(ModBusClientEventArgs e) => ClientDisconnected?.Invoke(this, e);

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	protected ModBusServerIp(int port = 502)
	{
		Address = IPAddress.Any;
		Port = port;
	}

	/// <summary>
	/// 디스트럭터
	/// </summary>
	~ModBusServerIp()
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
}
