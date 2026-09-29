namespace Light.AspNetCore.Middlewares;

public class RequestLoggingOptions
{
    public bool Enable { get; set; }

    public bool IncludeRequest { get; set; }

    public bool IncludeResponse { get; set; }

    public List<string>? ExcludePaths { get; set; }

    /// <summary>
    /// Maximum number of bytes of the request/response body to capture for logging (default 32 KB).
    /// Larger bodies are truncated in the log; the actual request/response is never altered.
    /// Values are clamped to the range 0 (disables body logging) to 16 MB.
    /// </summary>
    public int MaxBodyLogBytes { get; set; } = 32 * 1024;
}
