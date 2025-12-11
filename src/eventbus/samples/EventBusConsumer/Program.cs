using EventBusSample.Common;
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
        mq.Password = "123";
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

app.Run();