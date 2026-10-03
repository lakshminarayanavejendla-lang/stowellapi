using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
using StowellCoAPI.Services;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace StowellCoAPI.Controllers
{
    // NEW controller - Stowell PO Module (from the "Stowell PO Module" Moqups design spec, page
    // NAV-CO Queue Home). Mirrors PoQueueController.cs - see that file's header comment for the
    // shared design rationale (SAGESBQ entry point, cross-DB into StowellSandbox).
    [ApiController]
    public class CoQueueController : ControllerBase
    {
        private readonly ILogger<CoQueueController> _logger;
        private readonly IConfiguration _configuration;

        public CoQueueController(ILogger<CoQueueController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("api/CoQueue/GetCoQueueData", Name = "GetCoQueueData")]
        public async Task<IActionResult> GetCoQueueData()
        {
            var data = new CoQueueData();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand("CO_GetQueueData", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    await connection.OpenAsync();
                    using SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var record = new CoQueueRecord
                        {
                            MasterJobCoId = reader.IsDBNull(reader.GetOrdinal("MasterJobCoID")) ? string.Empty : reader["MasterJobCoID"].ToString(),
                            PoStatus = reader.IsDBNull(reader.GetOrdinal("PoStatus")) ? string.Empty : reader["PoStatus"].ToString(),
                            JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                            Pm = reader.IsDBNull(reader.GetOrdinal("PM")) ? string.Empty : reader["PM"].ToString(),
                            AllocationId = reader.IsDBNull(reader.GetOrdinal("AllocationId")) ? null : Convert.ToInt32(reader["AllocationId"])
                        };

                        if (record.PoStatus == "Closed")
                        {
                            data.CompletedCOs.Add(record);
                        }
                        else
                        {
                            data.ActiveCOs.Add(record);
                        }
                    }
                }

                return Ok(data);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the CO queue.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the CO queue.", Details = ex.Message });
            }
        }

        private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Save = safe, reversible, SAGESBQ-only draft (dbo.CoRequestDrafts/-Items). Mirrors
        // PoQueueController.SavePoRequest - see that action's comment for the reasoning.
        [HttpPost("api/CoQueue/SaveCoRequest", Name = "SaveCoRequest")]
        public async Task<IActionResult> SaveCoRequest([FromBody] CoRequestSubmissionDto request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_SaveDraft", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@JobId", long.TryParse(request.JobId, out var jobId) ? jobId : 0);
                command.Parameters.AddWithValue("@PurchaseOrderId", (object)request.PurchaseOrderId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Vendor", (object)request.Vendor ?? DBNull.Value);
                command.Parameters.AddWithValue("@CoReason", (object)request.CoReason ?? DBNull.Value);
                command.Parameters.AddWithValue("@Requester", (object)request.Requester ?? DBNull.Value);
                command.Parameters.AddWithValue("@DateRequested", ParseDateOrNull(request.DateRequested));
                command.Parameters.AddWithValue("@RequiredOnSite", ParseDateOrNull(request.RequiredOnSite));
                command.Parameters.AddWithValue("@TotalAmount", request.TotalCoAmount);
                // FIX (2026-09-14) - was request.Requester (free text); now the real authenticated
                // user (see PoQueueController.SavePoRequest's matching comment).
                command.Parameters.AddWithValue("@CreatedBy", (object)request.CreatedBy ?? DBNull.Value);
                command.Parameters.AddWithValue("@ItemsJson", JsonSerializer.Serialize(request.Items ?? new(), CamelCase));

                await connection.OpenAsync();
                var draftId = await command.ExecuteScalarAsync();

                return Ok(new { saved = true, draftId });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while saving the CO draft.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while saving the CO draft.", Details = ex.Message });
            }
        }

        // Submit = the one operation that creates a real, permanent, numbered Sage CO record
        // (StowellSandbox.dbo.prmchg/sbcgln). See CO_SubmitRequest in po_module_write_side.sql.
        [HttpPost("api/CoQueue/SubmitCoRequest", Name = "SubmitCoRequest")]
        public async Task<IActionResult> SubmitCoRequest([FromBody] CoRequestSubmissionDto request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                // Mike Smith, 2026-09-26: a submitted CO now waits for Accounting approval instead of being created in Sage straight away
                // (same as POs). CO_SubmitForApproval holds it (dbo.CoApprovalRequests); CO_ApproveRequest creates the Sage CO via
                // CO_SubmitRequest; CO_RejectRequest drops it from the queue. Same parameters as CO_SubmitRequest.
                SqlCommand command = new SqlCommand("CO_SubmitForApproval", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@JobId", long.TryParse(request.JobId, out var jobId) ? jobId : 0);
                command.Parameters.AddWithValue("@PurchaseOrderId", (object)request.PurchaseOrderId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Vendor", (object)request.Vendor ?? DBNull.Value);
                command.Parameters.AddWithValue("@CoReason", (object)request.CoReason ?? DBNull.Value);
                command.Parameters.AddWithValue("@Requester", (object)request.Requester ?? DBNull.Value);
                command.Parameters.AddWithValue("@DateRequested", ParseDateOrNull(request.DateRequested));
                command.Parameters.AddWithValue("@RequiredOnSite", ParseDateOrNull(request.RequiredOnSite));
                // FIX (2026-09-14) - was request.Requester; now the real authenticated user.
                command.Parameters.AddWithValue("@CreatedBy", (object)request.CreatedBy ?? DBNull.Value);
                command.Parameters.AddWithValue("@ItemsJson", JsonSerializer.Serialize(request.Items ?? new(), CamelCase));
                command.Parameters.AddWithValue("@BackChargesJson", JsonSerializer.Serialize(request.BackCharges ?? new(), CamelCase));

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                int requestId = 0;
                if (await reader.ReadAsync())
                {
                    requestId = Convert.ToInt32(reader["RequestId"]);
                }

                // coId stays empty until Accounting approves the request (that is when the Sage CO number is assigned)
                return Ok(new { submitted = true, requestId, status = "Pending Approval" });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while submitting the CO request.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while submitting the CO request.", Details = ex.Message });
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------
        // Accounting approval of submitted change orders (Mike Smith, 2026-09-26) - see db/co-approval-2026-09-26.sql. Approve -> the CO is
        // created in Sage; reject -> marked rejected and removed from the queue.
        // ------------------------------------------------------------------------------------------------------------------------

        /// <summary>COs waiting for Accounting approval (the Accounting CO Queue).</summary>
        [HttpGet("api/CoQueue/GetPendingCoApprovals", Name = "GetPendingCoApprovals")]
        public async Task<IActionResult> GetPendingCoApprovals()
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                using var command = new SqlCommand("CO_GetPendingApprovals", connection) { CommandType = CommandType.StoredProcedure };
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                var rows = new List<object>();
                while (await reader.ReadAsync())
                {
                    rows.Add(new
                    {
                        requestId = Convert.ToInt32(reader["RequestId"]),
                        jobId = reader["JobId"].ToString(),
                        jobName = reader["JobName"] == DBNull.Value ? "" : reader["JobName"].ToString(),
                        purchaseOrderId = reader["PurchaseOrderId"] == DBNull.Value ? "" : reader["PurchaseOrderId"].ToString(),
                        vendor = reader["Vendor"] == DBNull.Value ? "" : reader["Vendor"].ToString(),
                        coReason = reader["CoReason"].ToString(),
                        requester = reader["Requester"] == DBNull.Value ? "" : reader["Requester"].ToString(),
                        createdBy = reader["CreatedBy"] == DBNull.Value ? "" : reader["CreatedBy"].ToString(),
                        totalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        submittedDate = Convert.ToDateTime(reader["SubmittedDate"]).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture)
                    });
                }
                return Ok(rows);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the COs waiting for approval.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the COs waiting for approval.", Details = ex.Message });
            }
        }

        /// <summary>One request for the approval screen. {requestId} is the approval request id (shown as the CO id on the screen).</summary>
        [HttpGet("api/CoQueue/GetCoRequestForApproval/{requestId}", Name = "GetCoRequestForApproval")]
        public async Task<IActionResult> GetCoRequestForApproval(int requestId)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                using var command = new SqlCommand("CO_GetApprovalRequest", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@RequestId", requestId);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync()) return NotFound(new { Message = "CO request not found." });

                var coId = reader["RequestId"].ToString();
                var jobId = reader["JobId"].ToString();
                var jobName = reader["JobName"] == DBNull.Value ? "" : reader["JobName"].ToString();
                var purchaseOrderId = reader["PurchaseOrderId"] == DBNull.Value ? "" : reader["PurchaseOrderId"].ToString();
                var requester = reader["Requester"] == DBNull.Value ? "" : reader["Requester"].ToString();
                var vendor = reader["Vendor"] == DBNull.Value ? "" : reader["Vendor"].ToString();
                var coReason = reader["CoReason"].ToString();
                var dateRequested = reader["DateRequested"] == DBNull.Value ? "" : reader["DateRequested"].ToString();
                var requiredOnSite = reader["RequiredOnSite"] == DBNull.Value ? "" : reader["RequiredOnSite"].ToString();
                var totalCoAmount = Convert.ToDecimal(reader["TotalAmount"]);
                var status = reader["Status"].ToString();

                var items = new List<object>();
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        item = reader["Item"] == DBNull.Value ? "" : reader["Item"].ToString(),
                        remainingBudget = reader["RemainingBudget"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["RemainingBudget"]),
                        itemCost = reader["ItemCost"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["ItemCost"]),
                        itemNote = reader["ItemNote"] == DBNull.Value ? "" : reader["ItemNote"].ToString()
                    });
                }

                var backCharges = new List<object>();
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    backCharges.Add(new
                    {
                        vendor = reader["Vendor"] == DBNull.Value ? "" : reader["Vendor"].ToString(),
                        percentCharge = reader["PercentCharge"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["PercentCharge"]),
                        dollarCharge = Convert.ToDecimal(reader["DollarCharge"]),
                        reason = reader["Reason"] == DBNull.Value ? "" : reader["Reason"].ToString()
                    });
                }

                return Ok(new { coId, jobId, jobName, purchaseOrderId, requester, vendor, coReason, dateRequested, requiredOnSite, totalCoAmount, status, items, backCharges });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the CO request.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the CO request.", Details = ex.Message });
            }
        }

        public class CoApprovalActionDto
        {
            /// <summary>the approval request id (the id shown as the CO id on the approval screen)</summary>
            public string? CoId { get; set; }
            /// <summary>required when rejecting; not sent when approving</summary>
            public string? Reason { get; set; }
            /// <summary>the signed-in Accounting user who decided</summary>
            public string? DecidedBy { get; set; }
        }

        /// <summary>Approve: creates the CO in Sage and marks the request Approved.</summary>
        [HttpPost("api/CoQueue/ApproveCoRequest", Name = "ApproveCoRequest")]
        public async Task<IActionResult> ApproveCoRequest([FromBody] CoApprovalActionDto request)
        {
            if (request == null || !int.TryParse(request.CoId, out var requestId)) return BadRequest(new { Message = "Invalid CO request id." });
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                using var command = new SqlCommand("CO_ApproveRequest", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@RequestId", requestId);
                command.Parameters.AddWithValue("@ApprovedBy", (object)request.DecidedBy ?? DBNull.Value);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                string coId = null;
                if (await reader.ReadAsync()) coId = reader["CoId"].ToString();
                return Ok(new { succeeded = true, coId });
            }
            catch (SqlException sqlEx)
            {
                // business rules raised by the procedure (already decided ...) come back as readable messages
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(409, new { Message = sqlEx.Message, Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while approving the CO.", Details = ex.Message });
            }
        }

        /// <summary>Reject: marks the request Rejected (reason required) so it leaves the queue. Nothing is created in Sage.</summary>
        [HttpPost("api/CoQueue/RejectCoRequest", Name = "RejectCoRequest")]
        public async Task<IActionResult> RejectCoRequest([FromBody] CoApprovalActionDto request)
        {
            if (request == null || !int.TryParse(request.CoId, out var requestId)) return BadRequest(new { Message = "Invalid CO request id." });
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                using var command = new SqlCommand("CO_RejectRequest", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@RequestId", requestId);
                command.Parameters.AddWithValue("@RejectedBy", (object)request.DecidedBy ?? DBNull.Value);
                command.Parameters.AddWithValue("@Reason", (object)request.Reason ?? DBNull.Value);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return Ok(new { succeeded = true });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(409, new { Message = sqlEx.Message, Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while rejecting the CO.", Details = ex.Message });
            }
        }

        private static object ParseDateOrNull(string value)
        {
            return DateTime.TryParse(value, out var dt) ? dt.Date : DBNull.Value;
        }

        // Backs the "Draft Change Orders" section on CO Queue Home. Mirrors PoQueueController's
        // GetPoDrafts/GetPoDraft/DeletePoDraft.
        [HttpGet("api/CoQueue/GetCoDrafts", Name = "GetCoDrafts")]
        public async Task<IActionResult> GetCoDrafts()
        {
            var drafts = new List<CoDraftSummary>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_GetDrafts", connection) { CommandType = CommandType.StoredProcedure };

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    drafts.Add(new CoDraftSummary
                    {
                        DraftId = Convert.ToInt32(reader["DraftId"]),
                        JobId = reader["JobID"].ToString(),
                        PurchaseOrderId = reader.IsDBNull(reader.GetOrdinal("PurchaseOrderId")) ? string.Empty : reader["PurchaseOrderId"].ToString(),
                        Vendor = reader.IsDBNull(reader.GetOrdinal("Vendor")) ? string.Empty : reader["Vendor"].ToString(),
                        CoReason = reader.IsDBNull(reader.GetOrdinal("CoReason")) ? string.Empty : reader["CoReason"].ToString(),
                        Requester = reader.IsDBNull(reader.GetOrdinal("Requester")) ? string.Empty : reader["Requester"].ToString(),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? string.Empty : reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader["Status"].ToString()
                    });
                }

                return Ok(drafts);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving CO drafts.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving CO drafts.", Details = ex.Message });
            }
        }

        [HttpGet("api/CoQueue/GetCoDraft/{draftId}", Name = "GetCoDraft")]
        public async Task<IActionResult> GetCoDraft(int draftId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_GetDraftById", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@DraftId", draftId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                CoDraftDetail detail = null;
                if (await reader.ReadAsync())
                {
                    detail = new CoDraftDetail
                    {
                        DraftId = Convert.ToInt32(reader["DraftId"]),
                        JobId = reader["JobID"].ToString(),
                        PurchaseOrderId = reader.IsDBNull(reader.GetOrdinal("PurchaseOrderId")) ? string.Empty : reader["PurchaseOrderId"].ToString(),
                        Vendor = reader.IsDBNull(reader.GetOrdinal("Vendor")) ? string.Empty : reader["Vendor"].ToString(),
                        CoReason = reader.IsDBNull(reader.GetOrdinal("CoReason")) ? string.Empty : reader["CoReason"].ToString(),
                        Requester = reader.IsDBNull(reader.GetOrdinal("Requester")) ? string.Empty : reader["Requester"].ToString(),
                        DateRequested = reader.IsDBNull(reader.GetOrdinal("DateRequested")) ? string.Empty : Convert.ToDateTime(reader["DateRequested"]).ToString("yyyy-MM-dd"),
                        RequiredOnSite = reader.IsDBNull(reader.GetOrdinal("RequiredOnSite")) ? string.Empty : Convert.ToDateTime(reader["RequiredOnSite"]).ToString("yyyy-MM-dd"),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                    };
                }

                if (detail == null) return NotFound(new { Message = $"Draft {draftId} not found." });

                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    detail.Items.Add(new PoRequestItemDto
                    {
                        Item = reader.IsDBNull(reader.GetOrdinal("Item")) ? string.Empty : reader["Item"].ToString(),
                        RemainingBudget = Convert.ToDecimal(reader["RemainingBudget"]),
                        ItemCost = Convert.ToDecimal(reader["ItemCost"]),
                        ItemNote = reader.IsDBNull(reader.GetOrdinal("ItemNote")) ? string.Empty : reader["ItemNote"].ToString()
                    });
                }

                return Ok(detail);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the CO draft.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the CO draft.", Details = ex.Message });
            }
        }

        // Backs the edit-on-click flow for an already-submitted, real Sage CO (StowellSandbox.
        // dbo.prmchg/sbcgln) - distinct from GetCoDraft above, which reads the SAGESBQ-only draft
        // tables. Calls CO_GetCoById (2026-09-10 DB changes), which already exists in the local
        // dev database; this action just wires it up. Route uses a catch-all {*coId} because some
        // historical chgnum values contain a literal '/' (e.g. "10064-46/60") - see
        // co-request.service.ts's getCoById header comment, which already encodeURIComponent's
        // the id on the way out; a plain {coId} segment would 404 on those ids. Vendor always
        // comes back "" - CO_SubmitRequest/CO_UpdateCo have never had a header vendor column to
        // populate (see CO_GetCoById itself) - not fabricated here.
        [HttpGet("api/CoQueue/GetCoById/{*coId}", Name = "GetCoById")]
        public async Task<IActionResult> GetCoById(string coId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_GetCoById", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@CoId", coId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                CoDetail detail = null;
                if (await reader.ReadAsync())
                {
                    detail = new CoDetail
                    {
                        CoId = reader["CoId"].ToString(),
                        JobId = reader["JobId"].ToString(),
                        JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                        PurchaseOrderId = reader.IsDBNull(reader.GetOrdinal("PurchaseOrderId")) ? string.Empty : reader["PurchaseOrderId"].ToString(),
                        Requester = reader.IsDBNull(reader.GetOrdinal("Requester")) ? string.Empty : reader["Requester"].ToString(),
                        Vendor = reader.IsDBNull(reader.GetOrdinal("Vendor")) ? string.Empty : reader["Vendor"].ToString(),
                        CoReason = reader.IsDBNull(reader.GetOrdinal("CoReason")) ? string.Empty : reader["CoReason"].ToString(),
                        DateRequested = ParseSageDate(reader["DateRequested"]),
                        RequiredOnSite = ParseSageDate(reader["RequiredOnSite"]),
                        TotalCoAmount = Convert.ToDecimal(reader["TotalCoAmount"])
                    };
                }

                if (detail == null) return NotFound(new { Message = $"CO {coId} not found." });

                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    detail.Items.Add(new PoRequestItemDto
                    {
                        Item = reader.IsDBNull(reader.GetOrdinal("Item")) ? string.Empty : reader["Item"].ToString(),
                        RemainingBudget = Convert.ToDecimal(reader["RemainingBudget"]),
                        ItemCost = Convert.ToDecimal(reader["ItemCost"]),
                        ItemNote = reader.IsDBNull(reader.GetOrdinal("ItemNote")) ? string.Empty : reader["ItemNote"].ToString()
                    });
                }

                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    detail.BackCharges.Add(new PoBackChargeDto
                    {
                        Vendor = reader.IsDBNull(reader.GetOrdinal("Vendor")) ? string.Empty : reader["Vendor"].ToString(),
                        PercentCharge = reader.IsDBNull(reader.GetOrdinal("PercentCharge")) ? null : Convert.ToDecimal(reader["PercentCharge"]),
                        DollarCharge = Convert.ToDecimal(reader["DollarCharge"]),
                        Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? string.Empty : reader["Reason"].ToString()
                    });
                }

                return Ok(detail);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the CO.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the CO.", Details = ex.Message });
            }
        }

        // Re-saves an already-submitted, real Sage CO in place via CO_UpdateCo (2026-09-10 DB
        // changes) - full replace of its sbcgln lines and dbo.BackCharges rows. Same catch-all
        // route reasoning as GetCoById above. Job/CO number are not editable; the linked
        // PurchaseOrderId is (mirrors what CO_UpdateCo itself allows).
        [HttpPut("api/CoQueue/UpdateCo/{*coId}", Name = "UpdateCo")]
        public async Task<IActionResult> UpdateCo(string coId, [FromBody] CoRequestSubmissionDto request)
        {
            try
            {
                // CHANGE (2026-10-04, "all should be through approval"): this used to run CO_UpdateCo, which rewrote the
                // Sage change order (prmchg/sbcgln) on the spot. The edit is now saved as a Pending change request and Sage
                // is only changed when Accounting approves it (Change_Approve runs CO_UpdateCo with these same values).
                var requestId = await ChangeRequestStore.RequestAsync(
                    _configuration, ChangeRequestStore.Co, coId, "Edit",
                    JsonSerializer.Serialize(request, CamelCase), request.CreatedBy);

                return Ok(new { saved = true, pendingApproval = true, requestId });
            }
            catch (SqlException sqlEx) when (ChangeRequestStore.IsUserFacing(sqlEx))
            {
                return Conflict(new { Message = sqlEx.Message });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while updating the CO.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the CO.", Details = ex.Message });
            }
        }

        // Mirrors PoQueueController's ParseSageDate - CO_GetCoById also returns MM/DD/YYYY text.
        private static string ParseSageDate(object value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            return DateTime.TryParseExact(value.ToString(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt.ToString("yyyy-MM-dd")
                : string.Empty;
        }

        [HttpDelete("api/CoQueue/DeleteCoDraft/{draftId}", Name = "DeleteCoDraft")]
        public async Task<IActionResult> DeleteCoDraft(int draftId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_DeleteDraft", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@DraftId", draftId);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return Ok(new { deleted = true });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while deleting the CO draft.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the CO draft.", Details = ex.Message });
            }
        }
    }
}
