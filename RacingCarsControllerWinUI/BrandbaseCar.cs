namespace RacingCarsControllerWinUI
{
    internal class BrandbaseCar : RemoteCar
    {
        private readonly byte[] _aesKey = new byte[] { 0x34, 0x52, 0x2A, 0x5B, 0x7A, 0x6E, 0x49, 0x2C, 0x08, 0x09, 0x0A, 0x9D, 0x8D, 0x2A, 0x23, 0xF8 };

        public BrandbaseCar(IBLEDevice device) : base(device)
        {
            IgnoreSubsequentSameCommands = false;
        }

        public static bool IsSupportedModel(string name)
        {
            return name.StartsWith("QCAR");
        }

        public override string ControlServiceUUID => "0000fff0-0000-1000-8000-00805f9b34fb";

        public override string ControlCharacteristicUUID => "d44bc439-abfd-45a2-b575-925416129600";

        public override string BatteryServiceUUID => "0000fff0-0000-1000-8000-00805f9b34fb";

        public override string BatteryCharacteristicUUID => "d44bc439-abfd-45a2-b575-925416129601";

        protected override byte[] PreparePayload(CarCommand command)
        {
            byte[] data = new byte[16];
            data[0] = 0;
            data[1] = 0x43;
            data[2] = 0x54;
            data[3] = 0x4c;

            if (command.Forward)
                data[4] = 1;
            else if (command.Backward)
                data[5] = 1;

            if (command.Left)
                data[6] = 1;
            else if (command.Right)
                data[7] = 1;

            if (command.Lights)
                data[8] = 0;
            else
                data[8] = 1;

            if (command.Turbo)
                data[9] = 0x64;
            else
                data[9] = 0x50;

            data = AESHelper.Encrypt(_aesKey, data);
            return data;
        }

        protected override int GetBatteryLevel(byte[] data)
        {
            byte[] decoded = AESHelper.Decrypt(_aesKey, data);
            if (decoded.Length == 16)
            {
                return decoded[4];
            }
            return 0;
        }
    }
}
