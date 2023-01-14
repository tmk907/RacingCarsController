namespace RacingCarsController.Common
{
    public interface IRacingCar : IAsyncDisposable
    {
        event EventHandler<int> BatteryLevelChanged;

        Task SendCommandAsync(CarCommand command, CancellationToken cancellationToken);
        Task SubscribeToBatteryNotifications();
    }

    public abstract class RacingCar : IRacingCar
    {
        public abstract string ControlServiceUUID { get; }
        public abstract string ControlCharacteristicUUID { get; }

        public abstract string BatteryServiceUUID { get; }
        public abstract string BatteryCharacteristicUUID { get; }

        public event EventHandler<int>? BatteryLevelChanged;

        protected IBLEDevice Device;
        private readonly ILogger _logger;
        protected bool IgnoreSubsequentSameCommands;
        private CarCommand? _previousCommand;

        public RacingCar(IBLEDevice device, ILogger logger)
        {
            Device = device;
            _logger = logger;
            Device.CharacteristicChanged += Device_CharacteristicChanged;
            IgnoreSubsequentSameCommands = true;
        }

        private void Device_CharacteristicChanged(object? sender, byte[] args)
        {
            var level = GetBatteryLevel(args);
            _logger.Log($"Battery level is {level}");
            OnBatteryLevelChanged(level);
        }

        public virtual async Task SendCommandAsync(CarCommand command, CancellationToken cancellationToken)
        {
            if (command.IsNotMovingAndHaveSameState(_previousCommand))
            {
                _logger.Log($"Is not moving and has same state {command}");
                return;
            }
            if (IgnoreSubsequentSameCommands && command == _previousCommand)
            {
                _logger.Log($"Don't send command {command}");
                return;
            };
            _previousCommand = command;
            _logger.Log($"Send command {command}");

            var data = PreparePayload(command);
            await Device.WriteCharacteristics(ControlServiceUUID, ControlCharacteristicUUID, data, cancellationToken);
        }

        public virtual async Task SubscribeToBatteryNotifications()
        {
            await Device.SubscribeToNotifications(BatteryServiceUUID, BatteryCharacteristicUUID, CancellationToken.None);
        }

        protected abstract byte[] PreparePayload(CarCommand command);

        protected abstract int GetBatteryLevel(byte[] data);

        protected virtual void OnBatteryLevelChanged(int level)
        {
            BatteryLevelChanged?.Invoke(this, level);
        }

        public async ValueTask DisposeAsync()
        {
            Device.CharacteristicChanged -= Device_CharacteristicChanged;
            await Device.DisposeAsync();
        }
    }
}
