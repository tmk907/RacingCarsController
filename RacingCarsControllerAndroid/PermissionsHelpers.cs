using Android.Content;
using Xamarin.Essentials;

namespace RacingCarsControllerAndroid
{
    internal class PermissionsHelpers
    {
        private readonly Context _context;

        public PermissionsHelpers(Context context)
        {
            _context = context;
        }

        public async Task<PermissionStatus> CheckAndRequestBluetoothPermissions()
        {
            PermissionStatus status;
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
            {
                status = await Permissions.CheckStatusAsync<BluetoothSPermission>();

                if (status == PermissionStatus.Granted)
                    return status;

                if (Permissions.ShouldShowRationale<BluetoothSPermission>())
                {
                    var canRequest = await ShowBluetoothRequestPermissionRationale();
                    if (canRequest)
                    {
                        await Permissions.RequestAsync<BluetoothSPermission>();
                    }
                    return PermissionStatus.Denied;
                }

                status = await Permissions.RequestAsync<BluetoothSPermission>();
            }
            else
            {
                status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status == PermissionStatus.Granted)
                    return status;

                if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
                {
                    var canRequest = await ShowLocationRequestPermissionRationale();
                    if (canRequest)
                    {
                        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    }
                    return PermissionStatus.Denied;
                }

                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status;
        }

        private async Task<bool> ShowLocationRequestPermissionRationale()
        {
            var tcs = new TaskCompletionSource<bool>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(_context);
                builder.SetTitle("Location permission required");
                builder.SetMessage("Starting from Android M (6.0), the system requires apps to be granted " +
                "location access in order to scan for BLE devices.");
                builder.SetCancelable(false);
                builder.SetPositiveButton(Resource.String.ok, (sender, args) => tcs.SetResult(true));
                builder.SetNegativeButton("Cancel", (sender, args) => tcs.SetResult(false));
                builder.Show();
            });

            var result = await tcs.Task;
            return result;
        }

        private async Task<bool> ShowBluetoothRequestPermissionRationale()
        {
            var tcs = new TaskCompletionSource<bool>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(_context);
                builder.SetTitle("Bluetooth permissions required");
                builder.SetMessage("Starting from Android 12, the system requires apps to be granted " +
                "Bluetooth access in order to scan for and connect to BLE devices.");
                builder.SetCancelable(false);
                builder.SetPositiveButton(Resource.String.ok, (sender, args) => tcs.SetResult(true));
                builder.SetNegativeButton("Cancel", (sender, args) => tcs.SetResult(false));
                builder.Show();
            });

            var result = await tcs.Task;
            return result;
        }
    }

    public class BluetoothSPermission : Xamarin.Essentials.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new List<(string androidPermission, bool isRuntime)>
            {
                (Android.Manifest.Permission.BluetoothScan, true),
                (Android.Manifest.Permission.BluetoothConnect, true)
            }.ToArray();
    }
}
