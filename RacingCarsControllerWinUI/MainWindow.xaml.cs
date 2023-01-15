using CommunityToolkit.WinUI.Connectivity;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using RacingCarsController.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.System;

namespace RacingCarsControllerWinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly BluetoothLEHelper _bluetoothLEHelper = BluetoothLEHelper.Context;
        private readonly DispatcherTimer _dispatcherTimer;
        private IRacingCar _selectedCar;

        public MainWindow()
        {
            var manager = WinUIEx.WindowManager.Get(this);
            manager.Width = 640;
            manager.Height = 400;
            Title = "Racing Cars Controller";

            this.InitializeComponent();
            _bluetoothLEHelper.EnumerationCompleted += BluetoothLEHelper_EnumerationCompleted;

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += DispatcherTimer_Tick; ;
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);

            _dispatcherTimer.Start();
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            if (_selectedCar != null)
            {
                var command = new CarCommand(
                    buttonForward.IsPressed || IsKeyPressed(new[] { VirtualKey.Up, VirtualKey.W }),
                    buttonBackward.IsPressed || IsKeyPressed(new[] { VirtualKey.Down, VirtualKey.S }),
                    buttonLeft.IsPressed || IsKeyPressed(new[] { VirtualKey.Left, VirtualKey.A }),
                    buttonRight.IsPressed || IsKeyPressed(new[] { VirtualKey.Right, VirtualKey.D }),
                    buttonLights.IsChecked ?? false,
                    buttonTurbo.IsChecked ?? false);

                await _selectedCar.SendCommandAsync(command, CancellationToken.None);
            }
        }

        private bool IsKeyPressed(IEnumerable<VirtualKey> keys)
        {
            return keys.Any(key => IsKeyPressed(key));
        }

        private bool IsKeyPressed(VirtualKey key)
        {
            var state = InputKeyboardSource.GetKeyStateForCurrentThread(key);
            return state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        }

        private void BluetoothLEHelper_EnumerationCompleted(object sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                StopScan();
            });
        }

        public async Task<IRacingCar> ConnectDeviceAsync(DeviceInformation deviceInfo)
        {
            App.WriteDebug($"ConnectDeviceAsync {deviceInfo.Name}");
            IRacingCar car = new UnknownCar();
            try
            {
                // Note: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
                if (FerrariCar.IsSupportedModel(deviceInfo.Name))
                {
                    car = new FerrariCar(new WindowsBLEDevice(bluetoothLeDevice), new SystemDiagnosticsLogger());
                }
                else if (BrandbaseCar.IsSupportedModel(deviceInfo.Name))
                {
                    car = new BrandbaseCar(new WindowsBLEDevice(bluetoothLeDevice), new SystemDiagnosticsLogger());
                }
            }
            catch (Exception ex)
            {
                App.WriteDebug(ex.ToString());
            }
            await car.SubscribeToBatteryNotifications();
            return car;
        }

        private void scanDevices_Click(object sender, RoutedEventArgs e)
        {
            if (_bluetoothLEHelper.IsEnumerating)
            {
                StopScan();
            }
            else
            {
                StartScan();
            }
        }

        private void StopScan()
        {
            _bluetoothLEHelper.StopEnumeration();
            scanDevicesButton.Content = "Start scan";
            scan_Progress.Visibility = Visibility.Collapsed;
        }

        private void StartScan()
        {
            _bluetoothLEHelper.StartEnumeration();
            scanDevicesButton.Content = "Stop scan";
            scan_Progress.Visibility = Visibility.Visible;
        }

        private async void deviceSelected(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            App.WriteDebug("Device selected");
            if (_selectedCar != null)
            {
                await DisconnectCarAsync();
            }

            var device = e.AddedItems.FirstOrDefault() as ObservableBluetoothLEDevice;
            if (device != null)
            {
                _selectedCar = await ConnectDeviceAsync(device.DeviceInfo);
                _selectedCar.BatteryLevelChanged += OnBatteryLevelChanged;
                selectedCarName_Text.Text = device.Name;
            }
        }

        private async void disconnect_Click(object sender, RoutedEventArgs e)
        {
            await DisconnectCarAsync();
        }

        private async Task DisconnectCarAsync()
        {
            if (_selectedCar != null)
            {
                App.WriteDebug("DisconnectCarAsync");
                _selectedCar.BatteryLevelChanged -= OnBatteryLevelChanged;
                await _selectedCar.DisposeAsync();
                _selectedCar = null;
                devicesList.SelectedItem = null;

                selectedCarName_Text.Text = "";
                battery_Panel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnBatteryLevelChanged(object sender, int level)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ShowBatteryLevel(level);
            });
        }

        private void ShowBatteryLevel(int level)
        {
            selectedCarBattery_Text.Text = $"{level}%";
            selectedCarBattery_Icon.Glyph = BatteryLevelToIcon(level);
            battery_Panel.Visibility = Visibility.Visible;
        }

        private string BatteryLevelToIcon(int level)
        {
            if (level >= 95)
            {
                return "\uE83F";
            }
            else if (level >= 90)
            {
                return "\uE859";
            }
            else if (level >= 80)
            {
                return "\uE858";
            }
            else if (level >= 70)
            {
                return "\uE857";
            }
            else if (level >= 60)
            {
                return "\uE856";
            }
            else if (level >= 50)
            {
                return "\uE855";
            }
            else if (level >= 40)
            {
                return "\uE854";
            }
            else if (level >= 30)
            {
                return "\uE853";
            }
            else if (level >= 20)
            {
                return "\uE852";
            }
            else if (level >= 10)
            {
                return "\uE851";
            }
            else
            {
                return "\uE850";
            }
        }
    }
}
