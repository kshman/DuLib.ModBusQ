namespace Du.ModBusQ;

/// <summary>
/// 모드버스 서버 인터페이스
/// </summary>
public interface IModBusServer : IDisposable
{
	/// <summary>커넥션 종류</summary>
	/// <see cref="ModBusConnection"/>
	ModBusConnection ConnectionType { get; }
	/// <summary>실행중인가</summary>
	bool IsRunning { get; }
	/// <summary>시작 시간</summary>
	DateTime StartTime { get; }
	/// <summary>연결 시간 제한</summary>
	TimeSpan ConnectionTimeout { get; set; }
	/// <summary>읽기 시간 제한</summary>
	TimeSpan ReceiveTimeout { get; set; }

	void Start();
	void Stop();

	void SetFunctionEnable(ModBusFunction function, bool value);
	bool IsFunctionEnable(ModBusFunction function);
}
