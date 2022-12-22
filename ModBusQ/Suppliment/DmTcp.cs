using System.Net.Sockets;

namespace Du.ModBusQ.Suppliment;

internal static class DmTcp
{
	internal static void StreamWrite(this NetworkStream nst, byte[] buffer)
	{
		nst.Write(buffer, 0, buffer.Length);
	}

	internal static byte[] StreamRead(this NetworkStream nst, int count, out int read)
	{
		var buffer = new byte[count];
		read = nst.Read(buffer, 0, count);
		return buffer;
	}

	internal static byte[] BuildTcpBuffer(
		int id, uint transaction,
		int address, int count_or_value,
		ModBusFunction function, int length,
		int size = 12)
	{
		// 이거 엔디안 체크 해야하는거 아님....
		var btrn = BitConverter.GetBytes(transaction);
		var blen = BitConverter.GetBytes(length);
		var bsa = BitConverter.GetBytes(address);
		var bcnv = BitConverter.GetBytes(count_or_value);

		var bs = new byte[]
		{
			// 트랜잭션
			btrn[1], btrn[0],
			// 프로토콜
			0, 0,
			// 길이
			blen[1], blen[0],
			// 유닛 아이디
			(byte)id,
			// 기능
			(byte)function,
			// 시작 주소
			bsa[1], bsa[0],
			// 갯수 또는 값
			bcnv[1], bcnv[0],
		};

		if (size > 12)
			Array.Resize(ref bs, size);

		return bs;
	}
}
