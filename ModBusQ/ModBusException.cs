namespace Du.ModBusQ;

/// <summary>
/// 모드버스 예외
/// </summary>
public class ModBusException : Exception
{
	/// <summary>오류 코드</summary>
	public ModBusErrorCode ErrorCode { get; } = ModBusErrorCode.Unknown;

	/// <summary>
	/// 기본 ModBus 예외를 초기화합니다.
	/// </summary>
	public ModBusException()
	{
	}

	/// <summary>
	/// 지정된 메시지로 ModBus 예외를 초기화합니다.
	/// </summary>
	/// <param name="message">예외 설명 메시지입니다.</param>
	public ModBusException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 내부 예외를 포함하여 ModBus 예외를 초기화합니다.
	/// </summary>
	/// <param name="message">예외 설명 메시지입니다.</param>
	/// <param name="innerException">원인이 되는 내부 예외입니다.</param>
	public ModBusException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// 오류 코드를 지정하여 ModBus 예외를 초기화합니다.
	/// </summary>
	/// <param name="error">예외와 관련된 ModBus 오류 코드입니다.</param>
	public ModBusException(ModBusErrorCode error)
	{
		ErrorCode = error;
	}

	/// <summary>
	/// 오류 코드와 메시지를 지정하여 ModBus 예외를 초기화합니다.
	/// </summary>
	/// <param name="error">예외와 관련된 ModBus 오류 코드입니다.</param>
	/// <param name="message">예외 설명 메시지입니다.</param>
	public ModBusException(ModBusErrorCode error, string message)
		: base(message)
	{
		ErrorCode = error;
	}

	/// <summary>
	/// 오류 코드, 메시지 및 내부 예외를 지정하여 ModBus 예외를 초기화합니다.
	/// </summary>
	/// <param name="error">예외와 관련된 ModBus 오류 코드입니다.</param>
	/// <param name="message">예외 설명 메시지입니다.</param>
	/// <param name="innerException">원인이 되는 내부 예외입니다.</param>
	public ModBusException(ModBusErrorCode error, string message, Exception innerException)
		: base(message, innerException)
	{
		ErrorCode = error;
	}
}

/// <summary>
/// 모드버스 예외
/// </summary>
public class ModBusConnectionException : Exception
{
	/// <summary>오류 코드</summary>
	public ModBusErrorCode ErrorCode { get; } = ModBusErrorCode.Unknown;

	/// <summary>
	/// 기본 ModBus 연결 예외를 초기화합니다.
	/// </summary>
	public ModBusConnectionException()
	{
	}

	/// <summary>
	/// 지정된 메시지로 ModBus 연결 예외를 초기화합니다.
	/// </summary>
	/// <param name="message">예외 설명 메시지입니다.</param>
	public ModBusConnectionException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 내부 예외를 포함하여 ModBus 연결 예외를 초기화합니다.
	/// </summary>
	/// <param name="message">예외 설명 메시지입니다.</param>
	/// <param name="innerException">원인이 되는 내부 예외입니다.</param>
	public ModBusConnectionException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// 오류 코드를 지정하여 ModBus 연결 예외를 초기화합니다.
	/// </summary>
	/// <param name="error">예외와 관련된 ModBus 오류 코드입니다.</param>
	public ModBusConnectionException(ModBusErrorCode error)
	{
		ErrorCode = error;
	}

	/// <summary>
	/// 오류 코드와 메시지를 지정하여 ModBus 연결 예외를 초기화합니다.
	/// </summary>
	/// <param name="error">예외와 관련된 ModBus 오류 코드입니다.</param>
	/// <param name="message">예외 설명 메시지입니다.</param>
	public ModBusConnectionException(ModBusErrorCode error, string message)
		: base(message)
	{
		ErrorCode = error;
	}

	/// <summary>
	/// 오류 코드, 메시지 및 내부 예외를 지정하여 ModBus 연결 예외를 초기화합니다.
	/// </summary>
	/// <param name="error">예외와 관련된 ModBus 오류 코드입니다.</param>
	/// <param name="message">예외 설명 메시지입니다.</param>
	/// <param name="innerException">원인이 되는 내부 예외입니다.</param>
	public ModBusConnectionException(ModBusErrorCode error, string message, Exception innerException)
		: base(message, innerException)
	{
		ErrorCode = error;
	}
}
