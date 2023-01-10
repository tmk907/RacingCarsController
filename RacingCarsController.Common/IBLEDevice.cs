namespace RacingCarsController.Common
{
    public interface IBLEDevice
    {
        public event EventHandler<byte[]>? CharacteristicChanged;
        Task WriteCharacteristics(string serviceUUID, string characteristicsUUID, byte[] data);
        Task SubscribeToNotifications(string serviceUUID, string characteristicsUUID);
        ValueTask DisposeAsync();
    }
}
