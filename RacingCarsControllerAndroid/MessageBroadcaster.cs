using RacingCarsController.Common;
using Xamarin.Essentials;

namespace RacingCarsControllerAndroid
{
    public class MessageBroadcaster
    {
        private IRacingCar? _racingCar;
        private CarCommand? _lastCommand;
        private System.Timers.Timer _timer;
        private CancellationTokenSource _cts;
        private CancellationToken _cancellationToken;

        public MessageBroadcaster()
        {
            _timer = new System.Timers.Timer(TimeSpan.FromMilliseconds(100));
            _timer.AutoReset = false;
            _timer.Elapsed += TimerElapsed;
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;
        }

        public void SetCar(IRacingCar racingCar)
        {
            CancelBroadcasting();
            _racingCar = racingCar;
            _timer.Start();
        }

        private async void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_lastCommand != null && _racingCar != null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => await _racingCar.SendCommandAsync(_lastCommand, _cancellationToken));
            }
            if (!_cancellationToken.IsCancellationRequested)
            {
                _timer.Start();
            }
        }

        public void QueueCommand(CarCommand command)
        {
            if (_lastCommand != command)
            {
                System.Diagnostics.Debug.WriteLine($"APP LOG: New command {command}");
                _timer.Stop();
                _cts.Cancel();
                _cts = new CancellationTokenSource();
                _cancellationToken = _cts.Token;
                _lastCommand = command;
                TimerElapsed(null, null);
            }
        }

        public void CancelBroadcasting()
        {
            _lastCommand = null;
            _racingCar = null;
            _timer.Stop();
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;
        }
    }
}