using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Light.AspNetCore.Builder;

public static class JsonConfigurationLocation
{
    /// <summary>
    /// Add configuration files *.json from folder
    /// </summary>
    public static IHostApplicationBuilder LoadConfigurationFrom(this IHostApplicationBuilder host, string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return host; // use default config
        }

        var env = host.Environment.EnvironmentName;

        var configuration = host.Configuration;

        // get directory info
        var dInfo = new DirectoryInfo(path);
        if (dInfo.Exists)
        {
            var files = dInfo.GetFiles("*.json").Where(x => !x.Name.StartsWith("appsettings"));

            foreach (var file in files)
            {
                configuration
                    .AddJsonFile(Path.Combine(path, file.Name), optional: false, reloadOnChange: true);
            }
        }

        // load after add json files for can override values at root configurations
        configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

        configuration.AddEnvironmentVariables();

        return host;
    }

    public static IHostApplicationBuilder LoadConfigurationFrom(this IHostApplicationBuilder host, string[]? paths)
    {
        if (paths is null || paths.Length == 0)
        {
            return host; // use default config
        }

        // combine paths to string
        var path = Path.Combine(paths);

        return host.LoadConfigurationFrom(path);
    }
}
