namespace RacingCarsController.Common
{
    public class FerrariCar : RacingCar
    {
        public override string ControlServiceUUID => "0000fff0-0000-1000-8000-00805f9b34fb";
        public override string ControlCharacteristicUUID => "0000fff1-0000-1000-8000-00805f9b34fb";

        public override string BatteryServiceUUID => "0000180f-0000-1000-8000-00805f9b34fb";
        public override string BatteryCharacteristicUUID => "00002a19-0000-1000-8000-00805f9b34fb";

        public FerrariCar(IBLEDevice device, ILogger logger) : base(device, logger)
        {
        }

        public static bool IsSupportedModel(string name)
        {
            return name == "SL-FXX-K Evo"
                || name == "SL-488 GTE"
                || name == "SL-SF1000"
                || name == "SL-488 Challenge Evo"
                || name == "SL-F1-75"
                || name == "SL-Daytona SP3"
                || name == "SL-296 GTB"
                || name == "SL-330 P4(1967)";
        }

        protected override byte[] PreparePayload(CarCommand command)
        {
            byte[] data = new byte[8];
            data[0] = 1;

            if (command.Forward)
                data[1] = 1;
            else if (command.Backward)
                data[2] = 1;

            if (command.Right)
                data[4] = 1;
            else if (command.Left)
                data[3] = 1;

            if (command.Lights)
                data[5] = 1;

            if (command.Turbo)
                data[6] = 1;

            return data;
        }

        protected override int GetBatteryLevel(byte[] data)
        {
            return data[0];
        }
    }
}
