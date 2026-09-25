using System.Diagnostics;
using System.Text;
using System.Text.Json;
using StowellCoAPI.Services;

namespace StowellCoAPI.Middleware
{
    // NEW (2026-09-24) - records EVERY API call (all users) into dbo.ApiCallLog: who, when, method, path, status,
    // how long, and - for a failed call (HTTP >= 400 or an unhandled exception) - the reason. See
    // ApiCallLogService / db\api-call-log-2026-09-24.sql. Request bodies are never stored.
    public class ApiCallLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApiCallLogService _log;

        public ApiCallLoggingMiddleware(RequestDelegate next, ApiCallLogService log)
        {
            _next = next;
            _log = log;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ShouldSkip(context))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var original = context.Response.Body;
            var capture = new ErrorBodyCaptureStream(original, context.Response);
            context.Response.Body = capture;
            Exception? failure = null;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                failure = ex;
                throw;          // still let the normal exception handling answer the caller
            }
            finally
            {
                stopwatch.Stop();
                context.Response.Body = original;
                try { Record(context, capture, stopwatch.ElapsedMilliseconds, failure); }
                catch { /* logging must never break a request */ }
            }
        }

        private static bool ShouldSkip(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            return HttpMethods.IsOptions(context.Request.Method)                       // CORS preflight - not a real call
                || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/api/ClientLog", StringComparison.OrdinalIgnoreCase)   // logs itself (Source = 'Client')
                || path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
        }

        private void Record(HttpContext context, ErrorBodyCaptureStream capture, long elapsedMs, Exception? failure)
        {
            int status = failure != null ? 500 : context.Response.StatusCode;
            bool isError = failure != null || status >= 400;

            // The admin log page polls api/ApiLog/*; don't let viewing the log fill the log. Failures of those calls are still recorded.
            if (!isError && (context.Request.Path.Value ?? string.Empty).StartsWith("/api/ApiLog", StringComparison.OrdinalIgnoreCase)) return;

            var entry = new ApiCallLogEntry
            {
                UserName = ResolveUser(context),
                Source = "API",
                HttpMethod = context.Request.Method,
                RequestPath = context.Request.Path.Value,
                QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                StatusCode = status,
                DurationMs = (int)Math.Min(elapsedMs, int.MaxValue),
                IsError = isError,
                ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                TraceId = context.TraceIdentifier
            };

            if (failure != null)
            {
                entry.ErrorType = failure.GetType().Name;
                entry.ErrorMessage = failure.Message;
                entry.Details = failure.ToString();          // includes the stack trace
            }
            else if (isError)
            {
                entry.ErrorType = "HttpError";
                var body = capture.CapturedText();
                entry.ErrorMessage = ExtractMessage(body) ?? $"HTTP {status}";
                entry.Details = body;
            }

            _log.Enqueue(entry);
        }

        // The API's own error bodies look like { "message": "...", "details": "..." } (camelCase) - prefer the
        // specific "details"; ASP.NET validation errors use { "title": "...", "errors": {...} }.
        private static string? ExtractMessage(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var name in new[] { "details", "Details", "message", "Message", "title", "Title" })
                    {
                        if (doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString()))
                            return v.GetString();
                    }
                }
            }
            catch { /* not JSON */ }
            return body.Length > 500 ? body.Substring(0, 500) : body;
        }

        // Who made the call. The Angular app sends X-User-Email (the signed-in user) on every API call; fall back to the
        // email/upn claim of a bearer token if there is one, else "anonymous". The API does not authenticate callers, so this
        // is an audit label, not proof of identity.
        internal static string ResolveUser(HttpContext context)
        {
            string? header = context.Request.Headers["X-User-Email"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header)) return header.Trim();

            string? auth = context.Request.Headers.Authorization.FirstOrDefault();
            if (auth != null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var parts = auth.Substring(7).Split('.');
                    if (parts.Length >= 2)
                    {
                        string payload = parts[1].Replace('-', '+').Replace('_', '/');
                        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                        foreach (var claim in new[] { "preferred_username", "upn", "email", "unique_name", "name" })
                            if (doc.RootElement.TryGetProperty(claim, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString()))
                                return v.GetString()!;
                    }
                }
                catch { /* unreadable token - fall through */ }
            }
            return "anonymous";
        }
    }

    // Write-through stream that forwards everything to the real response body untouched and, only when the response is
    // an error (status >= 400 at the time of the write), keeps the first ~4 KB so the error message can be logged.
    // Successful (and large) responses are never buffered.
    internal sealed class ErrorBodyCaptureStream : Stream
    {
        private const int Limit = 4000;
        private readonly Stream _inner;
        private readonly HttpResponse _response;
        private readonly MemoryStream _captured = new();

        public ErrorBodyCaptureStream(Stream inner, HttpResponse response)
        {
            _inner = inner;
            _response = response;
        }

        public string? CapturedText() => _captured.Length == 0 ? null : Encoding.UTF8.GetString(_captured.ToArray());

        private void Capture(ReadOnlySpan<byte> data)
        {
            if (_response.StatusCode < 400 || _captured.Length >= Limit) return;
            int take = (int)Math.Min(data.Length, Limit - _captured.Length);
            _captured.Write(data.Slice(0, take));
        }

        public override void Write(byte[] buffer, int offset, int count) { Capture(buffer.AsSpan(offset, count)); _inner.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { Capture(buffer); _inner.Write(buffer); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) { Capture(buffer.AsSpan(offset, count)); return _inner.WriteAsync(buffer, offset, count, ct); }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) { Capture(buffer.Span); return _inner.WriteAsync(buffer, ct); }
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken ct) => _inner.FlushAsync(ct);

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
