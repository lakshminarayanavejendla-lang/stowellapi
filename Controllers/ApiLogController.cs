using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace StowellCoAPI.Controllers
{
    // NEW (2026-09-24) - read side of the API call/error log (dbo.ApiCallLog, see db\api-call-log-2026-09-24.sql), backing the
    // "API Success & Errors" page under Stowell Admin. Filtering and paging happen in SQL so the page stays fast on a big log.
    // Every filter value is a SQL parameter (never concatenated).
    //
    // NOTE: like the rest of this API there is no authentication on these endpoints - the log shows user names, paths and error
    // text, so anyone who can reach the API can read it. Restrict at the network/IIS level or add real authentication.
    [ApiController]
    public class ApiLogController : ControllerBase
    {
        private readonly ILogger<ApiLogController> _logger;
        private readonly IConfiguration _configuration;

        public ApiLogController(ILogger<ApiLogController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // Totals for the period, the endpoints failing most, and per-user counts (also feeds the User filter).
        [HttpGet("api/ApiLog/Summary", Name = "GetApiLogSummary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? source)
        {
            try
            {
                var (start, end) = Range(from, to);
                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                await connection.OpenAsync();

                long total = 0, success = 0, errors = 0;
                double avgMs = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT_BIG(*), SUM(CASE WHEN IsError = 0 THEN 1 ELSE 0 END), SUM(CASE WHEN IsError = 1 THEN 1 ELSE 0 END), AVG(CAST(DurationMs AS float))
                    FROM dbo.ApiCallLog WHERE LoggedAt >= @from AND LoggedAt < @to AND (@source IS NULL OR Source = @source)", connection))
                {
                    AddRange(cmd, start, end, source);
                    using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        total = r.GetInt64(0);
                        success = r.IsDBNull(1) ? 0 : Convert.ToInt64(r[1]);
                        errors = r.IsDBNull(2) ? 0 : Convert.ToInt64(r[2]);
                        avgMs = r.IsDBNull(3) ? 0 : Convert.ToDouble(r[3]);
                    }
                }

                var topErrors = new List<object>();
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 8 RequestPath, COUNT_BIG(*) AS n, MAX(LoggedAt) AS lastAt
                    FROM dbo.ApiCallLog WHERE IsError = 1 AND LoggedAt >= @from AND LoggedAt < @to AND (@source IS NULL OR Source = @source)
                    GROUP BY RequestPath ORDER BY n DESC, lastAt DESC", connection))
                {
                    AddRange(cmd, start, end, source);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                        topErrors.Add(new { path = r["RequestPath"] as string, count = Convert.ToInt64(r["n"]), lastAt = (DateTime)r["lastAt"] });
                }

                var users = new List<object>();
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 100 UserName, COUNT_BIG(*) AS calls, SUM(CASE WHEN IsError = 1 THEN 1 ELSE 0 END) AS errs
                    FROM dbo.ApiCallLog WHERE LoggedAt >= @from AND LoggedAt < @to AND (@source IS NULL OR Source = @source)
                    GROUP BY UserName ORDER BY calls DESC", connection))
                {
                    AddRange(cmd, start, end, source);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                        users.Add(new { userName = r["UserName"] as string, calls = Convert.ToInt64(r["calls"]), errors = Convert.ToInt64(r["errs"]) });
                }

                return Ok(new { total, success, errors, avgMs = Math.Round(avgMs, 1), topErrors, users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An error occurred while reading the API log summary.", Details = ex.Message });
            }
        }

        // One page of calls, newest first. result: all | success | error. q searches the path and the error message.
        [HttpGet("api/ApiLog/Search", Name = "SearchApiLog")]
        public async Task<IActionResult> Search([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? result,
            [FromQuery] string? user, [FromQuery] string? q, [FromQuery] string? source, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var (start, end) = Range(from, to);
                pageSize = Math.Clamp(pageSize, 10, 200);
                page = Math.Max(page, 1);

                string where = "LoggedAt >= @from AND LoggedAt < @to AND (@source IS NULL OR Source = @source)";
                if (string.Equals(result, "success", StringComparison.OrdinalIgnoreCase)) where += " AND IsError = 0";
                else if (string.Equals(result, "error", StringComparison.OrdinalIgnoreCase)) where += " AND IsError = 1";
                if (!string.IsNullOrWhiteSpace(user)) where += " AND UserName LIKE @user";
                if (!string.IsNullOrWhiteSpace(q)) where += " AND (RequestPath LIKE @q OR ErrorMessage LIKE @q)";

                void Bind(SqlCommand c)
                {
                    AddRange(c, start, end, source);
                    if (!string.IsNullOrWhiteSpace(user)) c.Parameters.AddWithValue("@user", "%" + user.Trim() + "%");
                    if (!string.IsNullOrWhiteSpace(q)) c.Parameters.AddWithValue("@q", "%" + q.Trim() + "%");
                }

                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                await connection.OpenAsync();

                long total;
                using (var cmd = new SqlCommand($"SELECT COUNT_BIG(*) FROM dbo.ApiCallLog WHERE {where}", connection))
                {
                    Bind(cmd);
                    total = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                }

                var rows = new List<object>();
                using (var cmd = new SqlCommand($@"
                    SELECT Id, LoggedAt, UserName, Source, HttpMethod, RequestPath, QueryString, StatusCode, DurationMs, IsError, ErrorType, ErrorMessage
                    FROM dbo.ApiCallLog WHERE {where}
                    ORDER BY LoggedAt DESC, Id DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY", connection))
                {
                    Bind(cmd);
                    cmd.Parameters.AddWithValue("@skip", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@take", pageSize);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        rows.Add(new
                        {
                            id = Convert.ToInt64(r["Id"]),
                            loggedAt = (DateTime)r["LoggedAt"],
                            userName = r["UserName"] as string,
                            source = r["Source"] as string,
                            method = r["HttpMethod"] as string,
                            path = r["RequestPath"] as string,
                            query = r["QueryString"] as string,
                            statusCode = r["StatusCode"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["StatusCode"]),
                            durationMs = r["DurationMs"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["DurationMs"]),
                            isError = (bool)r["IsError"],
                            errorType = r["ErrorType"] as string,
                            errorMessage = r["ErrorMessage"] as string
                        });
                    }
                }

                return Ok(new { total, page, pageSize, rows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An error occurred while searching the API log.", Details = ex.Message });
            }
        }

        // The (potentially long) stack trace / error response for one row - fetched on demand when a row is opened.
        [HttpGet("api/ApiLog/Detail/{id}", Name = "GetApiLogDetail")]
        public async Task<IActionResult> GetDetail(long id)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                await connection.OpenAsync();
                using var cmd = new SqlCommand("SELECT Details, ClientIp, UserAgent, TraceId FROM dbo.ApiCallLog WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", id);
                using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return NotFound();
                return Ok(new
                {
                    details = r["Details"] as string,
                    clientIp = r["ClientIp"] as string,
                    userAgent = r["UserAgent"] as string,
                    traceId = r["TraceId"] as string
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An error occurred while reading the API log entry.", Details = ex.Message });
            }
        }

        // Default window: the last 24 hours. `to` is inclusive of the whole day the caller picked.
        private static (DateTime start, DateTime end) Range(DateTime? from, DateTime? to)
        {
            DateTime end = to ?? DateTime.Now.AddMinutes(1);
            DateTime start = from ?? end.AddHours(-24);
            return (start, end);
        }

        private static void AddRange(SqlCommand cmd, DateTime start, DateTime end, string? source)
        {
            cmd.Parameters.AddWithValue("@from", start);
            cmd.Parameters.AddWithValue("@to", end);
            cmd.Parameters.AddWithValue("@source", string.IsNullOrWhiteSpace(source) ? DBNull.Value : source);
        }
    }
}
