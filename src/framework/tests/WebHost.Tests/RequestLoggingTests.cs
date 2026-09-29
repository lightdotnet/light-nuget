using Light.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace WebHost.Tests;

public class RequestLoggingTests
{
    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    private static (RequestLoggingMiddleware Middleware, ListLogger<RequestLoggingMiddleware> Logger) Create(
        RequestDelegate next, RequestLoggingOptions? options = null)
    {
        var logger = new ListLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            next,
            Options.Create(options ?? new RequestLoggingOptions { Enable = true, IncludeRequest = true, IncludeResponse = true }),
            logger);

        return (middleware, logger);
    }

    private static DefaultHttpContext CreateContext(string body, string contentType = "application/json")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/orders";
        context.Request.ContentType = contentType;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Test]
    public void Minify_InvalidJson_ReturnsRawInput()
    {
        const string invalid = "{ \"a\": ";

        Assert.That(RequestLoggingMiddleware.Minify(invalid), Is.EqualTo(invalid));
    }

    [Test]
    public void Minify_ValidJson_RemovesWhitespace()
    {
        Assert.That(RequestLoggingMiddleware.Minify("{ \"a\" : 1 }"), Is.EqualTo("{\"a\":1}"));
    }

    [Test]
    public async Task MalformedJsonBody_DoesNotFailRequest_AndBodyStillReadable()
    {
        string? downstreamBody = null;
        var (middleware, logger) = Create(async ctx =>
        {
            downstreamBody = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
            ctx.Response.StatusCode = 400;
        });

        var context = CreateContext("{ not json");

        await middleware.InvokeAsync(context);

        Assert.Multiple(() =>
        {
            Assert.That(downstreamBody, Is.EqualTo("{ not json"));
            Assert.That(logger.Entries.Any(e => e.Message.Contains("request { not json")), Is.True);
            Assert.That(logger.Entries.Any(e => e.Message.Contains(" 400 ")), Is.True);
        });
    }

    [Test]
    public async Task LargeBodies_AreTruncatedInLog_ButResponseIsComplete()
    {
        var large = new string('x', 100);
        var (middleware, logger) = Create(async ctx =>
        {
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.WriteAsync(large);
        }, new RequestLoggingOptions { Enable = true, IncludeRequest = true, IncludeResponse = true, MaxBodyLogBytes = 10 });

        var context = CreateContext(large, "text/plain");

        await middleware.InvokeAsync(context);

        var responseBody = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(responseBody, Is.EqualTo(large));
            Assert.That(logger.Entries.Any(e => e.Message.EndsWith("request xxxxxxxxxx...[truncated]")), Is.True);
            Assert.That(logger.Entries.Any(e => e.Message.EndsWith("response xxxxxxxxxx...[truncated]")), Is.True);
        });
    }

    [Test]
    public async Task BodyWriterResponse_IsCapturedAndFullyForwarded()
    {
        var (middleware, logger) = Create(async ctx =>
        {
            ctx.Response.ContentType = "application/json";
            // write through the PipeWriter without flushing: the middleware must flush it before restoring
            var bytes = Encoding.UTF8.GetBytes("{\"ok\":true}");
            bytes.CopyTo(ctx.Response.BodyWriter.GetSpan(bytes.Length));
            ctx.Response.BodyWriter.Advance(bytes.Length);
            await Task.CompletedTask;
        });

        var context = CreateContext("{}");
        var originalFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();

        await middleware.InvokeAsync(context);

        var responseBody = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(responseBody, Is.EqualTo("{\"ok\":true}"));
            Assert.That(logger.Entries.Any(e => e.Message.EndsWith("response {\"ok\":true}")), Is.True);
            Assert.That(context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>(), Is.SameAs(originalFeature));
        });
    }

    [Test]
    public async Task MaxBodyLogBytes_IntMaxValue_DoesNotOverflow()
    {
        var (middleware, logger) = Create(async ctx =>
        {
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.WriteAsync("pong");
        }, new RequestLoggingOptions { Enable = true, IncludeRequest = true, IncludeResponse = true, MaxBodyLogBytes = int.MaxValue });

        var context = CreateContext("ping", "text/plain");

        await middleware.InvokeAsync(context);

        Assert.Multiple(() =>
        {
            Assert.That(logger.Entries.Any(e => e.Message.EndsWith("request ping")), Is.True);
            Assert.That(logger.Entries.Any(e => e.Message.EndsWith("response pong")), Is.True);
        });
    }

    [Test]
    public async Task BinaryBodies_AreNotLogged()
    {
        var (middleware, logger) = Create(_ => Task.CompletedTask);

        var context = CreateContext("binary-data", "application/octet-stream");

        await middleware.InvokeAsync(context);

        Assert.That(logger.Entries.Any(e => e.Message.Contains("binary-data")), Is.False);
    }

    [Test]
    public void FailedRequest_IsStillLogged()
    {
        var (middleware, logger) = Create(_ => throw new InvalidOperationException("boom"));

        var context = CreateContext("{}");

        Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        Assert.That(logger.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("InvalidOperationException")), Is.True);
    }
}
