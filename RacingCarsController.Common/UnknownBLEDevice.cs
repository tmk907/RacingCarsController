namespace RacingCarsController.Common
{
    public class UnknownBLEDevice : IBLEDevice
    {
        public event EventHandler<byte[]>? CharacteristicChanged;

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task SubscribeToNotifications(string serviceUUID, string characteristicsUUID)
        {
            return Task.CompletedTask;
        }

        public Task WriteCharacteristics(string serviceUUID, string characteristicsUUID, byte[] data)
        {
            return Task.CompletedTask;
        }
    }
}
