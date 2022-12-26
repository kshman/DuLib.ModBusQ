namespace Du.ModBusQ.Suppliment;

// 모드버스 디바이스 구현
internal class Device
{
	private readonly ReaderWriterLockSlim _lock_coils = new();
	private readonly ReaderWriterLockSlim _lock_discrete_inputs = new();
	private readonly ReaderWriterLockSlim _lock_input_registers = new();
	private readonly ReaderWriterLockSlim _lock_holding_registers = new();

	private readonly HashSet<int> _coils = new();
	private readonly HashSet<int> _discrete_inputs = new();
	private readonly Dictionary<int, ushort> _input_registers = new();
	private readonly Dictionary<int, ushort> _holding_registers = new();

	public byte Id { get; }

	public Device(int id)
	{
		Id = (byte)id;
	}

	public bool GetCoil(int address)
	{
		using (_lock_coils.GetReadLock())
			return _coils.Contains(address);
	}

	public void SetCoil(int address, bool value)
	{
		using (_lock_coils.GetWriteLock())
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

	public void SetCoils(int address, bool[] values)
	{
		using (_lock_coils.GetWriteLock())
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

	public bool GetDiscreteInput(int address)
	{
		using (_lock_discrete_inputs.GetReadLock())
			return _discrete_inputs.Contains(address);
	}

	public void SetDiscreteInput(int address, bool value)
	{
		using (_lock_discrete_inputs.GetWriteLock())
		{
			switch (value)
			{
				case true when !_discrete_inputs.Contains(address):
					_discrete_inputs.Add(address);
					break;
				case false when _discrete_inputs.Contains(address):
					_discrete_inputs.Remove(address);
					break;
			}
		}
	}

	public ushort GetInputRegister(int address)
	{
		using (_lock_input_registers.GetReadLock())
		{
			if (_input_registers.TryGetValue(address, out var value))
				return value;
		}
		return 0;
	}

	public void SetInputRegister(int address, ushort value)
	{
		using (_lock_input_registers.GetWriteLock())
		{
			if (value > 0)
				_input_registers[address] = value;
			else
				_input_registers.Remove(address);
		}
	}

	public int GetHoldingRegister(int address)
	{
		using (_lock_holding_registers.GetReadLock())
		{
			if (_holding_registers.TryGetValue(address, out var value))
				return value;
		}
		return 0;
	}

	public void SetHoldingRegister(int address, int value)
	{
		using (_lock_holding_registers.GetWriteLock())
		{
			if (value > 0)
				_holding_registers[address] = (ushort)value;
			else
				_holding_registers.Remove(address);
		}
	}

	public void SetHoldingRegisters(int address, int[] values)
	{
		using (_lock_holding_registers.GetWriteLock())
		{
			for (var i = 0; i < values.Length; i++)
			{
				var addr = address + i;
				if (values[i] > 0)
					_holding_registers[addr] = (ushort)values[i];
				else
					_holding_registers.Remove(addr);
			}
		}
	}
}

