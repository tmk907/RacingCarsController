using Android.Util;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using RacingCarsController.Common;
using Xamarin.Essentials;

namespace RacingCarsControllerAndroid
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private PermissionsHelpers _permissionsHelpers;
        private Plugin.BLE.Abstractions.Contracts.IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
        private IRacingCar? _selectedCar;
        private System.Timers.Timer _buttonStateTimer;
        private MessageBroadcaster _messageBroadcaster;

        private DeviceItemAdapter _deviceItemAdapter;

        private LinearLayout _container;
        private Button scanButton;
        private LinearProgressIndicator scanProgressIndicator;

        private Button disconnectButton;
        private LinearLayout selectedCar_Layout;
        private TextView selectedCarTextView;
        private TextView selectedCarBatteryTextView;

        bool isLightsOn = false;
        bool isTurboOn = false;

        private Button lightsButton;
        private Button turboButton;
        private Button buttonForward;
        private Button buttonBackward;
        private Button buttonLeft;
        private Button buttonRight;


        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            _permissionsHelpers = new PermissionsHelpers(this);
            _adapter.ScanMode = Plugin.BLE.Abstractions.Contracts.ScanMode.Balanced;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _buttonStateTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(50));
            _buttonStateTimer.Elapsed += OnButtonStateTimerElapsed;
            _messageBroadcaster = new MessageBroadcaster();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);


            var mRecyclerView = FindViewById<RecyclerView>(Resource.Id.devicesList);

            var mLayoutManager = new LinearLayoutManager(this);
            mRecyclerView.SetLayoutManager(mLayoutManager);

            _deviceItemAdapter = new DeviceItemAdapter();
            mRecyclerView.SetAdapter(_deviceItemAdapter);

            _deviceItemAdapter.ItemClick += OnItemClick;
            
            _container = FindViewById<LinearLayout>(Resource.Id.container);

            scanButton = FindViewById<Button>(Resource.Id.buttonScan);
            scanButton.Click += ScanButton_Click;
            scanProgressIndicator = FindViewById<LinearProgressIndicator>(Resource.Id.scanProgressIndicator);

            disconnectButton = FindViewById<Button>(Resource.Id.disconnectButton);
            disconnectButton.Click += DisconnectButton_Click;

            selectedCar_Layout = FindViewById<LinearLayout>(Resource.Id.selectedCar_Layout);
            selectedCar_Layout.Visibility = Android.Views.ViewStates.Invisible;

            lightsButton = FindViewById<Button>(Resource.Id.buttonLights);
            lightsButton.Click += OnLightsButtonClicked;
            turboButton = FindViewById<Button>(Resource.Id.buttonTurbo);
            turboButton.Click += OnTurboButtonClicked;

            buttonForward = FindViewById<Button>(Resource.Id.buttonForward);
            buttonBackward = FindViewById<Button>(Resource.Id.buttonBackward);
            buttonLeft = FindViewById<Button>(Resource.Id.buttonLeft);
            buttonRight = FindViewById<Button>(Resource.Id.buttonRight);

            buttonBackward.Click += OnControlButtonClicked;
            buttonForward.Click += OnControlButtonClicked;
            buttonLeft.Click += OnControlButtonClicked;
            buttonRight.Click += OnControlButtonClicked;

            selectedCarTextView = FindViewById<TextView>(Resource.Id.selectedCarName_Text);
            selectedCarBatteryTextView = FindViewById<TextView>(Resource.Id.selectedCarBattery_Text);
        }

        private void OnControlButtonClicked(object? sender, EventArgs e)
        {
            SendCommand();
        }

        private void OnTurboButtonClicked(object? sender, EventArgs e)
        {
            isTurboOn = !isTurboOn;
            turboButton.Selected = isTurboOn;
            SendCommand();
        }

        private void OnLightsButtonClicked(object? sender, EventArgs e)
        {
            isLightsOn = !isLightsOn;
            lightsButton.Selected = isLightsOn;
            SendCommand();
        }

        private void SendCommand()
        {
            var command = new CarCommand(
                    buttonForward.Pressed,
                    buttonBackward.Pressed,
                    buttonLeft.Pressed,
                    buttonRight.Pressed,
                    isLightsOn,
                    isTurboOn);

            _messageBroadcaster.QueueCommand(command);
        }

        private async void ScanButton_Click(object? sender, EventArgs e)
        {
            await ToggleScanAsync();
        }

        private async void DisconnectButton_Click(object? sender, EventArgs e)
        {
            await DisconnectCarAsync();
        }

        private void OnButtonStateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            SendCommand();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _buttonStateTimer.Stop();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _buttonStateTimer.Start();
        }

        private void OnDeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Device.Name))
            {
                MainThread.BeginInvokeOnMainThread(() => _deviceItemAdapter.Add(e.Device));
            }
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
            scanButton.Enabled = false;

            var scanFilterOptions = new ScanFilterOptions();
            try
            {
                var state = await _permissionsHelpers.CheckAndRequestBluetoothPermissions();
                if (state != PermissionStatus.Granted) return;
                if (!CrossBluetoothLE.Current.IsOn)
                {
                    DisplayError("Bluetooth is turned off.");
                }
                else
                {
                    _deviceItemAdapter.Clear();
                    await _adapter.StartScanningForDevicesAsync();
                    if (_adapter.IsScanning)
                    {
                        scanProgressIndicator.Visibility = Android.Views.ViewStates.Visible;
                        scanButton.Text = "Stop scan";
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.ToString());
                DisplayError(ex.Message);
            }
            finally
            {
                scanButton.Enabled = true;
            }
        }

        public async Task StopScanAsync()
        {
            scanButton.Enabled = false;
            try
            {
                await _adapter.StopScanningForDevicesAsync();
                scanProgressIndicator.Visibility = Android.Views.ViewStates.Invisible;
                scanButton.Text = "Start scan";
            }
            catch(Exception ex)
            {
                LogMessage(ex.ToString());
                DisplayError(ex.Message);
            }
            finally
            {
                scanButton.Enabled = true;
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
                _messageBroadcaster.SetCar(_selectedCar);
                selectedCar_Layout.Visibility = Android.Views.ViewStates.Visible;
                selectedCarTextView.Text = device.Name;
            }
        }

        public async Task<IRacingCar> ConnectDeviceAsync(IDevice device)
        {
            LogMessage($"ConnectDeviceAsync {device.Name}");
            IRacingCar car = new UnknownCar();
            try
            {
                if (FerrariCar.IsSupportedModel(device.Name))
                {
                    await _adapter.ConnectToDeviceAsync(device);
                    car = new FerrariCar(new AndroidBLEDevice(device), new SystemDiagnosticsLogger());
                }
                else if (BrandbaseCar.IsSupportedModel(device.Name))
                {
                    await _adapter.ConnectToDeviceAsync(device);
                    car = new BrandbaseCar(new AndroidBLEDevice(device), new SystemDiagnosticsLogger());
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.ToString());
                DisplayError(ex.Message);
            }
            await car.SubscribeToBatteryNotifications();
            return car;
        }

        private async Task DisconnectCarAsync()
        {
            if (_selectedCar != null)
            {
                _messageBroadcaster.CancelBroadcasting();
                LogMessage("DisconnectCarAsync");
                _selectedCar.BatteryLevelChanged -= OnBatteryLevelChanged;
                await _selectedCar.DisposeAsync();
                _selectedCar = null;

                selectedCar_Layout.Visibility = Android.Views.ViewStates.Invisible;
                selectedCarTextView.Text = "";
                selectedCarBatteryTextView.Text = "";
            }
        }

        private void OnBatteryLevelChanged(object? sender, int level)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowBatteryLevel(level);
            });
        }

        private void ShowBatteryLevel(int level)
        {
            selectedCarBatteryTextView.Text = $"{level}%";
            int icon = Resource.Drawable.ic_baseline_battery_full_alt_48;
            if (level < 90 && level > 60)
            {
                icon = Resource.Drawable.ic_baseline_battery_horiz_075_48;
            }
            else if (level <=60 && level > 35)
            {
                icon = Resource.Drawable.ic_baseline_battery_horiz_050_48;
            }
            else
            {
                icon = Resource.Drawable.ic_baseline_battery_low_48;
            }
            //selectedCarBattery_Icon.Glyph = BatteryLevelToIcon(level);
        }

        public static void LogMessage(string text)
        {
            Log.Info("APP LOG", text);
            System.Diagnostics.Debug.WriteLine($"APP LOG: {text}");
        }

        private void DisplayError(string text)
        {
            Snackbar.Make(_container, $"Error: {text}", Snackbar.LengthLong).Show();
        }
    }
}