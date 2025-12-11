namespace Light.Extensions.Logging;

public interface ILogger<out T>
{
    void LogInformation(string? message, params object?[] args);

    void LogWarning(string? message, params object?[] args);

    void LogError(string? message, params object?[] args);
}
