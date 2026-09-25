using Microsoft.AspNetCore.Mvc;
using StowellCoAPI.Middleware;
using StowellCoAPI.Services;

namespace StowellCoAPI.Controllers
{
    // NEW (2026-09-24) - lets the Angular app report errors the API can't see on its own (a JavaScript error, or a call that never
    // got a response - network down, timeout, blocked) into the same dbo.ApiCallLog table (Source = 'Client'). Failed API calls
    // that DID reach the server are already recorded by ApiCallLoggingMiddleware, so the app only reports the rest.
    [ApiController]
    public class ClientLogController : ControllerBase
    {
        private readonly ApiCallLogService _log;

        public ClientLogController(ApiCallLogService log)
        {
            _log = log;
        }

        [HttpPost("api/ClientLog/Error", Name = "LogClientError")]
        public IActionResult LogClientError([FromBody] ClientErrorDto dto)
        {
            _log.Enqueue(new ApiCallLogEntry
            {
                UserName = !string.IsNullOrWhiteSpace(dto.UserName) ? dto.UserName.Trim() : ApiCallLoggingMiddleware.ResolveUser(HttpContext),
                Source = "Client",
                HttpMethod = dto.Method,
                RequestPath = dto.Url,
                StatusCode = dto.StatusCode,
                IsError = true,
                ErrorType = string.IsNullOrWhiteSpace(dto.ErrorType) ? "ClientError" : dto.ErrorType,
                ErrorMessage = dto.Message,
                Details = dto.Details,
                ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                TraceId = HttpContext.TraceIdentifier
            });
            return Ok();
        }
    }

    public class ClientErrorDto
    {
        public string? UserName { get; set; }
        public string? Url { get; set; }          // the page or the API url involved
        public string? Method { get; set; }
        public int? StatusCode { get; set; }      // 0 / null when there was no response
        public string? ErrorType { get; set; }
        public string? Message { get; set; }
        public string? Details { get; set; }      // stack trace, if any
    }
}
