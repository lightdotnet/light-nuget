namespace Light.AspNetCore.Middlewares;

public class RequestLoggingOptions
{
    public bool Enable { get; set; }

    public bool IncludeRequest { get; set; }

    public bool IncludeResponse { get; set; }

    public List<string>? ExcludePaths { get; set; }
}
