using Microsoft.Extensions.Logging;

namespace Light.Extensions.Logging;

internal class LoggerAdapter<T>(ILoggerFactory loggerFactory) : ILogger<T>
{
    private readonly Microsoft.Extensions.Logging.ILogger<T> _logger = loggerFactory.CreateLogger<T>();

    public void LogInformation(string? message, params object?[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogWarning(string? message, params object?[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogError(string? message, params object?[] args)
    {
        _logger.LogError(message, args);
    }
}
