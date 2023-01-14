namespace RacingCarsController.Common
{
    public record CarCommand(bool Forward, bool Backward, bool Left, bool Right, bool Lights, bool Turbo)
    {
        public bool IsNotMovingAndHaveSameState(CarCommand? command)
        {
            return command != null
                && Forward == command.Forward && Forward == false
                && Backward == command.Backward && Backward == false
                && Left == command.Left && Left == false
                && Right == command.Right && Right == false
                && Turbo == command.Turbo
                && Lights == command.Lights;
        }
    };
}
