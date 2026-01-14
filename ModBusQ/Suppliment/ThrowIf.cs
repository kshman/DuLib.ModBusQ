using Du.Properties;

namespace Du.ModBusQ.Suppliment;

internal static class ThrowIf
{
	private static readonly ModBusErrorCode[] s_modbus_errors =
	[
		// 0=에러 없음
		ModBusErrorCode.IllegalFunction,
		ModBusErrorCode.IllegalDataAddress,
		ModBusErrorCode.IllegalDataValue,
		ModBusErrorCode.SlaveDeviceFailure,
		// 5=승인
		//ModBusErrorCode.SlaveDeviceBusy,
		//ModBusErrorCode.NegativeAcknowledge,
		//ModBusErrorCode.MemoryParityError,
		//ModBusErrorCode.GatewayPath,
		//ModBusErrorCode.GatewayTargetDevice,
	];

	internal static void ReadError(IReadOnlyList<byte> bs, ModBusFunction function)
	{
		var channel = (byte)(128 + function);
		if (bs[7] == channel && s_modbus_errors.Contains((ModBusErrorCode)bs[8]))
			throw new ModBusException((ModBusErrorCode)bs[8], Resources.ErrorOnRead);
	}
}
