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

	bool AddDevice(int devId);
	bool RemoveDevice(int devId);

	void SetCoils(int devId, int address, params bool[] values);
	void SetHoldingRegisters(int devId, int address, params int[] values);

	bool GetCoil(int devId, int address);
	bool GetDiscreteInput(int devId, int address);
	int GetHoldingRegister(int devId, int address);
	int GetInputRegister(int devId, int address);
}
