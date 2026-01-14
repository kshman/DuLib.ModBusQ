using System.Runtime.Serialization;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 예외
/// </summary>
public class ModBusException : Exception
{
	/// <summary>오류 코드</summary>
	public ModBusErrorCode ErrorCode = ModBusErrorCode.Unknown;

	/// <summary>
	/// 컨스트럭트
	/// </summary>
	public ModBusException()
	{
	}

	/// <summary>
	/// 컨스트럭트
	/// </summary>
	public ModBusException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 컨스트럭트
	/// </summary>
	public ModBusException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// 컨스트럭트
	/// </summary>
	public ModBusException(ModBusErrorCode error)
	{
		ErrorCode = error;
	}

	/// <summary>
	/// 컨스트럭트
	/// </summary>
	public ModBusException(ModBusErrorCode error, string message)
		: base(message)
	{
		ErrorCode = error;
	}

	/// <summary>
	/// 컨스트럭트
	/// </summary>
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
	/// <summary>
	/// 컨스트럭트
	/// </summary>
	public ModBusConnectionException()
	{
	}

	/// <summary>
	/// 컨스트럭트
	/// </summary>
	public ModBusConnectionException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 컨스트럭트
	/// </summary>
	public ModBusConnectionException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
