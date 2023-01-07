using Android;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.Core.App;
using System;
using System.Linq;
using Xamarin.Essentials;
using static Android.Icu.Text.CaseMap;

namespace RacingCarsControllerAndroid
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private const int ENABLE_BLUETOOTH_REQUEST_CODE = 1;
        private const int RUNTIME_PERMISSION_REQUEST_CODE = 2;

        private BluetoothAdapter bluetoothAdapter;
        private BluetoothLeScanner bleScanner => bluetoothAdapter.BluetoothLeScanner;

        public bool IsScanning { get; set; } = false;


        protected override void OnCreate(Bundle? savedInstanceState)
        {

            var bluetoothManager = GetSystemService(Context.BluetoothService) as BluetoothManager;
            bluetoothAdapter = bluetoothManager?.Adapter;


            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var scanButton = FindViewById<Button>(Resource.Id.scan_button);
            scanButton.Click += ScanButton_Click;
        }

        private void ScanButton_Click(object? sender, EventArgs e)
        {
            if ()
            {
                StopBleScan();
            }
            else
            {
                StartBleScan();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!bluetoothAdapter.IsEnabled)
            {
                PromptEnableBluetooth();
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            switch(requestCode) {
                case ENABLE_BLUETOOTH_REQUEST_CODE:
                    if (resultCode != Result.Ok)
                    {
                        PromptEnableBluetooth();
                    }
                    break;
                default:
                    break;
            }
        }

        private void PromptEnableBluetooth()
        {
            if (!bluetoothAdapter.IsEnabled)
            {
                var enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, ENABLE_BLUETOOTH_REQUEST_CODE);
            }
        }

        private bool hasPermission(string permissionType)
        {
            return ApplicationContext.CheckSelfPermission(permissionType) == Android.Content.PM.Permission.Granted;
        }

        private bool hasRequiredRuntimePermissions()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) {
                return hasPermission(Manifest.Permission.BluetoothScan) 
                    && hasPermission(Manifest.Permission.BluetoothConnect);
            } else {
                return hasPermission(Manifest.Permission.AccessFineLocation);
            }
        }

        private void StartBleScan()
        {
            if (!hasRequiredRuntimePermissions())
            {
                RequestRelevantRuntimePermissions();
            }
            else 
            {
                


                IsScanning = true;
            }
        }

        private void StopBleScan()
        {
            bleScanner.StopScan();
            IsScanning = false;
        }


        private void RequestRelevantRuntimePermissions()
        {
            if (hasRequiredRuntimePermissions()) return;
            if(Build.VERSION.SdkInt < BuildVersionCodes.S)
            {
                RequestLocationPermission();
            }
            else
            {
                RequestBluetoothPermissions();
            }
        }

        private void RequestLocationPermission()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Location permission required");
                builder.SetMessage("Starting from Android M (6.0), the system requires apps to be granted " +
                "location access in order to scan for BLE devices.");
                builder.SetCancelable(false);
                builder.SetPositiveButton(Resource.String.ok, OnRequestLocationPermissionAccept);
                builder.Show();
            });
        }

        private void RequestBluetoothPermissions()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Bluetooth permissions required");
                builder.SetMessage("Starting from Android 12, the system requires apps to be granted " +
                "Bluetooth access in order to scan for and connect to BLE devices.");
                builder.SetCancelable(false);
                builder.SetPositiveButton(Resource.String.ok, OnRequestBluetoothPermissionsAccept);
                builder.Show();
            });
        }

        private void OnRequestLocationPermissionAccept(object sender, DialogClickEventArgs args)
        {
            ActivityCompat.RequestPermissions(
                            this,
                            new[] { Manifest.Permission.AccessFineLocation },
                            RUNTIME_PERMISSION_REQUEST_CODE
                        );
        }

        private void OnRequestBluetoothPermissionsAccept(object sender, DialogClickEventArgs args)
        {
            ActivityCompat.RequestPermissions(
                            this,
                            new[] { Manifest.Permission.BluetoothScan, Manifest.Permission.BluetoothConnect },
                            RUNTIME_PERMISSION_REQUEST_CODE
                        );
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RUNTIME_PERMISSION_REQUEST_CODE)
            {
                var containsPermanentDenial = permissions
                    .Zip(grantResults)
                    .Any(x => x.Second == Permission.Denied &&
                            ActivityCompat.ShouldShowRequestPermissionRationale(this, x.First));
                var containsDenial = grantResults.Any(x => x == Permission.Denied);
                var allGranted = grantResults.All(x => x == Permission.Granted);
                if (containsPermanentDenial)
                {
                    // TODO: Handle permanent denial (e.g., show AlertDialog with justification)
                    // Note: The user will need to navigate to App Settings and manually grant
                    // permissions that were permanently denied
                }
                else if (containsDenial)
                {
                    RequestRelevantRuntimePermissions();
                }
                else if (allGranted && hasRequiredRuntimePermissions())
                {
                    StartBleScan();
                }
                else
                {
                    // Unexpected scenario encountered when handling permissions
                    Recreate();
                }
            }
        }

    }
}