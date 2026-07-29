using Microsoft.Extensions.Hosting;
using Serilog;

namespace Light.Serilog
{
    public static class SerilogHostBuilderExtensions
    {
        public static IHostBuilder ConfigureSerilog(this IHostBuilder host)
        {
            return host.UseSerilog(SerilogConfigurationExtensions.Configure);
        }
    }
}