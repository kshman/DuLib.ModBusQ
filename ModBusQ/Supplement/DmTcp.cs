using System.Net;
using System.Net.Sockets;

namespace Du.ModBusQ.Supplement;

/// <summary>
/// ModBus/TCP 및 관련 유틸리티를 제공하는 정적 헬퍼 클래스입니다.
/// NetworkStream 확장 메서드와 ModBus TCP 프레임 빌더 등을 포함합니다.
/// </summary>
internal static class DmTcp
{
	extension(NetworkStream nst)
	{
		/// <summary>
		/// NetworkStream에 바이트 배열을 모두 씁니다.
		/// </summary>
		internal void StreamWrite(byte[] buffer)
		{
			nst.Write(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// 응답 버퍼의 공통 헤더와 상태 필드를 채웁니다.
		/// </summary>
		/// <remarks>BuildBuffer는 이미 설정된 Transaction/Protocol/Identifier/Function/Count/Length 값을 사용합니다.</remarks>


		/// <summary>
		/// NetworkStream에서 지정한 바이트 수만큼 읽어 바이트 배열을 반환합니다.
		/// </summary>
		/// <param name="count">읽을 최대 바이트 수입니다.</param>
		/// <param name="read">실제로 읽은 바이트 수가 out 파라미터로 반환됩니다.</param>
		internal byte[] StreamRead(int count, out int read)
		{
			var buffer = new byte[count];
			read = nst.Read(buffer, 0, count);
			return buffer;
		}
	}

	/// <summary>
	/// ModBus/TCP용 MBAP 헤더와 PDU를 조합하여 전송 버퍼를 생성합니다.
	/// </summary>
	/// <param name="id">유닛 식별자(슬레이브 ID)</param>
	/// <param name="transaction">트랜잭션 식별자</param>
	/// <param name="address">시작 주소</param>
	/// <param name="countOrValue">갯수 또는 값</param>
	/// <param name="function">ModBus 기능 코드</param>
	/// <param name="length">MBAP 헤더의 길이 필드 값</param>
	/// <param name="size">생성할 배열의 최소 크기 (기본 12)</param>
	internal static byte[] BuildTcpBuffer(
		int id, uint transaction,
		int address, int countOrValue,
		ModBusFunction function, int length,
		int size = 12)
	{
		// 이거 엔디안 체크 해야하는거 아님....
		var bTransaction = BitConverter.GetBytes(transaction);
		var bLength = BitConverter.GetBytes(length);
		var bAddress = BitConverter.GetBytes(address);
		var bValue = BitConverter.GetBytes(countOrValue);

		var bs = new byte[]
		{
			// 트랜잭션
			bTransaction[1], bTransaction[0],
			// 프로토콜
			0, 0,
			// 길이
			bLength[1], bLength[0],
			// 유닛 아이디
			(byte)id,
			// 기능
			(byte)function,
			// 시작 주소
			bAddress[1], bAddress[0],
			// 갯수 또는 값
			bValue[1], bValue[0],
		};

		if (size > 12)
			Array.Resize(ref bs, size);

		return bs;
	}
}

/// <summary>
/// UDP 클라이언트 상태를 담는 간단한 DTO 클래스입니다.
/// </summary>
internal class UdpClientState(IPEndPoint endPoint, byte[] data)
{
	/// <summary>패킷 송신자 엔드포인트</summary>
	public IPEndPoint EndPoint { get; set; } = endPoint;
	/// <summary>수신된 바이트 버퍼</summary>
	public byte[] Buffer { get; set; } = data;
}

/// <summary>
/// TCP 클라이언트 상태를 담는 클래스입니다.
/// </summary>
internal class TcpClientState
{
	/// <summary>클라이언트에서 수신할 버퍼입니다.</summary>
	public byte[] Buffer { get; init; }
	/// <summary>클라이언트의 네트워크 스트림입니다.</summary>
	public NetworkStream Stream { get; init; }
	/// <summary>마지막으로 활동한 시간의 틱 값입니다.</summary>
	public long AliveTick { get; private set; }
	/// <summary>원격 클라이언트의 엔드포인트 정보입니다.</summary>
	public IPEndPoint RemoteEndPoint { get; set; }

	/// <summary>
	/// TcpClient로부터 상태 객체를 초기화합니다.
	/// </summary>
	/// <param name="tcp">초기화에 사용할 클라이언트 인스턴스</param>
	public TcpClientState(TcpClient tcp)
	{
		Buffer = new byte[tcp.ReceiveBufferSize];
		Stream = tcp.GetStream();
		RemoteEndPoint = (tcp.Client.RemoteEndPoint as IPEndPoint)!; // 이게 널일리가 없음

		Invalidate();
	}

	/// <summary>
	/// AliveTick을 현재 시각으로 갱신합니다.
	/// </summary>
	public void Invalidate()
		=> AliveTick = DateTime.Now.Ticks;

