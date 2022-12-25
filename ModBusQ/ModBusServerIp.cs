using System.Net;
using Microsoft.Extensions.Logging;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스  TCP/IP 서버 기반 클래스
/// </summary>
public abstract class ModBusServerIp : ModBusServer
{
	/// <summary>리슨 주소</summary>
	public IPAddress Address { get; set; }
	/// <summary>리슨 포트</summary>
	public int Port { get; set; }

	/// <summary>
	/// 새 인스턴스를 만들어요
	/// </summary>
	protected ModBusServerIp(int port, ILogger? logger)
		: base(logger)
	{
		Address = IPAddress.Any;
		Port = port;
	}
}
