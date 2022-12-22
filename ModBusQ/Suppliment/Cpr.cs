using Microsoft.Extensions.Logging;

namespace Du.ModBusQ.Suppliment;

internal static partial class Cpr
{
	#region 로그메시지
	[LoggerMessage(Level = LogLevel.Trace,
		EventId = 201, Message = "{method} 들어왔어요")]
	public static partial void MethodEnter(this ILogger logger, string method);
	[LoggerMessage(Level = LogLevel.Trace,
		EventId = 202, Message = "{method} 나가요")]
	public static partial void MethodLeave(this ILogger logger, string method);

	[LoggerMessage(Level = LogLevel.Trace,
		EventId = 203, Message = "{method}에서 연결이 끊긴거 같아요: {ex}")]
	public static partial void ProbablyDisconnected(this ILogger logger, string method, string ex);
	#endregion
}