	/// <summary>
	/// 연결이 끊긴 상태로 표시합니다.
	/// </summary>
	public void MarkDisconnected()
		=> AliveTick = 0;
}

/// <summary>
/// ModBus 프로토콜의 공통 전송 헤더와 상태를 표현하는 기본 클래스입니다.
/// 요청(Request)과 응답(Response)이 공통으로 사용하는 트랜잭션, 프로토콜, 길이, 기능 코드 등
/// 기본 필드를 보관합니다.
/// </summary>
/// <remarks>
/// 이 클래스는 실제 바이트 직렬화/역직렬화 로직을 직접 포함하지 않고, 파생 클래스가
/// 자신에게 맞는 로직을 구현하도록 기본 속성들을 제공합니다.
/// </remarks>
internal class Transfer
{
	/// <summary>트랜잭션 식별자 (MBAP 헤더의 상위 바이트)</summary>
	public ushort Transaction { get; protected init; }
	/// <summary>프로토콜 식별자 (보통 0)</summary>
	public ushort Protocol { get; protected init; }
	/// <summary>패킷 길이(MBAP 헤더의 길이 필드)</summary>
	protected ushort Length { get; init; }

	/// <summary>유닛 식별자 (슬레이브 ID)</summary>
	public byte Identifier { get; protected init; }
	/// <summary>모드/기능 코드</summary>
	public byte Function { get; protected init; }
	/// <summary>바이트 카운트 또는 내부 카운트 필드</summary>
	protected byte Count { get; init; }

	/// <summary>CRC (RTU 프레임 등에서 사용될 수 있음)</summary>
	public ushort Crc { get; set; }

	/// <summary>시작 주소</summary>
	public ushort Address { get; protected init; }
	/// <summary>요청 또는 응답에 포함된 항목 수</summary>
	public ushort Quantity { get; protected init; }
	/// <summary>처리 결과 오류 코드</summary>
	public ModBusErrorCode Error { get; protected init; }
}

/// <summary>
/// 수신된 요청(Request) 패킷을 바탕으로 파싱된 정보를 보관하는 클래스입니다.
/// 트랜잭션 아이디, 프로토콜, 식별자, 기능 코드, 주소 및 개수 등 요청에 포함된 필드를
/// 바이트 배열에서 읽어 내부 속성으로 초기화합니다.
/// </summary>
/// <remarks>
/// 생성자는 원시 바이트 배열을 받아 필요한 경우 코일/레지스터 데이터 배열을 구성합니다.
/// 읽기 계열 함수와 쓰기 계열 함수 모두를 처리하도록 분기 로직이 포함되어 있습니다.
/// </remarks>
internal class Request : Transfer
{
	/// <summary>요청에 포함된 데이터(코일 또는 레지스터 값 등)</summary>
	public ushort[] Data { get; set; }

	/// <summary>
	/// 코일 읽기 응답을 구성할 때 필요한 바이트 수를 계산합니다.
	/// </summary>
	public int QuantityForBool => Quantity % 8 == 0 ? Quantity / 8 : (Quantity / 8) + 1;
	/// <summary>
	/// 레지스터(ushort) 읽기 응답에 필요한 바이트 수를 계산합니다.
	/// </summary>
	public int QuantityForUshort => 2 * Quantity;

	/// <summary>
	/// 원시 바이트 배열로부터 요청을 파싱하여 초기화합니다.
	/// </summary>
	/// <param name="arr">수신된 요청 패킷의 바이트 배열</param>
	public Request(byte[] arr)
	{
		Transaction = GetUshort(arr, 0);
		Protocol = GetUshort(arr, 2);
		Length = GetUshort(arr, 4);

		Identifier = arr[6];
		Function = arr[7];

		Address = GetUshort(arr, 8);
		Quantity = GetUshort(arr, 10);

		switch ((ModBusFunction)Function)
		{
			case ModBusFunction.ReadCoils:
			case ModBusFunction.ReadDiscreteInputs:
			case ModBusFunction.ReadHoldingRegisters:
			case ModBusFunction.ReadInputRegisters:
				Data = [];
				break;

			case ModBusFunction.WriteSingleCoil:
			case ModBusFunction.WriteSingleRegister:
				Data = [Quantity];
				break;

			case ModBusFunction.WriteMultipleCoils:
				Count = arr[12];
				Data = new ushort[Count % 2 == 0 ? Count / 2 : (Count / 2) + 1];
				Buffer.BlockCopy(arr, 13, Data, 0, Count);
				break;

			case ModBusFunction.WriteMultipleRegisters:
				Count = arr[12];
				Data = new ushort[Quantity];
				for (var i = 0; i < Quantity; i++)
					Data[i] = GetUshort(arr, 13 + (i * 2));
				break;

			case ModBusFunction.EncapsulatedInterface:
			default:
				Data = [];
				break;
		}
	}

	/// <summary>
	/// 바이트 배열에서 네트워크 바이트 오더(빅엔디안)로 저장된 ushort를 읽어 반환합니다.
	/// </summary>
	private static ushort GetUshort(byte[] bytes, int offset)
	{
		var bs = new[] { bytes[offset + 1], bytes[offset] };
		return BitConverter.ToUInt16(bs);
	}
}

/// <summary>
/// 응답(Response) 패킷을 구성하는 클래스입니다.
/// 생성자는 요청(Request)과 에러 코드 또는 데이터 길이를 받아 응답 버퍼를 생성하고
/// 내부적으로 바이트 순서를 맞춰 응답을 빌드합니다.
/// </summary>
/// <remarks>
/// 이 클래스는 읽기 응답(바이트/워드 데이터 포함)과 쓰기 응답(주소 및 값 에코)을 모두
/// 지원하는 헬퍼 메서드를 제공합니다.
/// </remarks>
internal class Response : Transfer
{
	/// <summary>응답 패킷의 바이트 배열입니다.</summary>
	public byte[] Buffer { get; init; }

