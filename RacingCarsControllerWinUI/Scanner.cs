using System.Collections.ObjectModel;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace RacingCarsControllerWinUI
{
    public class Scanner
    {
        private readonly DeviceWatcher deviceWatcher;

        public ObservableCollection<DeviceInformation> DeviceInformations { get; }

        public Scanner()
        {
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher =
                        DeviceInformation.CreateWatcher(
                                BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                                requestedProperties,
                                DeviceInformationKind.AssociationEndpoint);

            // Register event handlers before starting the watcher.
            // Added, Updated and Removed are required to get all nearby devices
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            // EnumerationCompleted and Stopped are optional to implement.
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            DeviceInformations = new ObservableCollection<DeviceInformation>();
        }

        public void StartScan()
        {
            if (deviceWatcher.Status == DeviceWatcherStatus.Stopped || 
                deviceWatcher.Status == DeviceWatcherStatus.Aborted)
            {
                deviceWatcher.Start();
            }
        }

        public void StopScan()
        {
            if (deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted ||
                deviceWatcher.Status == DeviceWatcherStatus.Started)
            {
                deviceWatcher.Stop();
            }
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var toRemove = DeviceInformations.FirstOrDefault(x => x.Id == args.Id);
            if (toRemove != null)
            {
                DeviceInformations.Remove(toRemove);
            }
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var toUpdate = DeviceInformations.FirstOrDefault(x => x.Id == args.Id);
            if (toUpdate != null)
            {
            }
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            System.Diagnostics.Debug.WriteLine($"Added {args.Name} {args.Id}");
            DeviceInformations.Add(args);
        }
    }
}
