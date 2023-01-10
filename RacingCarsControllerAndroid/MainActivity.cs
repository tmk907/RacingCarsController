using Android.Util;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Xamarin.Essentials;

namespace RacingCarsControllerAndroid
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private PermissionsHelpers _permissionsHelpers;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            _permissionsHelpers = new PermissionsHelpers(this);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var button = FindViewById<Button>(Resource.Id.textButton);
            button.Click += Button_Click;
        }

        private async void Button_Click(object? sender, EventArgs e)
        {
            var state = await _permissionsHelpers.CheckAndRequestBluetoothPermissions();
            await TestScan();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async Task TestScan()
        {
            var ble = CrossBluetoothLE.Current;
            var adapter = CrossBluetoothLE.Current.Adapter;

            adapter.DeviceDiscovered += async (s, a) =>
            {
                LogMessage($"New device found {a.Device.Id} {a.Device?.Name}");

                if (a.Device.Name != null && a.Device.Name.StartsWith("SL-SF"))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () => await test(a.Device));
                }
            };
            var scanFilterOptions = new ScanFilterOptions();
            adapter.ScanMode = Plugin.BLE.Abstractions.Contracts.ScanMode.Balanced;
            try
            {
                await adapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {

            }

        }

        private async Task test(IDevice device)
        {
            try
            {
                await CrossBluetoothLE.Current.Adapter.ConnectToDeviceAsync(device);
                var service = await device.GetServiceAsync(Guid.Parse("0000fff0-0000-1000-8000-00805f9b34fb"));
                var characteristic = await service.GetCharacteristicAsync(Guid.Parse("0000fff1-0000-1000-8000-00805f9b34fb"));
                var payload = PreparePayload();
                var success = await characteristic.WriteAsync(payload);

            }
            catch (DeviceConnectionException e)
            {
                // ... could not connect to device
            }
        }

        protected byte[] PreparePayload()
        {
            byte[] data = new byte[8];
            data[0] = 1;

                data[1] = 0;
                data[2] = 0;

                data[4] = 1;
                data[3] = 0;

                data[5] = 0;

                data[6] = 0;

            return data;
        }

        public static void LogMessage(string text)
        {
            Log.Info("APP LOG", text);
            System.Diagnostics.Debug.WriteLine($"APP LOG: {text}");
        }
    }
}