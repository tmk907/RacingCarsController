namespace RacingCarsController.Common
{
    public interface ILogger
    {
        void Log(string text);
    }

    public class SystemDiagnosticsLogger : ILogger
    {
        public void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}
