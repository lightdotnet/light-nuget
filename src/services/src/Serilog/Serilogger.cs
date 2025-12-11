using Serilog;

namespace Light.Serilog
{
    public class Serilogger
    {
        public static ILogger Initialize() =>
            new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateLogger();

        public static void EnsureInitialized()
        {
            if (Log.Logger.GetType() != typeof(Serilogger))
            {
                Log.Logger = Initialize();
            }
        }
    }
}
