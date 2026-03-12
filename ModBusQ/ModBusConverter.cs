using Du.Properties;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 컨버터
/// </summary>
public static class ModBusConverter
{
    /// <summary>
	/// 레지스터 배열에서 지정된 오프셋과 길이에 따라 레지스터 순서를 검사하고
	/// 필요하면 워드(레지스터) 순서를 반전하여 새로운 배열을 반환합니다.
	/// 이 헬퍼는 다른 변환 메서드들이 빅/리틀 엔디언 또는 레지스터 순서가 뒤집힌
	/// 장치와 호환되도록 중복 처리를 담당합니다.
	/// </summary>
	/// <param name="inverse">레지스터 순서를 반전해야 하면 true.</param>
	/// <param name="rs">원본 레지스터 배열.</param>
	/// <param name="offset">처리할 시작 오프셋(배열 인덱스).</param>
	/// <param name="length">복사할 레지스터 수(2 또는 4 또는 임의 길이).</param>
	/// <returns>요청한 길이만큼 (필요시 반전된) 레지스터 배열.</returns>
	private static short[] TestInverse(bool inverse, short[] rs, int offset, int length)
	{

		if (length == 2)
		{
			return inverse ?
			[
				rs[offset + 1],
				rs[offset],
			] :
			[
				rs[offset],
				rs[offset + 1],
			];
		}
		else if (length == 4)
		{
			return inverse ?
			[
				rs[offset + 3],
				rs[offset + 2],
				rs[offset + 1],
				rs[offset],
			] :
			[
				rs[offset],
				rs[offset + 1],
				rs[offset + 2],
				rs[offset + 3],
			];
		}
		else if (offset == 0)
		{
			return inverse ? [.. rs.Reverse()] : rs;
		}
		else
		{
			short[] ns = new short[length];
			Array.Copy(rs, offset, ns, 0, length);
			return inverse ? [.. ns.Reverse()] : ns;
		}
	}

    /// <summary>
	/// ModBus 레지스터 2개를 Int32 값으로 변환합니다.
	/// 레지스터는 16비트 값으로 표현되며 내부적으로 바이트/워드 순서를 조합하여 32비트 정수를 만듭니다.
	/// </summary>
	/// <param name="registers">원본 레지스터 배열(각 요소는 16비트 값).</param>
	/// <param name="offset">변환을 시작할 레지스터 오프셋(기본: 0).</param>
	/// <param name="inverse">레지스터 블록의 워드 순서를 반전할지 여부(기본: false).</param>
	/// <returns>변환된 32비트 정수값.</returns>
	/// <exception cref="ArgumentException">레지스터가 2개 미만일 때 발생합니다.</exception>
	public static int ToModBusInt(this short[] registers, int offset = 0, bool inverse = false)
	{
		if (registers.Length - offset < 2)
			throw new ArgumentException(Resources.RegisterCountIsNotTwo);

		var rs = TestInverse(inverse, registers, offset, 2);
		var bs1 = BitConverter.GetBytes(rs[1]);
		var bs2 = BitConverter.GetBytes(rs[0]);

		return BitConverter.ToInt32(
		[
			bs2[0], bs2[1],
			bs1[0], bs1[1]
		], 0);
	}

    /// <summary>
	/// ModBus 레지스터 4개를 Int64(롱) 값으로 변환합니다.
	/// 4개의 16비트 레지스터를 결합하여 64비트 정수로 재구성합니다.
	/// </summary>
	/// <param name="registers">원본 레지스터 배열.</param>
	/// <param name="offset">변환 시작 오프셋(기본: 0).</param>
	/// <param name="inverse">레지스터 워드 순서를 반전할지 여부.</param>
	/// <returns>변환된 64비트 정수값.</returns>
	/// <exception cref="ArgumentException">레지스터가 4개 미만일 때 발생합니다.</exception>
	public static long ToModBusLong(this short[] registers, int offset = 0, bool inverse = false)
	{
		if (registers.Length - offset < 4)
			throw new ArgumentException(Resources.RegisterCountIsNotFour);

		var rs = TestInverse(inverse, registers, offset, 4);
		var bs1 = BitConverter.GetBytes(rs[3]);
		var bs2 = BitConverter.GetBytes(rs[2]);
		var bs3 = BitConverter.GetBytes(rs[1]);
		var bs4 = BitConverter.GetBytes(rs[0]);

		return BitConverter.ToInt64(
		[
			bs4[0], bs4[1],
			bs3[0], bs3[1],
			bs2[0], bs2[1],
			bs1[0], bs1[1]
		], 0);
	}

