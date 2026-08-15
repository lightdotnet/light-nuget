using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Light.Serilog
{
    public static class SerilogOptionsExtensions
    {
        public static IEnumerable<WriteToOptions>? GetWriteToOptions(IConfiguration configuration)
        {
            return configuration.GetSection("Serilog:WriteTo").Get<IEnumerable<WriteToOptions>>();
        }

        public static WriteToOptions? GetWriteTo(IConfiguration configuration, string firstElementName)
        {
            var options = GetWriteToOptions(configuration);

            return options?.FirstOrDefault(x => x.Name == firstElementName);
        }
    }
}