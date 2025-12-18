using Light.ActiveDirectory;
using Light.Caching.Infrastructure;
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

/*
builder.Services.AddMicrosoftGraph(opt =>
{
    opt.ClientSecret = "";
    opt.ClientId = "";
    opt.TenantId = "";
});
*/
builder.Services.AddFileGenerator();

var settings = builder.Configuration.GetSection("Caching").Get<CacheOptions>();
builder.Services.AddCache(opt =>
{
    opt.Provider = settings!.Provider;
    opt.RedisHost = settings.RedisHost;
    opt.RedisPassword = settings.RedisPassword;
});

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();