using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.Services;
using System.Data;

namespace StowellCoAPI.Controllers
{
    /// <summary>
    /// Accounting's side of "everything goes through approval" for changes to records already in Sage: a PM's edit of a
    /// PO or CO, or the deletion of a posted CO budget allocation, waits here until Accounting approves it (Sage is only
    /// changed on approval) or rejects it with a reason (Sage is never touched). See db/change-requests-2026-10-04.sql.
    /// </summary>
    [ApiController]
    public class ChangeRequestsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChangeRequestsController> _logger;

        public ChangeRequestsController(IConfiguration configuration, ILogger<ChangeRequestsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public class DecisionRequest
        {
            public int Id { get; set; }
            public string? DecidedBy { get; set; }
            public string? Reason { get; set; }
        }

        private SqlConnection Open() => new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));

        /// <summary>Change requests, Pending by default. Optionally for one PO, CO or allocation (to show "change waiting" on it).</summary>
        [HttpGet("api/ChangeRequests", Name = "GetChangeRequests")]
        public async Task<IActionResult> GetList([FromQuery] string? status = "Pending", [FromQuery] string? entity = null, [FromQuery] string? entityId = null)
        {
            try
            {
                using var connection = Open();
                using var command = new SqlCommand("dbo.Change_GetList", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(status) || status == "All" ? DBNull.Value : status);
                command.Parameters.AddWithValue("@Entity", (object?)entity ?? DBNull.Value);
                command.Parameters.AddWithValue("@EntityId", (object?)entityId ?? DBNull.Value);
                await connection.OpenAsync();

                var rows = new List<object>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string? Str(string col) => reader.IsDBNull(reader.GetOrdinal(col)) ? null : reader[col].ToString();
                    rows.Add(new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("Id")),
                        entity = Str("Entity"),
                        entityId = Str("EntityId"),
                        jobId = Str("JobId"),
                        jobName = Str("JobName"),
                        changeType = Str("ChangeType"),
                        summary = Str("Summary"),
                        payloadJson = Str("PayloadJson"),
                        beforeJson = Str("BeforeJson"),
                        status = Str("Status"),
                        requestedBy = Str("RequestedBy"),
                        requestedAt = reader.GetDateTime(reader.GetOrdinal("RequestedAt")),
                        decidedBy = Str("DecidedBy"),
                        decidedAt = reader.IsDBNull(reader.GetOrdinal("DecidedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DecidedAt")),
                        rejectReason = Str("RejectReason"),
                        applyError = Str("ApplyError")
                    });
                }
                return Ok(rows);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while loading the change requests.", Details = sqlEx.Message });
            }
        }

        /// <summary>Approve: Sage is changed now. If applying it fails, the request stays Pending and the error is returned.</summary>
        [HttpPost("api/ChangeRequests/Approve", Name = "ApproveChangeRequest")]
        public async Task<IActionResult> Approve([FromBody] DecisionRequest request)
        {
            if (request == null || request.Id <= 0) return BadRequest(new { Message = "A change request id is required." });
            try
            {
                using var connection = Open();
                using var command = new SqlCommand("dbo.Change_Approve", connection) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
                command.Parameters.AddWithValue("@Id", request.Id);
                command.Parameters.AddWithValue("@ApprovedBy", string.IsNullOrWhiteSpace(request.DecidedBy) ? "Unknown user" : request.DecidedBy);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return Ok(new { succeeded = true });
            }
            catch (SqlException sqlEx) when (ChangeRequestStore.IsUserFacing(sqlEx))
            {
                return Conflict(new { succeeded = false, Message = sqlEx.Message });
            }
            catch (SqlException sqlEx)
            {
                // the change itself failed (for example the vendor no longer exists in Sage); nothing was applied
                _logger.LogError(sqlEx, "Approving change request {Id} failed", request.Id);
                return StatusCode(500, new { succeeded = false, Message = "The change could not be applied to Sage, so it is still waiting.", Details = sqlEx.Message });
            }
        }

        /// <summary>Reject: Sage is left as it is. A reason is required.</summary>
        [HttpPost("api/ChangeRequests/Reject", Name = "RejectChangeRequest")]
        public async Task<IActionResult> Reject([FromBody] DecisionRequest request)
        {
            if (request == null || request.Id <= 0) return BadRequest(new { Message = "A change request id is required." });
            if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { Message = "A reason is required to reject a change." });
            try
            {
                using var connection = Open();
                using var command = new SqlCommand("dbo.Change_Reject", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@Id", request.Id);
                command.Parameters.AddWithValue("@RejectedBy", string.IsNullOrWhiteSpace(request.DecidedBy) ? "Unknown user" : request.DecidedBy);
                command.Parameters.AddWithValue("@Reason", request.Reason);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return Ok(new { succeeded = true });
            }
            catch (SqlException sqlEx) when (ChangeRequestStore.IsUserFacing(sqlEx))
            {
                return Conflict(new { succeeded = false, Message = sqlEx.Message });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Rejecting change request {Id} failed", request.Id);
                return StatusCode(500, new { succeeded = false, Message = "A database error occurred while rejecting the change.", Details = sqlEx.Message });
            }
        }
    }
}
