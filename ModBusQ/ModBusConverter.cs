using Du.Properties;

namespace Du.ModBusQ;

/// <summary>
/// 모드버스 컨버터
/// </summary>
public static class ModBusConverter
{
	private static int[] TestInverse(bool inverse, int[] rs, int offset, int length)
	{

		if (length == 2)
		{
			return inverse ? new[]
			{
				rs[offset + 1],
				rs[offset],
			} : new[]
			{
				rs[offset],
				rs[offset + 1],
			};
		}
		else if (length == 4)
		{
			return inverse ? new[]
			{
				rs[offset + 3],
				rs[offset + 2],
				rs[offset + 1],
				rs[offset],
			} : new[]
			{
				rs[offset],
				rs[offset + 1],
				rs[offset + 2],
				rs[offset + 3],
			};
		}
		else if (offset == 0)
			return inverse ? rs.Reverse().ToArray() : rs;
		else
		{
			int[] ns = new int[length];
			Array.Copy(rs, offset, ns, 0, length);
			return inverse ? ns.Reverse().ToArray() : ns;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="registers"></param>
	/// <param name="offset"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static int ToModBusInt(this int[] registers, int offset = 0, bool inverse = false)
	{
		if (registers.Length - offset < 2)
			throw new ArgumentException(Resources.RegisterCountIsNotTwo);

		var rs = TestInverse(inverse, registers, offset, 2);
		var bs1 = BitConverter.GetBytes(rs[1]);
		var bs2 = BitConverter.GetBytes(rs[0]);

		return BitConverter.ToInt32(new[]
		{
			bs2[0], bs2[1],
			bs1[0], bs1[1]
		}, 0);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="registers"></param>
	/// <param name="offset"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static long ToModBusLong(this int[] registers, int offset = 0, bool inverse = false)
	{
		if (registers.Length - offset < 4)
			throw new ArgumentException(Resources.RegisterCountIsNotFour);

		var rs = TestInverse(inverse, registers, offset, 4);
		var bs1 = BitConverter.GetBytes(rs[3]);
		var bs2 = BitConverter.GetBytes(rs[2]);
		var bs3 = BitConverter.GetBytes(rs[1]);
		var bs4 = BitConverter.GetBytes(rs[0]);

		return BitConverter.ToInt64(new[]
		{
			bs4[0], bs4[1],
			bs3[0], bs3[1],
			bs2[0], bs2[1],
			bs1[0], bs1[1]
		}, 0);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="registers"></param>
	/// <param name="offset"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static float ToModBusFloat(this int[] registers, int offset = 0, bool inverse = false)
	{
		if (registers.Length - offset < 2)
			throw new ArgumentException(Resources.RegisterCountIsNotTwo);

		var rs = TestInverse(inverse, registers, offset, 2);
		var bs1 = BitConverter.GetBytes(rs[1]);
		var bs2 = BitConverter.GetBytes(rs[0]);

		return BitConverter.ToSingle(new[]
		{
			bs2[0], bs2[1],
			bs1[0], bs1[1]
		}, 0);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="registers"></param>
	/// <param name="offset"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static double ToModBusDouble(this int[] registers, int offset = 0, bool inverse = false)
	{
		if (registers.Length - offset < 4)
			throw new ArgumentException(Resources.RegisterCountIsNotFour);

		var rs = TestInverse(inverse, registers, offset, 4);
		var bs1 = BitConverter.GetBytes(rs[3]);
		var bs2 = BitConverter.GetBytes(rs[2]);
		var bs3 = BitConverter.GetBytes(rs[1]);
		var bs4 = BitConverter.GetBytes(rs[0]);

		return BitConverter.ToDouble(new[]
		{
			bs4[0], bs4[1],
			bs3[0], bs3[1],
			bs2[0], bs2[1],
			bs1[0], bs1[1]
		}, 0);
	}

	private static readonly char[] s_trim_chars = { ' ', '\t', '\0', '\r', '\n' };

	/// <summary>
	/// 
	/// </summary>
	/// <param name="registers"></param>
	/// <param name="offset"></param>
	/// <param name="length"></param>
	/// <param name="flip"></param>
	/// <returns></returns>
	public static string ToModBusString(this int[] registers, int offset, int length, bool flip = false)
	{
		var bs = new byte[length];
		if (flip)
		{
			for (var i = 0; i < length / 2; i++)
			{
				var tb = BitConverter.GetBytes(registers[offset + i]);
				bs[i * 2] = tb[1];
				bs[i * 2 + 1] = tb[0];
			}
		}
		else
		{
			for (var i = 0; i < length / 2; i++)
			{
				var tb = BitConverter.GetBytes(registers[offset + i]);
				bs[i * 2] = tb[0];
				bs[i * 2 + 1] = tb[1];
			}
		}

		var s = Encoding.UTF8.GetString(bs).Trim(s_trim_chars);
		var n = s.IndexOf('\0');

		return n >= 0 ? s[..n] : s;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="registers"></param>
	/// <param name="offset"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static int ToModBusRawInt(this int[] registers, int offset = 0)
		=> ToModBusInt(registers, offset);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="registers"></param>
	/// <param name="offset"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static long ToModBusRawLong(this int[] registers, int offset = 0)
		=> ToModBusLong(registers, offset);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="value"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	public static int[] ToModBusRegister(this int value, bool inverse = false)
	{
		var bs = BitConverter.GetBytes(value);
		var rs = new[]
		{
			BitConverter.ToInt32(new byte[] { bs[0], bs[1], 0, 0, }, 0),
			BitConverter.ToInt32(new byte[] { bs[2], bs[3], 0, 0, }, 0)
		};

		return TestInverse(inverse, rs, 0, 2);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="value"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	public static int[] ToModBusRegister(this long value, bool inverse = false)
	{
		var bs = BitConverter.GetBytes(value);
		var rs = new[]
		{
			BitConverter.ToInt32(new byte[] { bs[0], bs[1], 0, 0, }, 0),
			BitConverter.ToInt32(new byte[] { bs[2], bs[3], 0, 0, }, 0),
			BitConverter.ToInt32(new byte[] { bs[4], bs[5], 0, 0, }, 0),
			BitConverter.ToInt32(new byte[] { bs[6], bs[7], 0, 0, }, 0),
		};

		return TestInverse(inverse, rs, 0, 4);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="value"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	public static int[] ToModBusRegister(this float value, bool inverse = false)
	{
		var bs = BitConverter.GetBytes(value);
		var rs = new[]
		{
			BitConverter.ToInt32(new byte[] { bs[0], bs[1], 0, 0, }, 0),
			BitConverter.ToInt32(new byte[] { bs[2], bs[3], 0, 0, }, 0)
		};
		return TestInverse(inverse, rs, 0, 2);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="value"></param>
	/// <param name="inverse"></param>
	/// <returns></returns>
	public static int[] ToModBusRegister(this double value, bool inverse = false)
	{
		var bs = BitConverter.GetBytes(value);
		var rs = new[]
		{
			BitConverter.ToInt32(new byte[] { bs[0], bs[1], 0, 0, }, 0),
			BitConverter.ToInt32(new byte[] { bs[2], bs[3], 0, 0, }, 0),
			BitConverter.ToInt32(new byte[] { bs[4], bs[5], 0, 0, }, 0),
			BitConverter.ToInt32(new byte[] { bs[6], bs[7], 0, 0, }, 0),
		};

		return TestInverse(inverse, rs, 0, 4);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="value"></param>
	/// <param name="flip"></param>
	/// <returns></returns>
	public static int[] ToModBusRegister(this string value, bool flip = false)
	{
		var bs = Encoding.UTF8.GetBytes(value);
		var rs = new int[value.Length / 2 + value.Length % 2];
		if (flip)
		{
			for (var i = 0; i < bs.Length; i++)
			{
				rs[i] = bs[i * 2] << 8;
				if ((i * 2 + 1) < bs.Length)
					rs[i] |= bs[i * 2 + 1];
			}
		}
		else
		{
			for (var i = 0; i < bs.Length; i++)
			{
				rs[i] = bs[i * 2];
				if ((i * 2 + 1) < bs.Length)
					rs[i] |= bs[i * 2 + 1] << 8;
			}
		}

		return rs;
	}
}
