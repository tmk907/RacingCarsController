using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

namespace RacingCarsControllerWinUI
{
    public class UnknownCar : RemoteCar
    {
        public UnknownCar(BluetoothLEDevice device) : base(device)
        {
        }

        public override string ControlServiceUUID => throw new NotImplementedException();

        public override string ControlCharacteristicUUID => throw new NotImplementedException();

        public override string BatteryServiceUUID => throw new NotImplementedException();

        public override string BatteryCharacteristicUUID => throw new NotImplementedException();

        public override Task SendCommandAsync(CarCommand command)
        {
            return Task.CompletedTask;
        }

        public override Task SubscribeToBatteryNotifications()
        {
            return Task.CompletedTask;
        }

        protected override byte[] PreparePayload(CarCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
