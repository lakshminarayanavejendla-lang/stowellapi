using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
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
                SqlCommand command = new SqlCommand("CO_SubmitRequest", connection) { CommandType = CommandType.StoredProcedure };
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
                string coId = null;
                long coRecNum = 0;
                if (await reader.ReadAsync())
                {
                    coId = reader["CoId"].ToString();
                    coRecNum = Convert.ToInt64(reader["CoRecNum"]);
                }

                return Ok(new { submitted = true, coId, coRecNum });
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
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_UpdateCo", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@CoId", coId);
                command.Parameters.AddWithValue("@PurchaseOrderId", (object)request.PurchaseOrderId ?? DBNull.Value);
                command.Parameters.AddWithValue("@CoReason", (object)request.CoReason ?? DBNull.Value);
                // FIX (2026-09-14) - CO_UpdateCo's @Requester param feeds prmchg.usrnme directly
                // (no separate @CreatedBy param); sending request.CreatedBy (real authenticated
                // user) instead of request.Requester (free text) - see PoQueueController.UpdatePo's
                // matching comment.
                command.Parameters.AddWithValue("@Requester", (object)request.CreatedBy ?? DBNull.Value);
                command.Parameters.AddWithValue("@DateRequested", ParseDateOrNull(request.DateRequested));
                command.Parameters.AddWithValue("@RequiredOnSite", ParseDateOrNull(request.RequiredOnSite));
                command.Parameters.AddWithValue("@ItemsJson", JsonSerializer.Serialize(request.Items ?? new(), CamelCase));
                command.Parameters.AddWithValue("@BackChargesJson", JsonSerializer.Serialize(request.BackCharges ?? new(), CamelCase));

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return Ok(new { saved = true });
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
