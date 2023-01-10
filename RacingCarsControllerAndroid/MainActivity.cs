using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using AndroidX.RecyclerView.Widget;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using RacingCarsController.Common;
using Xamarin.Essentials;

namespace RacingCarsControllerAndroid
{
    [Activity(Label = "@string/app_name", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class MainActivity : Activity
    {
        private PermissionsHelpers _permissionsHelpers;
        private Plugin.BLE.Abstractions.Contracts.IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
        private IRacingCar _selectedCar;
        private System.Timers.Timer _timer;

        private DeviceItemAdapter _deviceItemAdapter;

        private TextView selectedCarTextView;
        private TextView selectedCarBatteryTextView;
        private Button scanButton;
        private Button disconnectButton;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            _permissionsHelpers = new PermissionsHelpers(this);
            _adapter.ScanMode = Plugin.BLE.Abstractions.Contracts.ScanMode.Balanced;
            _adapter.DeviceDiscovered += _adapter_DeviceDiscovered;
            _timer = new System.Timers.Timer(TimeSpan.FromMilliseconds(200));
            _timer.Elapsed += _timer_Elapsed;

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);


            var mRecyclerView = FindViewById<RecyclerView>(Resource.Id.devicesList);

            var mLayoutManager = new LinearLayoutManager(this);
            mRecyclerView.SetLayoutManager(mLayoutManager);

            _deviceItemAdapter = new DeviceItemAdapter();
            mRecyclerView.SetAdapter(_deviceItemAdapter);

            _deviceItemAdapter.ItemClick += OnItemClick;

            scanButton = FindViewById<Button>(Resource.Id.buttonScan);
            scanButton.Click += ScanButton_Click;
            disconnectButton = FindViewById<Button>(Resource.Id.disconnectButton);
            disconnectButton.Click += DisconnectButton_Click;

            selectedCarTextView = FindViewById<TextView>(Resource.Id.selectedCarName_Text);
            selectedCarBatteryTextView = FindViewById<TextView>(Resource.Id.selectedCarBattery_Text);

            //_timer.Start();
        }

        private async void ScanButton_Click(object? sender, EventArgs e)
        {
            await ToggleScanAsync();
        }

        private async void DisconnectButton_Click(object? sender, EventArgs e)
        {
            await DisconnectCarAsync();
        }

        private async void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_selectedCar != null)
            {
                //var command = new CarCommand(
                //    buttonForward.IsPressed || IsKeyPressed(new[] { VirtualKey.Up, VirtualKey.W }),
                //    buttonBackward.IsPressed || IsKeyPressed(new[] { VirtualKey.Down, VirtualKey.S }),
                //    buttonLeft.IsPressed || IsKeyPressed(new[] { VirtualKey.Left, VirtualKey.A }),
                //    buttonRight.IsPressed || IsKeyPressed(new[] { VirtualKey.Right, VirtualKey.D }),
                //    buttonLights.IsChecked ?? false,
                //    buttonTurbo.IsChecked ?? false);

                //await MainThread.InvokeOnMainThreadAsync(_selectedCar.SendCommandAsync(command));
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            _timer.Stop();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _timer.Start();
        }

        private void _adapter_DeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => _deviceItemAdapter.Add(e.Device));
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public async Task ToggleScanAsync()
        {
            if (_adapter.IsScanning)
            {
                await StopScanAsync();
            }
            else
            {
                await StartScanAsync();
            }
        }

        public async Task StartScanAsync()
        {
            var scanFilterOptions = new ScanFilterOptions();
            try
            {
                var state = await _permissionsHelpers.CheckAndRequestBluetoothPermissions();
                if (state != PermissionStatus.Granted) return;
                scanButton.Text = "Stop scan";
                await _adapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                LogMessage(ex.ToString());
            }
        }

        public async Task StopScanAsync()
        {
            try
            {
                scanButton.Text = "Start scan";
                await _adapter.StopScanningForDevicesAsync();
            }
            catch(Exception ex)
            {
                LogMessage(ex.ToString());
            }
        }

        private async void OnItemClick(object? sender, int position)
        {
            var device = _deviceItemAdapter.Get(position);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await StopScanAsync();
                await OnDeviceSelected(device);
            });
        }

        public async Task OnDeviceSelected(IDevice device)
        {
            LogMessage("Device selected");
            if (_selectedCar != null)
            {
                await DisconnectCarAsync();
            }

            if (device != null)
            {
                _selectedCar = await ConnectDeviceAsync(device);
                _selectedCar.BatteryLevelChanged += OnBatteryLevelChanged;
                selectedCarTextView.Text = device.Name;
            }
        }

        public async Task<IRacingCar> ConnectDeviceAsync(IDevice device)
        {
            LogMessage($"ConnectDeviceAsync {device.Name}");
            IRacingCar car = new UnknownCar();
            try
            {
                await _adapter.ConnectToDeviceAsync(device);
                if (FerrariCar.IsSupportedModel(device.Name))
                {
                    car = new FerrariCar(new AndroidBLEDevice(device), new SystemDiagnosticsLogger());
                }
                else if (BrandbaseCar.IsSupportedModel(device.Name))
                {
                    car = new BrandbaseCar(new AndroidBLEDevice(device), new SystemDiagnosticsLogger());
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.ToString());
            }
            await car.SubscribeToBatteryNotifications();
            return car;
        }

        private async Task DisconnectCarAsync()
        {
            if (_selectedCar != null)
            {
                LogMessage("DisconnectCarAsync");
                _selectedCar.BatteryLevelChanged -= OnBatteryLevelChanged;
                await _selectedCar.DisposeAsync();
                _selectedCar = null;
                //devicesList.SelectedItem = null;

                selectedCarTextView.Text = "";
                selectedCarBatteryTextView.Text = "";
                //battery_Panel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnBatteryLevelChanged(object sender, int level)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowBatteryLevel(level);
            });
        }

        private void ShowBatteryLevel(int level)
        {
            selectedCarBatteryTextView.Text = $"{level}%";
            //selectedCarBattery_Icon.Glyph = BatteryLevelToIcon(level);
            //battery_Panel.Visibility = Visibility.Visible;
        }

        public static void LogMessage(string text)
        {
            Log.Info("APP LOG", text);
            System.Diagnostics.Debug.WriteLine($"APP LOG: {text}");
        }
    }
}