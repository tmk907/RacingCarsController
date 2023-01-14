namespace RacingCarsController.Common
{
    public class UnknownCar : RacingCar
    {
        public UnknownCar() : base(new UnknownBLEDevice(), new SystemDiagnosticsLogger())
        {
        }

        public override string ControlServiceUUID => throw new NotImplementedException();

        public override string ControlCharacteristicUUID => throw new NotImplementedException();

        public override string BatteryServiceUUID => throw new NotImplementedException();

        public override string BatteryCharacteristicUUID => throw new NotImplementedException();

        public override Task SendCommandAsync(CarCommand command, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task SubscribeToBatteryNotifications()
        {
            return Task.CompletedTask;
        }

        protected override int GetBatteryLevel(byte[] data)
        {
            return 0;
        }

        protected override byte[] PreparePayload(CarCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
