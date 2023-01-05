using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace RacingCarsControllerWinUI
{
    public interface IRemoteCar : IAsyncDisposable
    {
        event EventHandler<int> BatteryLevelChanged;

        Task SendCommandAsync(CarCommand command);
        Task SubscribeToBatteryNotifications();
    }

    public abstract class RemoteCar : IRemoteCar
    {
        public abstract string ControlServiceUUID { get; }   
        public abstract string ControlCharacteristicUUID { get; }

        public abstract string BatteryServiceUUID { get; }
        public abstract string BatteryCharacteristicUUID { get; }

        public event EventHandler<int> BatteryLevelChanged;

        protected BluetoothLEDevice Device;

        protected bool IgnoreSubsequentSameCommands;
        private CarCommand _previousCommand;
        private readonly Dictionary<string, GattCharacteristic> _cachedCharacteristics;

        public RemoteCar(BluetoothLEDevice device)
        {
            Device = device;
            _cachedCharacteristics = new Dictionary<string, GattCharacteristic>();
            IgnoreSubsequentSameCommands = true;
        }

        private Task<GattCharacteristic> GetControlCharacteristicsAsync()
        {
            return GetCharacteristicAsync(ControlServiceUUID, ControlCharacteristicUUID);
        }

        private Task<GattCharacteristic> GetBatteryCharacteristicsAsync()
        {
            return GetCharacteristicAsync(BatteryServiceUUID, BatteryCharacteristicUUID);
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

        public virtual async Task SendCommandAsync(CarCommand command)
        {
            if (IgnoreSubsequentSameCommands && command == _previousCommand)
            {
                App.WriteDebug($"Don't send command {command}");
                return;
            };
            _previousCommand = command;
            App.WriteDebug($"Send command {command}");

            var data = PreparePayload(command);
            var writer = new DataWriter();
            writer.WriteBytes(data);

            try
            {
                var characteristic = await GetControlCharacteristicsAsync();

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

        protected abstract byte[] PreparePayload(CarCommand command);

        public virtual async Task SubscribeToBatteryNotifications()
        {
            try
            {
                var characteristic = await GetBatteryCharacteristicsAsync();

                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                {
                    characteristic.ValueChanged += BatteryCharacteristic_ValueChanged;
                }
            }
            catch (Exception ex)
            {
                App.WriteDebug(ex.ToString());
            }
        }

        private void BatteryCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // An Indicate or Notify reported that the value has changed.
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            var level = GetBatteryLevel(reader);
            App.WriteDebug($"Battery level is {level}");
            OnBatteryLevelChanged(level);
        }

        protected virtual int GetBatteryLevel(DataReader reader)
        {
            return reader.ReadByte();
        }

        protected virtual void OnBatteryLevelChanged(int level)
        {
            BatteryLevelChanged?.Invoke(this, level);
        }

        public async ValueTask DisposeAsync()
        {
            if (_cachedCharacteristics.ContainsKey(BatteryCharacteristicUUID))
            {
                var characteristic = _cachedCharacteristics[BatteryCharacteristicUUID];
                var result = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (result == GattCommunicationStatus.Success)
                {
                    characteristic.ValueChanged -= BatteryCharacteristic_ValueChanged;
                }
            }
            Device.Dispose();
        }
    }
}
