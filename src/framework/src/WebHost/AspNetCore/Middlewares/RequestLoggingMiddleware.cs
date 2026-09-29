using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

namespace Light.AspNetCore.Middlewares;

public class RequestLoggingMiddleware(
    RequestDelegate next,
    IOptions<RequestLoggingOptions> options,
    // must use Microsoft Logger because only Singleton services can be resolved by constructor injection in Middleware
    ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestLoggingOptions _settings = options.Value;
    private readonly IReadOnlyList<string> _excludePaths = BuildExcludePaths(options.Value);

    private static IReadOnlyList<string> BuildExcludePaths(RequestLoggingOptions settings)
    {
        var excludePath = new List<string> { "hangfire", "swagger" };
        if (settings.ExcludePaths is not null)
            excludePath.AddRange(settings.ExcludePaths);

        return excludePath;
    }

    /// <summary>
    /// Hard upper bound for <see cref="RequestLoggingOptions.MaxBodyLogBytes"/>: keeps buffer sizes (and the
    /// <c>+1</c> used for truncation detection) far away from <see cref="int.MaxValue"/>.
    /// </summary>
    internal const int MaxBodyLogBytesLimit = 16 * 1024 * 1024;

    private int MaxBodyBytes => Math.Clamp(_settings.MaxBodyLogBytes, 0, MaxBodyLogBytesLimit);

    public async Task InvokeAsync(HttpContext context)
    {
        if (CheckSkipWriteLog(context.Request))
        {
            // Continue processing the request
            await next(context);
            return;
        }

        var timer = Stopwatch.StartNew();
        Exception? failure = null;

        try
        {
            await WriteRequestBodyAsync(context);

            await WriteResponseAsync(context);
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            timer.Stop();

            // always log, including requests that failed with an unhandled exception
            WriteRequestLog(context, timer.ElapsedMilliseconds, failure);
        }
    }

    private bool CheckSkipWriteLog(HttpRequest httpRequest)
    {
        var path = httpRequest.Path.ToString();
        return _excludePaths.Any(c => path.Contains(c));
    }

    private void WriteRequestLog(HttpContext context, long elapsedMilliseconds, Exception? failure)
    {
        var httpRequest = context.Request;

        var traceId = context.TraceIdentifier;
        var ip = httpRequest.HttpContext.Connection.RemoteIpAddress;
        var clientIp = ip == null ? "UnknownIP" : ip.ToString();

        var requestMethod = httpRequest.Method;
        var requestPath = httpRequest.Path;
        var requestQuery = httpRequest.QueryString.ToString();
        var requestScheme = httpRequest.Scheme;

        if (failure is not null)
        {
            // status code is not final yet: an outer exception handler decides it
            var failedContent = $"{traceId} {requestScheme} {requestMethod} {failure.GetType().Name} {requestPath}{requestQuery} FromIP: {clientIp}";

            logger.LogWarning("{content} failed in {elapsedMilliseconds} ms", failedContent, elapsedMilliseconds);
            return;
        }

        var statusCode = context.Response.StatusCode;

        var logContent = $"{traceId} {requestScheme} {requestMethod} {statusCode} {requestPath}{requestQuery} FromIP: {clientIp}";

        logger.LogInformation("{content} in {elapsedMilliseconds} ms", logContent, elapsedMilliseconds);
    }

    private async Task WriteRequestBodyAsync(HttpContext context)
    {
        if (_settings.IncludeRequest is false || !IsTextContentType(context.Request.ContentType))
        {
            return;
        }

        var requestBody = await ReadBodyAsync(context.Request, MaxBodyBytes, context.RequestAborted);

        if (string.IsNullOrEmpty(requestBody))
        {
            return;
        }

        var logContent = $"{context.TraceIdentifier} request {requestBody}";

        logger.LogInformation("{content}", logContent);
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request, int maxBytes, CancellationToken cancellationToken)
    {
        if (maxBytes == 0)
            return string.Empty;

        // Ensure the request's body can be read multiple times
        // (for the next middlewares in the pipeline).
        request.EnableBuffering();

        // read at most maxBytes (+1 to detect truncation) instead of the whole body;
        // maxBytes is clamped to MaxBodyLogBytesLimit so the +1 can't overflow.
        // when Content-Length is known and smaller, only rent what is needed
        var capacity = request.ContentLength is long contentLength && contentLength < maxBytes
            ? (int)contentLength + 1
            : maxBytes + 1;

        var buffer = ArrayPool<byte>.Shared.Rent(capacity);
        try
        {
            var read = 0;
            int n;
            while (read < capacity
                && (n = await request.Body.ReadAsync(buffer.AsMemory(read, capacity - read), cancellationToken)) > 0)
            {
                read += n;
            }

            // Reset the request's body stream position for
            // next middleware in the pipeline.
            request.Body.Position = 0;

            return FormatBody(buffer, Math.Min(read, maxBytes), truncated: read > maxBytes, request.ContentType);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string FormatBody(byte[] buffer, int length, bool truncated, string? contentType)
    {
        var body = Encoding.UTF8.GetString(buffer, 0, length);

        if (truncated)
            return $"{body}...[truncated]";

        // minify if is JSON
        if (contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) is true)
            body = Minify(body);

        return body;
    }

    /// <summary>
    /// Minify a JSON string; returns the input unchanged when it is not valid JSON.
    /// </summary>
    internal static string Minify(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement);
        }
        catch (JsonException)
        {
            // malformed body (typically a 400): log it raw instead of failing the request
            return json;
        }
    }

    /// <summary>
    /// Body logging is limited to textual content types (JSON, XML, text, form-urlencoded).
    /// </summary>
    internal static bool IsTextContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("graphql", StringComparison.OrdinalIgnoreCase);
    }

    private async Task WriteResponseAsync(HttpContext context)
    {
        if (_settings.IncludeResponse is false || MaxBodyBytes == 0)
        {
            await next(context);
            return;
        }

        // Tee the response: write through to the client while capturing the first bytes for logging
        // (no full buffering, streaming responses keep working).
        // Swap the whole body feature (not just Response.Body) so writes via Response.BodyWriter are captured too.
        var originalFeature = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();

        using var capture = new CapturingStream(originalFeature.Stream, MaxBodyBytes);
        var captureFeature = new CapturingResponseBodyFeature(originalFeature, capture);
        context.Features.Set<IHttpResponseBodyFeature>(captureFeature);

        var completed = false;
        try
        {
            // Continue processing the request; let exceptions propagate to the
            // exception-handling pipeline instead of being swallowed here
            await next(context);
            completed = true;
        }
        finally
        {
            try
            {
                // push bytes still buffered in the BodyWriter through to the client before restoring;
                // skipped on failure so a partial body doesn't start the response ahead of the exception handler
                if (completed)
                    await captureFeature.FlushWriterAsync(context.RequestAborted);
            }
            finally
            {
                context.Features.Set(originalFeature);
            }
        }

        try
        {
            var response = context.Response;
            if (capture.CapturedLength == 0
                || !IsTextContentType(response.ContentType)
                || response.Headers.ContentEncoding.Count > 0) // compressed
            {
                return;
            }

            var responseText = FormatBody(capture.GetCapturedBuffer(), capture.CapturedLength, capture.Truncated, contentType: null);

            var logContent = $"{context.TraceIdentifier} response {responseText}";

            logger.LogInformation("{content}", logContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception when writing response log.");
        }
    }

    /// <summary>
    /// Response body feature routing both <see cref="Stream"/> and <see cref="Writer"/> through the capturing stream.
    /// </summary>
    private sealed class CapturingResponseBodyFeature(IHttpResponseBodyFeature inner, CapturingStream stream) : IHttpResponseBodyFeature
    {
        private PipeWriter? _writer;

        public Stream Stream => stream;

        public PipeWriter Writer => _writer ??= PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

        public void DisableBuffering() => inner.DisableBuffering();

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await FlushWriterAsync(cancellationToken);
            await inner.StartAsync(cancellationToken);
        }

        public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            await FlushWriterAsync(cancellationToken);
            await SendFileFallback.SendFileAsync(stream, path, offset, count, cancellationToken);
        }

        public async Task CompleteAsync()
        {
            await FlushWriterAsync(default);
            await inner.CompleteAsync();
        }

        /// <summary>
        /// Flushes data buffered in <see cref="Writer"/>; no-op when the writer was never used.
        /// </summary>
        public async Task FlushWriterAsync(CancellationToken cancellationToken)
        {
            if (_writer is not null)
                await _writer.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Write-through stream that keeps a copy of the first <c>limit</c> bytes written,
    /// in a pooled buffer that grows on demand (no up-front <c>limit</c>-sized allocation per request).
    /// </summary>
    private sealed class CapturingStream(Stream inner, int limit) : Stream
    {
        private const int InitialBufferSize = 4 * 1024;

        private byte[]? _buffer;

        public int CapturedLength { get; private set; }

        public bool Truncated { get; private set; }

        public byte[] GetCapturedBuffer() => _buffer ?? [];

        private void Capture(ReadOnlySpan<byte> data)
        {
            var free = limit - CapturedLength;
            if (data.Length > free)
                Truncated = true;

            var count = Math.Min(free, data.Length);
            if (count > 0)
            {
                EnsureCapacity(CapturedLength + count);
                data[..count].CopyTo(_buffer.AsSpan(CapturedLength));
                CapturedLength += count;
            }
        }

        private void EnsureCapacity(int required)
        {
            if (_buffer is not null && _buffer.Length >= required)
                return;

            // grow geometrically, never beyond the limit (required <= limit, long math avoids overflow)
            var size = (int)Math.Min(limit, Math.Max(required, Math.Max(InitialBufferSize, 2L * (_buffer?.Length ?? 0))));

            var newBuffer = ArrayPool<byte>.Shared.Rent(size);
            if (_buffer is not null)
            {
                _buffer.AsSpan(0, CapturedLength).CopyTo(newBuffer);
                ArrayPool<byte>.Shared.Return(_buffer);
            }

            _buffer = newBuffer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }

            base.Dispose(disposing);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Capture(buffer.AsSpan(offset, count));
            inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Capture(buffer);
            inner.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Capture(buffer.AsSpan(offset, count));
            return inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Capture(buffer.Span);
            return inner.WriteAsync(buffer, cancellationToken);
        }

        public override void Flush() => inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
