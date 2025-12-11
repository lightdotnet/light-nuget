using EventBusSample.Common;
using Light.EventBus.Abstractions;
using Light.Extensions.DependencyInjection;
using MassTransit;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var executingAssembly = Assembly.GetExecutingAssembly();

builder.Services.AddMassTransit(x =>
{
    //x.AddConsumer<ColorChangedConsumer, ColorChangedConsumerDefinition>();
    x.AddConsumers(executingAssembly);
    x.ConfigRabbitMQ(mq =>
    {
        mq.Host = "10.114.32.16";
        mq.Username = "super";
        mq.Password = "adm!n";
        mq.Exclude<IntegrationEvent>();
        mq.Exclude<EventBase>();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/changed", async (
    Color oldColor,
    Color newColor,
    IEventBus eventBus,
    ILogger<ColorChangedIntegrationEvent> logger) =>
{
    var colorUpdatedEvent = new ColorChangedIntegrationEvent
    {
        OldColor = oldColor.ToString(),
        NewColor = newColor.ToString(),
        ChangeOn = DateTime.Now,
    };

    await eventBus.Publish(colorUpdatedEvent);

    return;
})
.WithName("Publish-Change");

app.MapGet("/removed", async (
    Color color,
    IEventBus eventBus) =>
{
    var colorUpdatedEvent = new ColorRemovedIntegrationEvent(color.ToString());

    await eventBus.Publish(colorUpdatedEvent);

    return;
})
.WithName("Publish-Remove");

app.Run();