	/// <summary>
	/// 에러 코드에 따라 오류 응답 패킷을 생성하는 생성자입니다.
	/// </summary>
	/// <param name="req">원본 요청 정보</param>
	/// <param name="error">응답할 오류 코드</param>
	public Response(Request req, ModBusErrorCode error)
	{
		Transaction = req.Transaction;
		Protocol = req.Protocol;
		Identifier = req.Identifier;
		Function = req.Function;
		Address = req.Address;
		Length = 3;
		Error = error;

		Buffer = new byte[9];
		BuildBuffer();
	}

	/// <summary>
	/// 요청에 대한 정상 응답 또는 오류 응답을 생성하는 생성자입니다.
	/// </summary>
	/// <param name="req">원본 요청 정보</param>
	/// <param name="quantity">응답에 포함할 데이터 바이트 수</param>
	/// <param name="maxQuantity">허용되는 최대 수량 검사 값</param>
	public Response(Request req, int quantity, int maxQuantity = 2000)
	{
		Transaction = req.Transaction;
		Protocol = req.Protocol;
		Identifier = req.Identifier;
		Function = req.Function;
		Address = req.Address;

		// 범위 검사: 주소/수량이 허용 범위를 벗어나면 오류 설정
		if (req.Address + 1 + req.Quantity > ushort.MaxValue)
			Error = ModBusErrorCode.IllegalDataAddress;
		if (req.Quantity == 0 || req.Quantity > maxQuantity)
			Error = ModBusErrorCode.IllegalDataValue;

		if (Error != ModBusErrorCode.NoError)
		{
			// 오류 응답: 고정 길이 버퍼 할당
			Length = 3;
			Buffer = new byte[9];
		}
		else
		{
			// 정상 응답: 바이트 카운트 및 버퍼 크기 설정
			Count = (byte)quantity;
			Length = (ushort)(9 + quantity - 6);
			Buffer = new byte[9 + quantity];
		}

		BuildBuffer();
	}

	/// <summary>
	/// Builds the ModBus protocol buffer based on the current transaction, protocol, and error state.
	/// </summary>
	/// <remarks>This method updates the internal buffer to reflect the current values of transaction, protocol,
	/// length, identifier, function, and error code. The buffer format follows the ModBus protocol specification. If an
	/// error is present, the function code and error code are encoded accordingly. This method is intended for internal
	/// use and should be called whenever the buffer needs to be refreshed after property changes.</remarks>
	private void BuildBuffer()
	{
		// 0,1 트랜잭션
		SetUshort(0, Transaction);
		// 2,3 프로토콜
		SetUshort(2, Protocol);
		// 4,5 길이
		SetUshort(4, Length);
		// 6 아이디
		Buffer[6] = Identifier;

		if (Error == ModBusErrorCode.NoError)
		{
			// 7 기능
			Buffer[7] = Function;
			// 8 바이트 개수
			Buffer[8] = Count;
		}
		else
		{
			// 7 기능
			Buffer[7] = (byte)(Function + 128);
			// 8 바이트 개수 또는 오류
			Buffer[8] = (byte)Error;
		}
	}

	/// <summary>
	/// 쓰기(single) 응답에 대해 주소와 부호 있는 값을 응답 버퍼에 기록합니다.
	/// </summary>
	/// <param name="value">기록할 부호 있는 값</param>
	public void AddWriteResponse(short value)
	{
		// 8,9 주소
		SetUshort(8, Address);
		// 10,11 값
		SetShort(10, value);
	}

	/// <summary>
	/// 쓰기(single) 응답에 대해 주소와 부호 없는 값을 응답 버퍼에 기록합니다.
	/// </summary>
	/// <param name="value">기록할 부호 없는 값</param>
	public void AddWriteResponse(ushort value)
	{
		// 8,9 주소
		SetUshort(8, Address);
		// 10,11 값
		SetUshort(10, value);
	}

	/// <summary>
	/// ushort 값을 네트워크 바이트 오더(빅 엔디안)로 응답 버퍼에 씁니다.
	/// </summary>
	private void SetUshort(int offset, ushort value)
	{
		var bs = BitConverter.GetBytes(value);
		Buffer[offset + 0] = bs[1];
		Buffer[offset + 1] = bs[0];
	}

	/// <summary>
	/// short 값을 네트워크 바이트 오더(빅 엔디안)로 응답 버퍼에 씁니다.
	/// </summary>
	private void SetShort(int offset, short value)
	{
		var bs = BitConverter.GetBytes(value);
		Buffer[offset + 0] = bs[1];
		Buffer[offset + 1] = bs[0];
	}
}

