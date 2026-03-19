namespace Du.ModBusQ.Supplement;

// 모드버스 디바이스 구현
/// <summary>
/// ModBus 디바이스(슬레이브)를 표현하는 내부 구현 클래스입니다.
/// 코일, 디스크릿 입력, 입력/홀딩 레지스터 상태를 스레드 안전하게 관리합니다.
/// </summary>
/// <param name="id">디바이스 식별자(슬레이브 ID).</param>
internal class Device(int id)
{
	private readonly ReaderWriterLockSlim _lockCoils = new();
	private readonly ReaderWriterLockSlim _lockDiscreteInputs = new();
	private readonly ReaderWriterLockSlim _lockInputRegisters = new();
	private readonly ReaderWriterLockSlim _lockHoldingRegisters = new();

	private readonly HashSet<int> _coils = [];
	private readonly HashSet<int> _discreteInputs = [];
	private readonly Dictionary<int, short> _inputRegisters = [];
	private readonly Dictionary<int, short> _holdingRegisters = [];

	/// <summary>디바이스 식별자(슬레이브 ID).</summary>
	public byte Id { get; } = (byte)id;

	/// <summary>
	/// 지정한 주소의 코일 상태를 읽어 반환합니다.
	/// </summary>
	/// <param name="address">읽을 코일 주소입니다.</param>
	/// <returns>코일이 설정되어 있으면 true, 아니면 false를 반환합니다.</returns>
	public bool GetCoil(int address)
	{
		using (_lockCoils.GetReadLock())
			return _coils.Contains(address);
	}

	/// <summary>
	/// 지정한 주소의 코일 값을 설정하거나 해제합니다.
	/// </summary>
	/// <param name="address">설정할 코일 주소입니다.</param>
	/// <param name="value">설정할 값(참이면 설정, 거짓이면 해제).</param>
	public void SetCoil(int address, bool value)
	{
		using (_lockCoils.GetWriteLock())
		{
			switch (value)
			{
				case true when !_coils.Contains(address):
					_coils.Add(address);
					break;
				case false when _coils.Contains(address):
					_coils.Remove(address);
					break;
			}
		}
	}

	/// <summary>
	/// 연속된 여러 코일 값을 설정합니다.
	/// </summary>
	/// <param name="address">설정 시작 주소입니다.</param>
	/// <param name="values">설정할 값 배열입니다.</param>
	public void SetCoils(int address, bool[] values)
	{
		using (_lockCoils.GetWriteLock())
		{
			for (var i = 0; i < values.Length; i++)
			{
				var addr = address + i;
				switch (values[i])
				{
					case true when !_coils.Contains(addr):
						_coils.Add(addr);
						break;
					case false when _coils.Contains(addr):
						_coils.Remove(addr);
						break;
				}
			}
		}
	}

	/// <summary>
	/// 지정한 주소의 디스크릿 입력 값을 읽어 반환합니다.
	/// </summary>
	/// <param name="address">읽을 입력 주소입니다.</param>
	/// <returns>입력이 활성화되어 있으면 true를 반환합니다.</returns>
	public bool GetDiscreteInput(int address)
	{
		using (_lockDiscreteInputs.GetReadLock())
			return _discreteInputs.Contains(address);
	}

	/// <summary>
	/// 지정한 주소의 디스크릿 입력 값을 설정하거나 해제합니다.
	/// </summary>
	/// <param name="address">설정할 입력 주소입니다.</param>
	/// <param name="value">설정할 값입니다.</param>
	public void SetDiscreteInput(int address, bool value)
	{
		using (_lockDiscreteInputs.GetWriteLock())
		{
			switch (value)
			{
				case true when !_discreteInputs.Contains(address):
					_discreteInputs.Add(address);
					break;
				case false when _discreteInputs.Contains(address):
					_discreteInputs.Remove(address);
					break;
			}
		}
	}

	/// <summary>
	/// 지정한 주소의 입력 레지스터 값을 읽어 반환합니다.
	/// </summary>
	/// <param name="address">읽을 레지스터 주소입니다.</param>
	/// <returns>레지스터 값(없으면 0)을 반환합니다.</returns>
	public short GetInputRegister(int address)
	{
		using (_lockInputRegisters.GetReadLock())
		{
			if (_inputRegisters.TryGetValue(address, out var value))
				return value;
		}
		return 0;
	}

	/// <summary>
	/// 지정한 주소의 입력 레지스터 값을 설정합니다. 값이 0 이하이면 레지스터를 제거합니다.
	/// </summary>
	/// <param name="address">설정할 레지스터 주소입니다.</param>
	/// <param name="value">설정할 16비트 값입니다.</param>
	public void SetInputRegister(int address, short value)
	{
		using (_lockInputRegisters.GetWriteLock())
		{
			if (value > 0)
				_inputRegisters[address] = value;
			else
				_inputRegisters.Remove(address);
		}
	}

	/// <summary>
	/// 지정한 주소의 홀딩 레지스터 값을 읽어 반환합니다.
	/// </summary>
	/// <param name="address">읽을 레지스터 주소입니다.</param>
	/// <returns>레지스터 값(없으면 0)을 반환합니다.</returns>
	public short GetHoldingRegister(int address)
	{
		using (_lockHoldingRegisters.GetReadLock())
		{
			if (_holdingRegisters.TryGetValue(address, out var value))
				return value;
		}
		return 0;
	}

	/// <summary>
	/// 지정한 주소의 홀딩 레지스터 값을 설정합니다. 값이 0 이하이면 레지스터를 제거합니다.
	/// </summary>
	/// <param name="address">설정할 레지스터 주소입니다.</param>
	/// <param name="value">설정할 16비트 값입니다.</param>
	public void SetHoldingRegister(int address, short value)
	{
		using (_lockHoldingRegisters.GetWriteLock())
		{
			if (value > 0)
				_holdingRegisters[address] = value;
			else
				_holdingRegisters.Remove(address);
		}
	}

	/// <summary>
	/// 연속된 홀딩 레지스터들을 설정합니다.
	/// </summary>
	/// <param name="address">설정 시작 주소입니다.</param>
	/// <param name="values">설정할 16비트 값 배열입니다.</param>
	public void SetHoldingRegisters(int address, short[] values)
	{
		using (_lockHoldingRegisters.GetWriteLock())
		{
			for (var i = 0; i < values.Length; i++)
			{
				var addr = address + i;
				if (values[i] > 0)
					_holdingRegisters[addr] = values[i];
				else
					_holdingRegisters.Remove(addr);
			}
		}
	}
}

