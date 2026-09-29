using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Light.AspNetCore.Builder;

public static class JsonConfigurationLocation
{
    /// <summary>
    /// Add configuration files *.json from folder
    /// </summary>
    /// <remarks>
    /// A relative <paramref name="path"/> is resolved against the host's content root.
    /// </remarks>
    public static IHostApplicationBuilder LoadConfigurationFrom(this IHostApplicationBuilder host, string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return host; // use default config
        }

        return host.LoadConfigurationFrom([path]);
    }

    /// <summary>
    /// Add configuration files *.json from folders (in the given order), then the root
    /// appsettings.json / appsettings.{env}.json and environment variables once.
    /// </summary>
    /// <remarks>
    /// Relative paths are resolved against the host's content root.
    /// </remarks>
    public static IHostApplicationBuilder LoadConfigurationFrom(this IHostApplicationBuilder host, string[]? paths)
    {
        var validPaths = paths?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        if (validPaths is null || validPaths.Length == 0)
        {
            return host; // use default config
        }

        var env = host.Environment.EnvironmentName;
        var contentRoot = host.Environment.ContentRootPath;

        var configuration = host.Configuration;

        foreach (var path in validPaths)
        {
            // resolve relative to content root (same base as the root appsettings files below)
            var dInfo = new DirectoryInfo(Path.GetFullPath(path, contentRoot));
            if (!dInfo.Exists)
                continue;

            // deterministic order: file system enumeration order is not guaranteed
            var files = dInfo.GetFiles("*.json")
                .Where(x => !x.Name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Name, StringComparer.Ordinal);

            foreach (var file in files)
            {
                configuration
                    .AddJsonFile(file.FullName, optional: false, reloadOnChange: true);
            }
        }

        // load after add json files for can override values at root configurations
        configuration
            .AddJsonFile(Path.Combine(contentRoot, "appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(contentRoot, $"appsettings.{env}.json"), optional: true, reloadOnChange: true);

        configuration.AddEnvironmentVariables();

        return host;
    }
}
