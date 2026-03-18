namespace Du.ModBusQ;

/// <summary>
/// 모드버스 서버 인터페이스
/// </summary>
public interface IModBusServer : IDisposable
{
	/// <summary>커넥션 종류</summary>
	/// <see cref="ModBusConnection"/>
	ModBusConnection ConnectionType { get; }
	/// <summary>서버가 현재 실행 중인지 여부를 나타냅니다.</summary>
	bool IsRunning { get; }
	/// <summary>서버가 시작된 시각을 나타냅니다.</summary>
	DateTime StartTime { get; }
	/// <summary>클라이언트 연결 타임아웃(초)입니다. 연결 수립에 사용할 최대 대기 시간입니다.</summary>
	TimeSpan ConnectionTimeout { get; set; }
	/// <summary>네트워크 스트림 읽기 작업에 대한 타임아웃입니다.</summary>
	TimeSpan ReceiveTimeout { get; set; }

	/// <summary>추가된 추적 플래그</summary>
	ModBusTraceMasks TraceMask { get; set; }

	/// <summary>
	/// 서버를 시작합니다. 이미 실행 중이면 아무 작업도 하지 않습니다.
	/// </summary>
	void Start();
	/// <summary>
	/// 서버를 중지합니다. 실행 중이 아닐 경우 아무 작업도 하지 않습니다.
	/// </summary>
	void Stop();

	/// <summary>
	/// 지정한 기능 코드를 사용 가능/사용 불가로 설정합니다.
	/// </summary>
	void SetFunctionEnable(ModBusFunction function, bool value);
	/// <summary>
	/// 지정한 기능 코드가 사용 가능한지 여부를 반환합니다.
	/// </summary>
	bool IsFunctionEnable(ModBusFunction function);

	/// <summary>
	/// 디바이스(슬레이브)를 추가합니다. 성공하면 true를 반환합니다.
	/// </summary>
	/// <param name="devId">추가할 디바이스의 식별자입니다.</param>
	bool AddDevice(int devId);
	/// <summary>
	/// 지정한 식별자의 디바이스를 제거합니다. 성공하면 true를 반환합니다.
	/// </summary>
	/// <param name="devId">제거할 디바이스의 식별자입니다.</param>
	bool RemoveDevice(int devId);

	/// <summary>
	/// 지정한 디바이스의 연속된 코일(디지털 출력) 값을 설정합니다.
	/// </summary>
	/// <param name="devId">대상 디바이스 식별자입니다.</param>
	/// <param name="address">시작 주소입니다.</param>
	/// <param name="values">설정할 값 목록입니다.</param>
	void SetCoils(int devId, int address, params bool[] values);
	/// <summary>
	/// 지정한 디바이스의 연속된 홀딩 레지스터(아날로그 출력) 값을 설정합니다.
	/// </summary>
	/// <param name="devId">대상 디바이스 식별자입니다.</param>
	/// <param name="address">시작 주소입니다.</param>
	/// <param name="values">설정할 16비트 정수 값 목록입니다.</param>
	void SetHoldingRegisters(int devId, int address, params short[] values);

	/// <summary>지정한 디바이스의 코일 값을 읽어 반환합니다.</summary>
	bool GetCoil(int devId, int address);
	/// <summary>지정한 디바이스의 디지털 입력 값을 읽어 반환합니다.</summary>
	bool GetDiscreteInput(int devId, int address);
	/// <summary>지정한 디바이스의 홀딩 레지스터 값을 읽어 반환합니다.</summary>
	short GetHoldingRegister(int devId, int address);
	/// <summary>지정한 디바이스의 입력 레지스터 값을 읽어 반환합니다.</summary>
	short GetInputRegister(int devId, int address);
}
