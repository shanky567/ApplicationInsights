using System.Diagnostics;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace ApplicationInsights.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly TelemetryClient _telemetryClient;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger,
            TelemetryClient telemetryClient)
        {
            _next = next;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            string requestBody = await ReadRequestBodyAsync(context.Request);
            string path = context.Request.Path;
            string method = context.Request.Method;
            string queryString = context.Request.QueryString.ToString();
            string traceId = context.TraceIdentifier;

            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                _logger.LogInformation(
                    "Incoming request. TraceId: {TraceId}, Method: {Method}, Path: {Path}, Query: {Query}, Payload: {Payload}",
                    traceId, method, path, queryString, requestBody);

                await _next(context);

                stopwatch.Stop();

                string responseText = await ReadResponseBodyAsync(context.Response);
                int statusCode = context.Response.StatusCode;

                _logger.LogInformation(
                    "Outgoing response. TraceId: {TraceId}, Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, DurationMs: {DurationMs}, Response: {Response}",
                    traceId, method, path, statusCode, stopwatch.ElapsedMilliseconds, responseText);

                var traceTelemetry = new TraceTelemetry("Request/Response log", SeverityLevel.Information);
                traceTelemetry.Properties["TraceId"] = traceId;
                traceTelemetry.Properties["Method"] = method;
                traceTelemetry.Properties["Path"] = path;
                traceTelemetry.Properties["QueryString"] = queryString;
                traceTelemetry.Properties["RequestBody"] = Truncate(requestBody, 8000);
                traceTelemetry.Properties["ResponseBody"] = Truncate(responseText, 8000);
                traceTelemetry.Properties["StatusCode"] = statusCode.ToString();
                traceTelemetry.Properties["DurationMs"] = stopwatch.ElapsedMilliseconds.ToString();

                _telemetryClient.TrackTrace(traceTelemetry);

                await CopyResponseToOriginalStreamAsync(responseBody, originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "Unhandled exception. TraceId: {TraceId}, Method: {Method}, Path: {Path}, DurationMs: {DurationMs}, Payload: {Payload}",
                    traceId, method, path, stopwatch.ElapsedMilliseconds, requestBody);

                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    ["TraceId"] = traceId,
                    ["Method"] = method,
                    ["Path"] = path,
                    ["QueryString"] = queryString,
                    ["RequestBody"] = Truncate(requestBody, 8000),
                    ["DurationMs"] = stopwatch.ElapsedMilliseconds.ToString()
                });

                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();

            request.Body.Position = 0;

            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            string body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            return body;
        }

        private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
            string text = await reader.ReadToEndAsync();

            response.Body.Seek(0, SeekOrigin.Begin);

            return text;
        }

        private static async Task CopyResponseToOriginalStreamAsync(MemoryStream source, Stream destination)
        {
            source.Seek(0, SeekOrigin.Begin);
            await source.CopyToAsync(destination);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;

            return value.Length <= maxLength
                ? value
                : value[..maxLength] + "...(truncated)";
        }
    }
}