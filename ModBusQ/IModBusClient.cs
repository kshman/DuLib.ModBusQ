namespace Du.ModBusQ;

/// <summary>
/// 모드버스 클라이언트 인터페이스
/// </summary>
public interface IModBusClient : IDisposable
{
	/// <summary>커넥션 종류</summary>
	/// <see cref="ModBusConnection"/>
	ModBusConnection ConnectionType { get; }

	/// <summary>연결 중이면 참이예요</summary>
	bool IsConnected { get; }
	/// <summary>커넥션을 시도할 때 시간 제한</summary>
	TimeSpan ConnectionTimeout { get; set; }
	/// <summary>받기를 시도할 때 시간 제한</summary>
	TimeSpan ReceiveTimeout { get; set; }

	/// <summary>추가된 추적 플래그</summary>
	ModBusTraceFlags TraceFlags { get; set; }

	/// <summary>커넥션을 열어요</summary>
	void Open();
	/// <summary>커넥션을 닫아요</summary>
	void Close();

	/// <summary>
	/// 코일을 읽어요
	/// </summary>
	/// <param name="devId">읽어올 장치 아이디</param>
	/// <param name="startAddress">읽기 시작할 주소</param>
	/// <param name="readCount">읽을 주소의 개수</param>
	/// <returns></returns>
	public bool[] ReadCoils(int devId, int startAddress, int readCount);
	/// <summary>
	/// 디스크릿 입력을 읽어요
	/// </summary>
	/// <param name="devId">읽어올 장치 아이디</param>
	/// <param name="startAddress">읽기 시작할 주소</param>
	/// <param name="readCount">읽을 주소의 개수</param>
	/// <returns></returns>
	public bool[] ReadDiscreteInputs(int devId, int startAddress, int readCount);
	/// <summary>
	/// 홀딩 레지스터를 읽어요
	/// </summary>
	/// <param name="devId">읽어올 장치 아이디</param>
	/// <param name="startAddress">읽기 시작할 주소</param>
	/// <param name="readCount">읽을 주소의 개수</param>
	/// <returns></returns>
	public int[] ReadHoldingRegisters(int devId, int startAddress, int readCount);
	/// <summary>
	/// 인풋 레지스터를 읽어요
	/// </summary>
	/// <param name="devId">읽어올 장치 아이디</param>
	/// <param name="startAddress">읽기 시작할 주소</param>
	/// <param name="readCount">읽을 주소의 개수</param>
	/// <returns></returns>
	public int[] ReadInputRegisters(int devId, int startAddress, int readCount);

	/// <summary>
	/// 코일 한개를 써요
	/// </summary>
	/// <param name="devId">쓸 장치 아이디</param>
	/// <param name="startAddress">쓸 위치의 주소</param>
	/// <param name="value">쓸 값</param>
	public void WriteSingleCoil(int devId, int startAddress, bool value);
	/// <summary>
	/// 레지스터 한개를 써요
	/// </summary>
	/// <param name="devId">쓸 장치 아이디</param>
	/// <param name="startAddress">쓸 위치의 주소</param>
	/// <param name="value">쓸 값</param>
	public void WriteSingleRegister(int devId, int startAddress, int value);
	/// <summary>
	/// 연속된 코일 여러개를 써요
	/// </summary>
	/// <param name="devId">쓸 장치 아이디</param>
	/// <param name="startAddress">쓰기 시작할 위치의 주소</param>
	/// <param name="values">쓸 값</param>
	public void WriteMultipleCoils(int devId, int startAddress, bool[] values);
	/// <summary>
	/// 연속된 레지스터 여러개를 써요
	/// </summary>
	/// <param name="devId">쓸 장치 아이디</param>
	/// <param name="startAddress">쓰기 시작할 위치의 주소</param>
	/// <param name="values">쓸 값</param>
	public void WriteMultipleRegisters(int devId, int startAddress, int[] values);
}
