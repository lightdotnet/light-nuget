using Light.AspNetCore.ExceptionHandlers;
using Light.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace WebHost.Tests;

public class ExceptionHandlerTests
{
    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private static DefaultHttpContext CreateContext(bool started = false)
    {
        var services = new ServiceCollection();
        services.AddOptions<ExceptionHandlerOptions>();

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            TraceIdentifier = "trace-1",
        };

        if (started)
            context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string ReadBody(HttpContext context)
    {
        var body = (MemoryStream)context.Response.Body;
        return Encoding.UTF8.GetString(body.ToArray());
    }

    private static readonly ExceptionHandler Handler = new(NullLogger<ExceptionHandler>.Instance);

    [TestCase(typeof(NotFoundException), 404)]
    [TestCase(typeof(ForbiddenException), 403)]
    [TestCase(typeof(ConflictException), 409)]
    [TestCase(typeof(UnauthorizedException), 401)]
    public async Task MapsExceptionBaseToStatusCode(Type exceptionType, int expectedStatus)
    {
        var context = CreateContext();
        var exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;

        var handled = await Handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(context.Response.StatusCode, Is.EqualTo(expectedStatus));
            Assert.That(ReadBody(context), Does.Contain("boom").And.Contain("trace-1"));
        });
    }

    [Test]
    public async Task UnwrapsWrappedExceptionBase()
    {
        var context = CreateContext();
        var exception = new InvalidOperationException("outer",
            new AggregateException("middle",
                new NotFoundException("not found")));

        await Handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(404));
            Assert.That(ReadBody(context), Does.Contain("not found"));
        });
    }

    [Test]
    public async Task UnknownException_Returns500_AndHidesMessage()
    {
        var context = CreateContext();

        await Handler.TryHandleAsync(context, new InvalidOperationException("secret detail"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(500));
            Assert.That(ReadBody(context), Does.Not.Contain("secret detail"));
        });
    }

    [Test]
    public async Task ClearsPartialResponseStateBeforeWritingError()
    {
        var context = CreateContext();
        context.Response.StatusCode = 201;
        context.Response.ContentType = "text/plain";
        context.Response.Headers.ETag = "\"stale\"";
        context.Response.Headers.ContentLength = 3;
        await context.Response.Body.WriteAsync("abc"u8.ToArray());

        await Handler.TryHandleAsync(context, new NotFoundException("not found"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(404));
            Assert.That(context.Response.ContentType, Does.StartWith("application/json"));
            Assert.That(context.Response.Headers.ContainsKey("ETag"), Is.False);
            Assert.That(context.Response.Headers.ContentLength, Is.Null);
            Assert.That(ReadBody(context), Does.StartWith("{").And.Contain("not found"));
        });
    }

    [Test]
    public async Task PreservesCorsHeadersWhenClearingResponse()
    {
        var context = CreateContext();
        context.Response.Headers.AccessControlAllowOrigin = "https://example.com";
        context.Response.Headers.Vary = "Origin";
        context.Response.Headers.ETag = "\"stale\"";

        await Handler.TryHandleAsync(context, new NotFoundException("not found"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(404));
            Assert.That(context.Response.Headers.AccessControlAllowOrigin.ToString(), Is.EqualTo("https://example.com"));
            Assert.That(context.Response.Headers.Vary.ToString(), Is.EqualTo("Origin"));
            Assert.That(context.Response.Headers.ContainsKey("ETag"), Is.False);
        });
    }

    [Test]
    public async Task ResponseStarted_DoesNotWriteOrChangeStatus()
    {
        var context = CreateContext(started: true);

        var handled = await Handler.TryHandleAsync(context, new NotFoundException("x"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.False);
            Assert.That(context.Response.StatusCode, Is.EqualTo(200));
            Assert.That(ReadBody(context), Is.Empty);
        });
    }

    [Test]
    public void Middleware_RethrowsWhenResponseStarted()
    {
        var context = CreateContext(started: true);
        var middleware = new ExceptionHandlerMiddleware(
            _ => throw new NotFoundException("x"),
            NullLogger<ExceptionHandlerMiddleware>.Instance);

        Assert.ThrowsAsync<NotFoundException>(() => middleware.InvokeAsync(context));
    }

    [Test]
    public async Task ClientAbort_IsNotTreatedAsServerError()
    {
        var context = CreateContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        context.RequestAborted = cts.Token;

        var handled = await Handler.TryHandleAsync(context, new OperationCanceledException(cts.Token), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(context.Response.StatusCode, Is.EqualTo(499));
            Assert.That(ReadBody(context), Is.Empty);
        });
    }
}
