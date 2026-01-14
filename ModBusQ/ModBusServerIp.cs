using System.Net;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스  TCP/IP 서버 기반 클래스
/// </summary>
/// <remarks>
/// 새 인스턴스를 만들어요
/// </remarks>
public abstract class ModBusServerIp(int port, ILogger? logger) : ModBusServer(logger)
{
	/// <summary>리슨 주소</summary>
	public IPAddress Address { get; set; } = IPAddress.Any;
	/// <summary>리슨 포트</summary>
	public int Port { get; set; } = port;
}
