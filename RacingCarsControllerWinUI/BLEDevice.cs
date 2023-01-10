using RacingCarsController.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace RacingCarsControllerWinUI
{

    public class BLEDevice : IBLEDevice, IAsyncDisposable
    {
        private readonly Dictionary<string, GattCharacteristic> _cachedCharacteristics;
        private readonly List<string> _notificationsCharacteristics;

        protected BluetoothLEDevice Device;
        public event EventHandler<byte[]> CharacteristicChanged;

        public BLEDevice(BluetoothLEDevice device)
        {
            _cachedCharacteristics = new Dictionary<string, GattCharacteristic>();
            _notificationsCharacteristics = new List<string>();
            Device = device;
        }

        public async Task WriteCharacteristics(string serviceUUID, string characteristicsUUID, byte[] data)
        {
            var writer = new DataWriter();
            writer.WriteBytes(data);

            try
            {
                var characteristic = await GetCharacteristicAsync(serviceUUID, characteristicsUUID);

                GattCommunicationStatus commStatus = await characteristic.WriteValueAsync(writer.DetachBuffer());
                if (commStatus == GattCommunicationStatus.Success)
                {
                    App.WriteDebug("Message sent sucessfully");
                }
                else
                {
                    App.WriteDebug($"Could not write characteristics. Status: {commStatus}");
                }
            }
            catch (Exception ex)
            {
                App.WriteDebug(ex.ToString());
            }
        }

        public async Task SubscribeToNotifications(string serviceUUID, string characteristicsUUID)
        {
            try
            {
                var characteristic = await GetCharacteristicAsync(serviceUUID, characteristicsUUID);

                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                {
                    _notificationsCharacteristics.Add(characteristicsUUID);
                    characteristic.ValueChanged += Characteristic_ValueChanged;
                }
            }
            catch (Exception ex)
            {
                App.WriteDebug(ex.ToString());
            }
        }

        private async Task<GattCharacteristic> GetCharacteristicAsync(string serviceUUID, string characteristicsUUID)
        {
            if (_cachedCharacteristics.ContainsKey(characteristicsUUID)) return _cachedCharacteristics[characteristicsUUID];

            GattDeviceServicesResult gattServices = await Device.GetGattServicesAsync();

            if (gattServices.Status == GattCommunicationStatus.Success)
            {
                var services = gattServices.Services;
                var service = services.FirstOrDefault(x => x.Uuid.ToString() == serviceUUID);
                GattCharacteristicsResult gattCharacteristics = await service.GetCharacteristicsAsync();

                if (gattCharacteristics.Status == GattCommunicationStatus.Success)
                {
                    var characteristics = gattCharacteristics.Characteristics;
                    var characteristic = characteristics.FirstOrDefault(x => x.Uuid.ToString() == characteristicsUUID);
                    _cachedCharacteristics.TryAdd(characteristicsUUID, characteristic);
                    return characteristic;
                }
                else
                {
                    App.WriteDebug($"Get Gatt characteristics error {gattCharacteristics.Status}");
                    throw new Exception($"Get Gatt characteristics error {gattCharacteristics.Status}");
                }
            }
            else
            {
                App.WriteDebug($"Get Gatt services error {gattServices.Status}");
                throw new Exception($"Get Gatt services error {gattServices.Status}");
            }
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // An Indicate or Notify reported that the value has changed.
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);
            CharacteristicChanged?.Invoke(this, data);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var notificationsCharact in _notificationsCharacteristics)
            {
                if (_cachedCharacteristics.ContainsKey(notificationsCharact))
                {
                    var characteristic = _cachedCharacteristics[notificationsCharact];
                    var result = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                    if (result == GattCommunicationStatus.Success)
                    {
                        characteristic.ValueChanged -= Characteristic_ValueChanged;
                    }
                }
            }
            Device.Dispose();
        }
    }
}
