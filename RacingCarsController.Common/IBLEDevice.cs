namespace RacingCarsController.Common
{
    public interface IBLEDevice
    {
        public event EventHandler<byte[]>? CharacteristicChanged;
        Task WriteCharacteristics(string serviceUUID, string characteristicsUUID, byte[] data, CancellationToken cancellationToken);
        Task SubscribeToNotifications(string serviceUUID, string characteristicsUUID, CancellationToken cancellationToken);
        ValueTask DisposeAsync();
    }
}
