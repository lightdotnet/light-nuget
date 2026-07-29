using EventBusSample.Common;
using Light.Extensions.DependencyInjection;
using MassTransit;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var executingAssembly = Assembly.GetExecutingAssembly();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(executingAssembly);
    x.ConfigRabbitMQ(mq =>
    {
        mq.Host = builder.Configuration["RabbitMQ:Host"]!;
        mq.Username = builder.Configuration["RabbitMQ:Username"]!;
        mq.Password = builder.Configuration["RabbitMQ:Password"]!;
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