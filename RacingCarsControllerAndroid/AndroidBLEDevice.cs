using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using RacingCarsController.Common;

namespace RacingCarsControllerAndroid
{
    internal class AndroidBLEDevice : IBLEDevice, IAsyncDisposable
    {
        private readonly Dictionary<string, ICharacteristic> _cachedCharacteristics;
        private readonly List<string> _notificationsCharacteristicUUIDs;

        protected IDevice Device;
        private readonly ILogger _logger;

        public event EventHandler<byte[]> CharacteristicChanged;

        public AndroidBLEDevice(IDevice device)
        {
            _cachedCharacteristics = new Dictionary<string, ICharacteristic>();
            _notificationsCharacteristicUUIDs = new List<string>();
            Device = device;
            _logger = new SystemDiagnosticsLogger();
        }


        public async Task SubscribeToNotifications(string serviceUUID, string characteristicsUUID)
        {
            try
            {
                var characteristic = await GetCharacteristicAsync(serviceUUID, characteristicsUUID);
                characteristic.ValueUpdated += Characteristic_ValueUpdated;
                _notificationsCharacteristicUUIDs.Add(characteristicsUUID);
                await characteristic.StartUpdatesAsync();
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString());
            }
        }

        public async Task WriteCharacteristics(string serviceUUID, string characteristicsUUID, byte[] data)
        {
            try
            {
                var characteristic = await GetCharacteristicAsync(serviceUUID, characteristicsUUID);

                var success = await characteristic.WriteAsync(data);
                if (success)
                {
                    _logger.Log("Message sent sucessfully");
                }
                else
                {
                    _logger.Log($"Could not write characteristics.");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString());
            }
        }

        private async Task<ICharacteristic> GetCharacteristicAsync(string serviceUUID, string characteristicsUUID)
        {
            if (_cachedCharacteristics.ContainsKey(characteristicsUUID)) return _cachedCharacteristics[characteristicsUUID];

            var service = await Device.GetServiceAsync(Guid.Parse(serviceUUID));

            if (service != null)
            {
                var characteristic = await service.GetCharacteristicAsync(Guid.Parse(characteristicsUUID));

                if (characteristic != null)
                {
                    _cachedCharacteristics.TryAdd(characteristicsUUID, characteristic);
                    return characteristic;
                }
                else
                {
                    _logger.Log($"Get Gatt characteristics error");
                    throw new Exception($"Get Gatt characteristics error");
                }
            }
            else
            {
                _logger.Log($"Get Gatt services error");
                throw new Exception($"Get Gatt services error");
            }
        }

        private void Characteristic_ValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
        {
            CharacteristicChanged?.Invoke(this, e.Characteristic.Value);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var notificationsCharact in _notificationsCharacteristicUUIDs)
            {
                if (_cachedCharacteristics.ContainsKey(notificationsCharact))
                {
                    var characteristic = _cachedCharacteristics[notificationsCharact];
                    await characteristic.StopUpdatesAsync();
                    characteristic.ValueUpdated -= Characteristic_ValueUpdated;
                }
            }
            Device.Dispose();
        }
    }
}