    /// <summary>
	/// ModBus 레지스터 2개를 IEEE 754 단정도(float) 값으로 변환합니다.
	/// </summary>
	/// <param name="registers">원본 레지스터 배열.</param>
	/// <param name="offset">변환 시작 오프셋(기본: 0).</param>
	/// <param name="inverse">레지스터 워드 순서를 반전할지 여부.</param>
	/// <returns>변환된 float 값.</returns>
	/// <exception cref="ArgumentException">레지스터가 2개 미만일 때 발생합니다.</exception>
	public static float ToModBusFloat(this short[] registers, int offset = 0, bool inverse = false)
	{
		if (registers.Length - offset < 2)
			throw new ArgumentException(Resources.RegisterCountIsNotTwo);

		var rs = TestInverse(inverse, registers, offset, 2);
		var bs1 = BitConverter.GetBytes(rs[1]);
		var bs2 = BitConverter.GetBytes(rs[0]);

		return BitConverter.ToSingle(
		[
			bs2[0], bs2[1],
			bs1[0], bs1[1]
		], 0);
	}

    /// <summary>
	/// ModBus 레지스터 4개를 IEEE 754 배정도(double) 값으로 변환합니다.
	/// </summary>
	/// <param name="registers">원본 레지스터 배열.</param>
	/// <param name="offset">변환 시작 오프셋(기본: 0).</param>
	/// <param name="inverse">레지스터 워드 순서를 반전할지 여부.</param>
	/// <returns>변환된 double 값.</returns>
	/// <exception cref="ArgumentException">레지스터가 4개 미만일 때 발생합니다.</exception>
	public static double ToModBusDouble(this short[] registers, int offset = 0, bool inverse = false)
	{
		if (registers.Length - offset < 4)
			throw new ArgumentException(Resources.RegisterCountIsNotFour);

		var rs = TestInverse(inverse, registers, offset, 4);
		var bs1 = BitConverter.GetBytes(rs[3]);
		var bs2 = BitConverter.GetBytes(rs[2]);
		var bs3 = BitConverter.GetBytes(rs[1]);
		var bs4 = BitConverter.GetBytes(rs[0]);

		return BitConverter.ToDouble(
		[
			bs4[0], bs4[1],
			bs3[0], bs3[1],
			bs2[0], bs2[1],
			bs1[0], bs1[1]
		], 0);
	}

    /// <summary>
	/// 문자열 변환시 사용되는 트림 문자 집합(공백, 탭, 널, 개행 등).
	/// </summary>
	private static readonly char[] s_trim_chars = [' ', '\t', '\0', '\r', '\n'];

    /// <summary>
	/// ModBus 레지스터 블록을 문자열로 변환합니다.
	/// 각 레지스터는 2바이트이므로 length는 바이트 수를 의미하며
	/// flip이 true이면 각 레지스터의 바이트 순서를 반전하여 처리합니다.
	/// </summary>
	/// <param name="registers">원본 레지스터 배열.</param>
	/// <param name="offset">문자열 변환을 시작할 레지스터 오프셋.</param>
	/// <param name="length">문자열로 읽을 바이트 길이(레지스터 수 * 2).</param>
	/// <param name="flip">레지스터 내 바이트 순서를 반전할지 여부(기본: false).</param>
	/// <returns>UTF8로 디코딩된 문자열(널 이후는 잘라냄).</returns>
	public static string ToModBusString(this short[] registers, int offset, int length, bool flip = false)
	{
		var bs = new byte[length];
		if (flip)
		{
			for (var i = 0; i < length / 2; i++)
			{
				var tb = BitConverter.GetBytes(registers[offset + i]);
				bs[i * 2] = tb[1];
				bs[(i * 2) + 1] = tb[0];
			}
		}
		else
		{
			for (var i = 0; i < length / 2; i++)
			{
				var tb = BitConverter.GetBytes(registers[offset + i]);
				bs[i * 2] = tb[0];
				bs[(i * 2) + 1] = tb[1];
			}
		}

		var s = Encoding.UTF8.GetString(bs).Trim(s_trim_chars);
		var n = s.IndexOf('\0');

		return n >= 0 ? s[..n] : s;
	}

    /// <summary>
	/// 레지스터에서 ModBus 표준 방식으로 해석한 32비트 정수값을 반환합니다.
	/// 단순 호출 래퍼로 내부적으로 ToModBusInt를 사용합니다.
	/// </summary>
	/// <param name="registers">원본 레지스터 배열.</param>
	/// <param name="offset">변환 시작 오프셋.</param>
	/// <returns>32비트 정수값.</returns>
	/// <exception cref="ArgumentException">레지스터가 부족한 경우 발생합니다.</exception>
	public static int ToModBusRawInt(this short[] registers, int offset = 0)
		=> ToModBusInt(registers, offset);

    /// <summary>
	/// 레지스터에서 ModBus 표준 방식으로 해석한 64비트 정수값을 반환합니다.
	/// 내부적으로 ToModBusLong를 호출하는 래퍼입니다.
	/// </summary>
	/// <param name="registers">원본 레지스터 배열.</param>
	/// <param name="offset">변환 시작 오프셋.</param>
	/// <returns>64비트 정수값.</returns>
	/// <exception cref="ArgumentException">레지스터가 부족한 경우 발생합니다.</exception>
	public static long ToModBusRawLong(this short[] registers, int offset = 0)
		=> ToModBusLong(registers, offset);

