using Light.Extensions.DependencyInjection;
using Light.Serilog;
using System.Reflection;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

var executingAssembly = Assembly.GetExecutingAssembly();

builder.Host.ConfigureSerilog();

//builder.Services.AddHostedService<Worker>();

#pragma warning disable CA1416
builder.Services.AddActiveDirectory(opt => opt.Name = "company.local");
#pragma warning restore

// Microsoft Graph is only registered when credentials are supplied (e.g. via user-secrets:
// Graph:TenantId / Graph:ClientId / Graph:ClientSecret). Otherwise GraphController returns 503.
var graphSection = builder.Configuration.GetSection("Graph");
if (!string.IsNullOrWhiteSpace(graphSection["TenantId"])
    && !string.IsNullOrWhiteSpace(graphSection["ClientId"])
    && !string.IsNullOrWhiteSpace(graphSection["ClientSecret"]))
{
    builder.Services.AddMicrosoftGraph(opt =>
    {
        opt.TenantId = graphSection["TenantId"];
        opt.ClientId = graphSection["ClientId"];
        opt.ClientSecret = graphSection["ClientSecret"];
    });
}

builder.Services.AddFileGenerator();

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new ByteArrayModelBinderProvider());
});

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<RawByteArrayBodyFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();