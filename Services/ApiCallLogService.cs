using System.Data;
using System.Threading.Channels;
using Microsoft.Data.SqlClient;

namespace StowellCoAPI.Services
{
    // NEW (2026-09-24) - API call + error log (dbo.ApiCallLog, see db\api-call-log-2026-09-24.sql).
    // One entry per API call from every user, plus errors reported by the browser app. Entries are
    // queued in memory and written to SQL Server in batches by ApiCallLogWriter on a background thread,
    // so logging never slows a request down and a logging failure can never break a request.
    public class ApiCallLogEntry
    {
        public DateTime LoggedAt { get; set; } = DateTime.Now;
        public DateTime LoggedAtUtc { get; set; } = DateTime.UtcNow;
        public string? UserName { get; set; }
        public string Source { get; set; } = "API";          // "API" or "Client"
        public string? HttpMethod { get; set; }
        public string? RequestPath { get; set; }
        public string? QueryString { get; set; }
        public int? StatusCode { get; set; }
        public int? DurationMs { get; set; }
        public bool IsError { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
        public string? TraceId { get; set; }
    }

    public class ApiCallLogService
    {
        // Bounded so a database outage can't grow memory without limit - the OLDEST entries are dropped first.
        private readonly Channel<ApiCallLogEntry> _channel = Channel.CreateBounded<ApiCallLogEntry>(
            new BoundedChannelOptions(20000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

        public ChannelReader<ApiCallLogEntry> Reader => _channel.Reader;

        public void Enqueue(ApiCallLogEntry entry) => _channel.Writer.TryWrite(entry);
    }

    public class ApiCallLogWriter : BackgroundService
    {
        private readonly ApiCallLogService _queue;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiCallLogWriter> _logger;

        public ApiCallLogWriter(ApiCallLogService queue, IConfiguration configuration, ILogger<ApiCallLogWriter> logger)
        {
            _queue = queue;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<ApiCallLogEntry>(1000);
            try
            {
                while (await _queue.Reader.WaitToReadAsync(stoppingToken))
                {
                    await Task.Delay(1000, stoppingToken);   // let a burst of calls (a page load) collect into one write
                    Drain(batch);
                    await FlushAsync(batch);
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }

            // Best-effort final flush on shutdown.
            Drain(batch);
            await FlushAsync(batch);
        }

        private void Drain(List<ApiCallLogEntry> batch)
        {
            while (batch.Count < 1000 && _queue.Reader.TryRead(out var e)) batch.Add(e);
        }

        private async Task FlushAsync(List<ApiCallLogEntry> batch)
        {
            if (batch.Count == 0) return;
            try
            {
                var table = new DataTable();
                string[] cols = { "LoggedAt", "LoggedAtUtc", "UserName", "Source", "HttpMethod", "RequestPath", "QueryString", "StatusCode",
                                  "DurationMs", "IsError", "ErrorType", "ErrorMessage", "Details", "ClientIp", "UserAgent", "TraceId" };
                foreach (var c in cols) table.Columns.Add(c, typeof(object));

                foreach (var e in batch)
                {
                    table.Rows.Add(e.LoggedAt, e.LoggedAtUtc, Cut(e.UserName, 256), Cut(e.Source, 10) ?? "API", Cut(e.HttpMethod, 10),
                        Cut(e.RequestPath, 500), Cut(e.QueryString, 1000), (object?)e.StatusCode ?? DBNull.Value, (object?)e.DurationMs ?? DBNull.Value,
                        e.IsError, Cut(e.ErrorType, 200), Cut(e.ErrorMessage, 4000), Cut(e.Details, 20000), Cut(e.ClientIp, 64),
                        Cut(e.UserAgent, 400), Cut(e.TraceId, 64));
                }

                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                await connection.OpenAsync();
                using var bulk = new SqlBulkCopy(connection) { DestinationTableName = "dbo.ApiCallLog", BulkCopyTimeout = 15 };
                foreach (var c in cols) bulk.ColumnMappings.Add(c, c);
                await bulk.WriteToServerAsync(table);
            }
            catch (Exception ex)
            {
                // Never let logging break the app - drop the batch and note it on the console.
                _logger.LogWarning(ex, "ApiCallLog: could not write {Count} log rows; dropped.", batch.Count);
            }
            finally
            {
                batch.Clear();
            }
        }

        private static object Cut(string? value, int max) =>
            value == null ? DBNull.Value : (value.Length <= max ? value : value.Substring(0, max));
    }
}