   /// <summary>
	/// 32비트 정수값을 ModBus 레지스터 배열(각 요소는 16비트)로 변환합니다.
	/// 반환 배열은 레지스터 순서(워드 순서)를 나타내며 필요시 <paramref name="inverse"/>로 반전됩니다.
	/// </summary>
	/// <param name="value">변환할 32비트 정수값.</param>
	/// <param name="inverse">레지스터 순서를 반전할지 여부(기본: false).</param>
	/// <returns>길이 2의 레지스터 배열.</returns>
	public static short[] ToModBusRegister(this int value, bool inverse = false)
	{
		var bs = BitConverter.GetBytes(value);
		var rs = new short[]
		{
			BitConverter.ToInt16(bs, 0),
			BitConverter.ToInt16(bs, 2)
		};

		return TestInverse(inverse, rs, 0, 2);
	}

   /// <summary>
	/// 64비트 정수값을 ModBus 레지스터 배열(각 요소는 16비트)로 변환합니다.
	/// 반환 배열은 총 4개의 레지스터로 구성되며 필요시 워드 순서를 반전합니다.
	/// </summary>
	/// <param name="value">변환할 64비트 정수값.</param>
	/// <param name="inverse">레지스터 순서를 반전할지 여부(기본: false).</param>
	/// <returns>길이 4의 레지스터 배열.</returns>
	public static short[] ToModBusRegister(this long value, bool inverse = false)
	{
		var bs = BitConverter.GetBytes(value);
		var rs = new short[]
		{
			BitConverter.ToInt16(bs, 0),
			BitConverter.ToInt16(bs, 2),
			BitConverter.ToInt16(bs, 4),
			BitConverter.ToInt16(bs, 6),
		};

		return TestInverse(inverse, rs, 0, 4);
	}

   /// <summary>
	/// 단정도(float) 값을 ModBus 레지스터 배열(2개 레지스터)로 변환합니다.
	/// 필요시 워드 순서를 반전하여 반환합니다.
	/// </summary>
	/// <param name="value">변환할 float 값.</param>
	/// <param name="inverse">레지스터 순서를 반전할지 여부(기본: false).</param>
	/// <returns>길이 2의 레지스터 배열.</returns>
	public static short[] ToModBusRegister(this float value, bool inverse = false)
	{
		var bs = BitConverter.GetBytes(value);
		var rs = new short[]
		{
			BitConverter.ToInt16(bs, 0),
			BitConverter.ToInt16(bs, 2)
		};
		return TestInverse(inverse, rs, 0, 2);
	}

   /// <summary>
	/// 배정도(double) 값을 ModBus 레지스터 배열(4개 레지스터)로 변환합니다.
	/// 필요시 워드 순서를 반전하여 반환합니다.
	/// </summary>
	/// <param name="value">변환할 double 값.</param>
	/// <param name="inverse">레지스터 순서를 반전할지 여부(기본: false).</param>
	/// <returns>길이 4의 레지스터 배열.</returns>
	public static short[] ToModBusRegister(this double value, bool inverse = false)
	{
		var bs = BitConverter.GetBytes(value);
		var rs = new short[]
		{
			BitConverter.ToInt16(bs, 0),
			BitConverter.ToInt16(bs, 2),
			BitConverter.ToInt16(bs, 4),
			BitConverter.ToInt16(bs, 6),
		};

		return TestInverse(inverse, rs, 0, 4);
	}

   /// <summary>
	/// 문자열을 ModBus 레지스터 배열로 변환합니다. 문자열은 UTF8로 인코딩되며
	/// 각 레지스터는 2바이트를 저장합니다. 길이가 홀수인 경우 마지막 레지스터의 상위/하위 바이트는 0으로 채워집니다.
	/// </summary>
	/// <param name="value">변환할 문자열.</param>
	/// <param name="flip">각 레지스터 내 바이트 순서를 뒤집을지 여부(기본: false).</param>
	/// <returns>문자열을 담은 레지스터 배열.</returns>
	public static short[] ToModBusRegister(this string value, bool flip = false)
	{
		var bs = Encoding.UTF8.GetBytes(value);
		var rs = new short[(value.Length / 2) + (value.Length % 2)];
		if (flip)
		{
			for (var i = 0; i < bs.Length; i++)
			{
				rs[i] = (short)(bs[i * 2] << 8);
				if (((i * 2) + 1) < bs.Length)
					rs[i] = (short)((ushort)rs[i] | (ushort)bs[(i * 2) + 1]);
			}
		}
		else
		{
			for (var i = 0; i < bs.Length; i++)
			{
				rs[i] = (short)bs[i * 2];
				if (((i * 2) + 1) < bs.Length)
					rs[i] = (short)((ushort)rs[i] | (ushort)(bs[(i * 2) + 1] << 8));
			}
		}

		return rs;
	}
}
