namespace Du.ModBusQ;

/// <summary>
/// 모드버스 연결 형식
/// </summary>
public enum ModBusConnection
{
	/// <summary>시리얼</summary>
	Rtu,
	/// <summary>TCP</summary>
	Tcp,
	/// <summary>UDP</summary>
	Udp,
}

/// <summary>
/// 모드버스 오브젝트 타입
/// </summary>
public enum ModBusObjectType
{
	/// <summary>코일</summary>
	Coil,
	/// <summary>디스크릿 인풋</summary>
	DiscreteInput,
	/// <summary>홀드 레지스터</summary>
	HoldingRegister,
	/// <summary>인풋 레지스터</summary>
	InputRegister,
}

/// <summary>
/// 모드버스 기능 코드
/// </summary>
public enum ModBusFunction
{
	/// <summary>코일 읽기</summary>
	ReadCoils = 1,
	/// <summary>디스크릿 읽기</summary>
	ReadDiscreteInputs = 2,
	/// <summary>홀드 레지스터 읽기</summary>
	ReadHoldingRegisters = 3,
	/// <summary>인풋 레지스터 읽기</summary>
	ReadInputRegisters = 4,
	/// <summary>코일 한개 쓰기</summary>
	WriteSingleCoil = 5,
	/// <summary>레지스터 한개 쓰기</summary>
	WriteSingleRegister = 6,
	/// <summary>여러 코일 쓰기</summary>
	WriteMultipleCoils = 15,
	/// <summary>여러 레지스터 쓰기</summary>
	WriteMultipleRegisters = 16,
	/// <summary>EncapsulatedInterface</summary>
	EncapsulatedInterface = 43,
}

/// <summary>
/// 모드버스 MEI 타입
/// </summary>
public enum ModBusMei : byte
{
	/// <summary>CanOpenGeneralReference</summary>
	CanOpenGeneralReference = 13,
	/// <summary>ReadDeviceInformation</summary>
	ReadDeviceInformation = 14,
}

/// <summary>
/// 모드버스 디바이스 ID 카테고리
/// </summary>
public enum ModBusDevIdCategory : byte
{
	/// <summary>기본</summary>
	Basic = 1,
	/// <summary>일반</summary>
	Regular = 2,
	/// <summary>확장</summary>
	Extended = 3,
	/// <summary>개별 항목</summary>
	Individual = 4,
}

/// <summary>
/// 모드버스 디바이스 ID 오브젝트
/// </summary>
public enum ModBusDevIdObject : byte
{
	/// <summary>벤더 이름</summary>
	VendorName = 0,
	/// <summary>제품 코드</summary>
	ProductCode = 1,
	/// <summary>제품 하위 리비전</summary>
	MajorMinorRevision = 2,
	/// <summary>벤더 URL</summary>
	VendorUrl = 3,
	/// <summary>제품 이름</summary>
	ProductName = 4,
	/// <summary>모델 이름</summary>
	ModelName = 5,
	/// <summary>어플리케이션 이름</summary>
	UserApplicationName = 6,
}

/// <summary>
/// 모드버스 오류 코드
/// </summary>
public enum ModBusErrorCode : byte
{
	/// <summary>문제없음</summary>
	[Description("문제없음")]
	NoError = 0,
	/// <summary>잘못된 기능</summary>
	[Description("잘못된 기능")]
	IllegalFunction = 1,
	/// <summary>잘못된 데이터 주소</summary>
	[Description("잘못된 데이터 주소")]
	IllegalDataAddress = 2,
	/// <summary>잘못된 데이터 값</summary>
	[Description("잘못된 데이터 값")]
	IllegalDataValue = 3,
	/// <summary>슬레이브 장치 실패</summary>
	[Description("슬레이브 장치 실패")]
	SlaveDeviceFailure = 4,
	/// <summary>승인</summary>
	[Description("승인")]
	Acknowledge = 5,
	/// <summary>슬레이브 장치 바쁨</summary>
	[Description("슬레이브 장치 바쁨")]
	SlaveDeviceBusy = 6,
	/// <summary>승인 부정</summary>
	[Description("승인 부정")]
	NegativeAcknowledge = 7,
	/// <summary>메모리 패리티 오류</summary>
	[Description("메모리 패리티 오류")]
	MemoryParityError = 8,
	/// <summary>게이트웨이 경로가 없음</summary>
	[Description("게이트웨이 경로가 없음")]
	GatewayPath = 10,
	/// <summary>게이트웨이 대상 장치가 반응이 없음</summary>
	[Description("게이트웨이 대상 장치가 반응이 없음")]
	GatewayTargetDevice = 11,

	/// <summary>연결 끊김</summary>
	[Description("연결 끊김")]
	Disconnected = 192,
	/// <summary>연결 실패</summary>
	[Description("연결 실패")]
	ConnectionFail = 193,
	/// <summary>시간 초과</summary>
	[Description("시간 초과")]
	ConnectionTimeout = 194,

	/// <summary>알 수 없어요</summary>
	[Description("알 수 없어요")]
	Unknown = 255,
}

/// <summary>
/// ModBus 통신에 대한 추적(트레이스) 옵션을 지정하는 열거형입니다. 어떤 종류의 데이터 작업을 추적할지 설정합니다.
/// </summary>
/// <remarks>이 열거형을 사용하여 ModBus 데이터 추적의 상세 수준을 제어할 수 있습니다. 읽기/쓰기 또는 둘 다에 대해
/// 추적을 활성화하려면 플래그를 조합해서 사용하세요. 이 열거형은 비트 연산으로 구성할 수 있습니다.</remarks>
[Flags]
public enum ModBusTraceMasks
{
	/// <summary>없음</summary>
	None = 0,
	/// <summary>읽기 데이터</summary>
	Read = 1,
	/// <summary>쓰기 데이터</summary>
	Write = 2,
	/// <summary>모든 데이터</summary>
	AllData = Read | Write,
}
