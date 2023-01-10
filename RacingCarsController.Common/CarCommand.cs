namespace RacingCarsController.Common
{
    public record CarCommand(bool Forward, bool Backward, bool Left, bool Right, bool Lights, bool Turbo);
}
