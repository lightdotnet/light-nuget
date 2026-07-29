using Serilog;

namespace Light.Serilog
{
    public class Serilogger
    {
        private static bool _initialized;

        public static ILogger Initialize() =>
            new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateLogger();

        public static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Log.Logger = Initialize();
                _initialized = true;
            }
        }
    }
}
