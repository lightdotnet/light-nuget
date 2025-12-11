using Microsoft.Extensions.Hosting;
using Serilog;

namespace Light.Serilog
{
    public static class Startup
    {
        public static IHostBuilder ConfigureSerilog(this IHostBuilder host)
        {
            return host.UseSerilog(SerilogConfigurationExtensions.Configure);
        }
    }
}