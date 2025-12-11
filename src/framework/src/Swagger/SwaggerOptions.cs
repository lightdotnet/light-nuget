namespace Light.AspNetCore.Swagger;

public class SwaggerOptions
{
    public bool Enable { get; set; }

    public string? Title { get; set; }

    public string? SecurityScheme { get; set; }

    public bool VersionDefinition { get; set; }
}