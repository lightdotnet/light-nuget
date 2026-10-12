using Serilog;

namespace Light.Serilog
{
    public class Serilogger
    {
        private static readonly object _sync = new object();
        private static volatile bool _initialized;

        public static ILogger Initialize() =>
            new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateLogger();

        /// <summary>
        /// Assigns <see cref="Log.Logger"/> = <see cref="Initialize"/> exactly once per process.
        /// Thread-safe: concurrent callers are serialized, and every caller returns only after the logger is assigned.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (_sync)
            {
                if (!_initialized)
                {
                    Log.Logger = Initialize();
                    _initialized = true;
                }
            }
        }
    }
}
