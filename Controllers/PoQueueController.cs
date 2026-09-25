using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace StowellCoAPI.Controllers
{
    // NEW controller - Stowell PO Module (from the "Stowell PO Module" Moqups design spec).
    // Reads go through SAGESBQ (SageSBQConnection), which cross-database-joins into
    // StowellSandbox for the real Sage PO data (pchord/pcorln) - see PO_GetQueueData/
    // PO_GetJobOverview in po_module_read_side.sql. Matches the routes already wired into the
    // Angular app's project-management-endpoints.ts (GetPoQueueDataUrl/GetJobPoOverviewUrl).
    [ApiController]
    public class PoQueueController : ControllerBase
    {
        private readonly ILogger<PoQueueController> _logger;
        private readonly IConfiguration _configuration;

        public PoQueueController(ILogger<PoQueueController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("api/PoQueue/GetPoQueueData", Name = "GetPoQueueData")]
        public async Task<IActionResult> GetPoQueueData()
        {
            var data = new PoQueueData();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand("PO_GetQueueData", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    await connection.OpenAsync();
                    using SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var record = new PoQueueRecord
                        {
                            MasterJobPoId = reader.IsDBNull(reader.GetOrdinal("MasterJobPoID")) ? string.Empty : reader["MasterJobPoID"].ToString(),
                            PoStatus = reader.IsDBNull(reader.GetOrdinal("PoStatus")) ? string.Empty : reader["PoStatus"].ToString(),
                            JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                            Pm = reader.IsDBNull(reader.GetOrdinal("PM")) ? string.Empty : reader["PM"].ToString()
                        };

                        if (record.PoStatus == "Closed")
                        {
                            data.CompletedPOs.Add(record);
                        }
                        else
                        {
                            data.ActivePOs.Add(record);
                        }
                    }
                }

                return Ok(data);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the PO queue.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the PO queue.", Details = ex.Message });
            }
        }

        [HttpGet("api/PoQueue/GetJobPoOverview/{jobId}", Name = "GetJobPoOverview")]
        public async Task<IActionResult> GetJobPoOverview(long jobId)
        {
            var data = new JobPoOverviewData();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand("PO_GetJobOverview", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    command.Parameters.AddWithValue("@JobId", jobId);

                    await connection.OpenAsync();
                    using SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        data.PurchaseOrders.Add(new PurchaseOrderRecord
                        {
                            PoId = reader.IsDBNull(reader.GetOrdinal("PoId")) ? string.Empty : reader["PoId"].ToString(),
                            PoDescription = reader.IsDBNull(reader.GetOrdinal("PoDescription")) ? string.Empty : reader["PoDescription"].ToString(),
                            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader["Status"].ToString(),
                            DateSubmitted = reader.IsDBNull(reader.GetOrdinal("DateSubmitted")) ? string.Empty : reader["DateSubmitted"].ToString(),
                            RequiredOnsite = reader.IsDBNull(reader.GetOrdinal("RequiredOnsite")) ? string.Empty : reader["RequiredOnsite"].ToString(),
                            Vendor = reader.IsDBNull(reader.GetOrdinal("Vendor")) ? string.Empty : reader["Vendor"].ToString(),
                            PoPhase = reader.HasColumn("PoPhase") && !reader.IsDBNull(reader.GetOrdinal("PoPhase")) ? Convert.ToInt32(reader["PoPhase"]) : (int?)null
                        });
                    }

                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        data.ChangeOrderLog.Add(new ChangeOrderLogRecord
                        {
                            CoId = reader.IsDBNull(reader.GetOrdinal("CoId")) ? string.Empty : reader["CoId"].ToString(),
                            CoDescription = reader.IsDBNull(reader.GetOrdinal("CoDescription")) ? string.Empty : reader["CoDescription"].ToString(),
                            CoAmount = reader.IsDBNull(reader.GetOrdinal("CoAmount")) ? 0 : Convert.ToDecimal(reader["CoAmount"]),
                            CoStatus = reader.IsDBNull(reader.GetOrdinal("CoStatus")) ? string.Empty : reader["CoStatus"].ToString(),
                            CoReason = reader.IsDBNull(reader.GetOrdinal("CoReason")) ? string.Empty : reader["CoReason"].ToString(),
                            CoPhase = reader.HasColumn("CoPhase") && !reader.IsDBNull(reader.GetOrdinal("CoPhase")) ? Convert.ToInt32(reader["CoPhase"]) : (int?)null
                        });
                    }

                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        data.BackChargeLog.Add(new BackChargeLogRecord
                        {
                            BcId = reader.IsDBNull(reader.GetOrdinal("BcId")) ? string.Empty : reader["BcId"].ToString(),
                            BcDescription = reader.IsDBNull(reader.GetOrdinal("BcDescription")) ? string.Empty : reader["BcDescription"].ToString(),
                            BcAmount = reader.IsDBNull(reader.GetOrdinal("BcAmount")) ? 0 : Convert.ToDecimal(reader["BcAmount"]),
                            BcStatus = reader.IsDBNull(reader.GetOrdinal("BcStatus")) ? string.Empty : reader["BcStatus"].ToString(),
                            BcReason = reader.IsDBNull(reader.GetOrdinal("BcReason")) ? string.Empty : reader["BcReason"].ToString()
                        });
                    }
                }

                return Ok(data);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the job PO overview.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the job PO overview.", Details = ex.Message });
            }
        }

        private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Save = safe, reversible, SAGESBQ-only draft (dbo.PoRequestDrafts/-Items). Does NOT
        // touch the real Sage pchord/pcorln tables. See po_module_write_side.sql header comment
        // for the reasoning (don't create permanent numbered accounting records for an unfinished
        // form).
        [HttpPost("api/PoQueue/SavePoRequest", Name = "SavePoRequest")]
        public async Task<IActionResult> SavePoRequest([FromBody] PoRequestSubmissionDto request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_SaveDraft", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@JobId", long.TryParse(request.JobId, out var jobId) ? jobId : 0);
                command.Parameters.AddWithValue("@Vendor", (object)request.Vendor ?? DBNull.Value);
                command.Parameters.AddWithValue("@PoDescription", (object)request.PoDescription ?? DBNull.Value);
                command.Parameters.AddWithValue("@Requester", (object)request.Requester ?? DBNull.Value);
                command.Parameters.AddWithValue("@DateRequested", ParseDateOrNull(request.DateRequested));
                command.Parameters.AddWithValue("@RequiredOnSite", ParseDateOrNull(request.RequiredOnSite));
                command.Parameters.AddWithValue("@TotalAmount", request.TotalPoAmount);
                // FIX (2026-09-14) - was request.Requester (free text the user typed, not
                // necessarily who's actually logged in); now the real authenticated user.
                command.Parameters.AddWithValue("@CreatedBy", (object)request.CreatedBy ?? DBNull.Value);
                command.Parameters.AddWithValue("@ItemsJson", JsonSerializer.Serialize(request.Items ?? new(), CamelCase));
                command.Parameters.AddWithValue("@PhaseNumber", (object)request.PhaseNumber ?? DBNull.Value);

                await connection.OpenAsync();
                var draftId = await command.ExecuteScalarAsync();

                return Ok(new { saved = true, draftId });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while saving the PO draft.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while saving the PO draft.", Details = ex.Message });
            }
        }

        // Submit = the one operation that creates a real, permanent, numbered Sage PO record
        // (StowellSandbox.dbo.pchord/pcorln). See PO_SubmitRequest in po_module_write_side.sql.
        [HttpPost("api/PoQueue/SubmitPoRequest", Name = "SubmitPoRequest")]
        public async Task<IActionResult> SubmitPoRequest([FromBody] PoRequestSubmissionDto request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_SubmitRequest", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@JobId", long.TryParse(request.JobId, out var jobId) ? jobId : 0);
                command.Parameters.AddWithValue("@Vendor", (object)request.Vendor ?? DBNull.Value);
                command.Parameters.AddWithValue("@PoDescription", (object)request.PoDescription ?? DBNull.Value);
                command.Parameters.AddWithValue("@Requester", (object)request.Requester ?? DBNull.Value);
                command.Parameters.AddWithValue("@DateRequested", ParseDateOrNull(request.DateRequested));
                command.Parameters.AddWithValue("@RequiredOnSite", ParseDateOrNull(request.RequiredOnSite));
                // FIX (2026-09-14) - was request.Requester; now the real authenticated user (see
                // SavePoRequest's matching comment).
                command.Parameters.AddWithValue("@CreatedBy", (object)request.CreatedBy ?? DBNull.Value);
                command.Parameters.AddWithValue("@ItemsJson", JsonSerializer.Serialize(request.Items ?? new(), CamelCase));
                command.Parameters.AddWithValue("@BackChargesJson", JsonSerializer.Serialize(request.BackCharges ?? new(), CamelCase));
                // NEW (2026-09-23): the PO's phase -> pchord.phsnum (Moqups: "User must select Project Phase").
                command.Parameters.AddWithValue("@PhaseNumber", (object)request.PhaseNumber ?? DBNull.Value);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                string poId = null;
                long poRecNum = 0;
                if (await reader.ReadAsync())
                {
                    poId = reader["PoId"].ToString();
                    poRecNum = Convert.ToInt64(reader["PoRecNum"]);
                }

                return Ok(new { submitted = true, poId, poRecNum });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while submitting the PO request.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while submitting the PO request.", Details = ex.Message });
            }
        }

        private static object ParseDateOrNull(string value)
        {
            return DateTime.TryParse(value, out var dt) ? dt.Date : DBNull.Value;
        }

        // Backs the vendor picker on both FORM-PO Request and FORM-CO Request (see VendorOption
        // header comment). Reads directly from StowellSandbox - a plain SELECT, no new proc
        // needed since there's no cross-DB join or write involved.
        [HttpGet("api/PoQueue/GetVendors", Name = "GetVendors")]
        public async Task<IActionResult> GetVendors()
        {
            var vendors = new List<VendorOption>();
            try
            {
                string connectionString = _configuration.GetConnectionString("StowellConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("SELECT recnum, vndnme FROM dbo.actpay ORDER BY vndnme", connection);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vendors.Add(new VendorOption
                    {
                        Recnum = Convert.ToInt64(reader["recnum"]),
                        VendorName = reader.IsDBNull(reader.GetOrdinal("vndnme")) ? string.Empty : reader["vndnme"].ToString()
                    });
                }

                return Ok(vendors);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving vendors.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving vendors.", Details = ex.Message });
            }
        }

        // Backs the job-scoped cost code picker on both FORM-PO Request and FORM-CO Request (see
        // JobCostCodeOption header comment).
        [HttpGet("api/PoQueue/GetJobCostCodes/{jobId}", Name = "GetJobCostCodes")]
        public async Task<IActionResult> GetJobCostCodes(long jobId)
        {
            var codes = new List<JobCostCodeOption>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_GetJobCostCodes", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@JobId", jobId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    codes.Add(new JobCostCodeOption
                    {
                        CostCode = Convert.ToDecimal(reader["CostCode"]),
                        CostCodeDescription = reader.IsDBNull(reader.GetOrdinal("CostCodeDescription")) ? string.Empty : reader["CostCodeDescription"].ToString(),
                        Budget = Convert.ToDecimal(reader["Budget"]),
                        ToDate = Convert.ToDecimal(reader["ToDate"]),
                        Remaining = Convert.ToDecimal(reader["Remaining"])
                    });
                }

                return Ok(codes);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving cost codes.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving cost codes.", Details = ex.Message });
            }
        }

        // Backs the Phase dropdown on FORM-CO Reallocation (Change Order Budget Allocation) -
        // scoped to the selected job's real Sage phases (see JobPhaseOption header comment).
        // Lives here (not CoBudgetAllocationController) for the same reuse reason as
        // GetJobCostCodes above - shared PO/CO job-scoped picker data.
        [HttpGet("api/PoQueue/GetJobPhases/{jobId}", Name = "GetJobPhases")]
        public async Task<IActionResult> GetJobPhases(long jobId)
        {
            var phases = new List<JobPhaseOption>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_GetJobPhases", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@JobId", jobId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    phases.Add(new JobPhaseOption
                    {
                        PhaseNumber = Convert.ToInt32(reader["PhaseNumber"]),
                        PhaseName = reader.IsDBNull(reader.GetOrdinal("PhaseName")) ? string.Empty : reader["PhaseName"].ToString()
                    });
                }

                return Ok(phases);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving job phases.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving job phases.", Details = ex.Message });
            }
        }

        // Backs the "Draft Purchase Orders" section on PO Queue Home - closes the gap where
        // Save PO had no way to browse/resume/discard what got saved.
        [HttpGet("api/PoQueue/GetPoDrafts", Name = "GetPoDrafts")]
        public async Task<IActionResult> GetPoDrafts()
        {
            var drafts = new List<PoDraftSummary>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_GetDrafts", connection) { CommandType = CommandType.StoredProcedure };

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    drafts.Add(new PoDraftSummary
                    {
                        DraftId = Convert.ToInt32(reader["DraftId"]),
                        JobId = reader["JobID"].ToString(),
                        Vendor = reader.IsDBNull(reader.GetOrdinal("Vendor")) ? string.Empty : reader["Vendor"].ToString(),
                        PoDescription = reader.IsDBNull(reader.GetOrdinal("PoDescription")) ? string.Empty : reader["PoDescription"].ToString(),
                        Requester = reader.IsDBNull(reader.GetOrdinal("Requester")) ? string.Empty : reader["Requester"].ToString(),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? string.Empty : reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader["Status"].ToString(),
                        PhaseNumber = reader.HasColumn("PhaseNumber") && !reader.IsDBNull(reader.GetOrdinal("PhaseNumber")) ? Convert.ToInt32(reader["PhaseNumber"]) : (int?)null
                    });
                }

                return Ok(drafts);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving PO drafts.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving PO drafts.", Details = ex.Message });
            }
        }

        [HttpGet("api/PoQueue/GetPoDraft/{draftId}", Name = "GetPoDraft")]
        public async Task<IActionResult> GetPoDraft(int draftId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_GetDraftById", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@DraftId", draftId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                PoDraftDetail detail = null;
                if (await reader.ReadAsync())
                {
                    detail = new PoDraftDetail
                    {
                        DraftId = Convert.ToInt32(reader["DraftId"]),
                        JobId = reader["JobID"].ToString(),
                        Vendor = reader.IsDBNull(reader.GetOrdinal("Vendor")) ? string.Empty : reader["Vendor"].ToString(),
                        PoDescription = reader.IsDBNull(reader.GetOrdinal("PoDescription")) ? string.Empty : reader["PoDescription"].ToString(),
                        Requester = reader.IsDBNull(reader.GetOrdinal("Requester")) ? string.Empty : reader["Requester"].ToString(),
                        DateRequested = reader.IsDBNull(reader.GetOrdinal("DateRequested")) ? string.Empty : Convert.ToDateTime(reader["DateRequested"]).ToString("yyyy-MM-dd"),
                        RequiredOnSite = reader.IsDBNull(reader.GetOrdinal("RequiredOnSite")) ? string.Empty : Convert.ToDateTime(reader["RequiredOnSite"]).ToString("yyyy-MM-dd"),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        PhaseNumber = reader.HasColumn("PhaseNumber") && !reader.IsDBNull(reader.GetOrdinal("PhaseNumber")) ? Convert.ToInt32(reader["PhaseNumber"]) : (int?)null
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
                return StatusCode(500, new { Message = "A database error occurred while retrieving the PO draft.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the PO draft.", Details = ex.Message });
            }
        }

        // Backs the edit-on-click flow for an already-submitted, real Sage PO (StowellSandbox.
        // dbo.pchord/pcorln) - distinct from GetPoDraft above, which reads the SAGESBQ-only draft
        // tables. Calls PO_GetPoById (2026-09-10 DB changes), which already exists in the local
        // dev database; this action just wires it up. Dates come back from Sage as MM/DD/YYYY
        // text, reformatted here to yyyy-MM-dd for the Angular <input type="date"> fields.
        [HttpGet("api/PoQueue/GetPoById/{poId}", Name = "GetPoById")]
        public async Task<IActionResult> GetPoById(string poId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_GetPoById", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@PoId", poId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                PoDetail detail = null;
                if (await reader.ReadAsync())
                {
                    detail = new PoDetail
                    {
                        PoId = reader["PoId"].ToString(),
                        JobId = reader["JobId"].ToString(),
                        JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                        Requester = reader.IsDBNull(reader.GetOrdinal("Requester")) ? string.Empty : reader["Requester"].ToString(),
                        Vendor = reader.IsDBNull(reader.GetOrdinal("Vendor")) ? string.Empty : reader["Vendor"].ToString(),
                        PoDescription = reader.IsDBNull(reader.GetOrdinal("PoDescription")) ? string.Empty : reader["PoDescription"].ToString(),
                        DateRequested = ParseSageDate(reader["DateRequested"]),
                        RequiredOnSite = ParseSageDate(reader["RequiredOnSite"]),
                        TotalPoAmount = Convert.ToDecimal(reader["TotalPoAmount"]),
                        PhaseNumber = reader.HasColumn("PhaseNumber") && !reader.IsDBNull(reader.GetOrdinal("PhaseNumber")) ? Convert.ToInt32(reader["PhaseNumber"]) : (int?)null
                    };
                }

                if (detail == null) return NotFound(new { Message = $"PO {poId} not found." });

                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    detail.Items.Add(new PoRequestItemDto
                    {
                        Item = reader.IsDBNull(reader.GetOrdinal("Item")) ? string.Empty : reader["Item"].ToString(),
                        RemainingBudget = Convert.ToDecimal(reader["RemainingBudget"]),
                        ItemCost = Convert.ToDecimal(reader["ItemCost"]),
                        ItemNote = reader.IsDBNull(reader.GetOrdinal("ItemNote")) ? string.Empty : reader["ItemNote"].ToString(),
                        CostCode = reader.IsDBNull(reader.GetOrdinal("CostCode")) ? null : Convert.ToDecimal(reader["CostCode"])
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
                return StatusCode(500, new { Message = "A database error occurred while retrieving the PO.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the PO.", Details = ex.Message });
            }
        }

        // Re-saves an already-submitted, real Sage PO in place via PO_UpdatePo (2026-09-10 DB
        // changes) - full replace of its pcorln lines and dbo.BackCharges rows, same vendor-must-
        // exist validation as SubmitPoRequest. Job/PO number themselves are not editable.
        [HttpPut("api/PoQueue/UpdatePo/{poId}", Name = "UpdatePo")]
        public async Task<IActionResult> UpdatePo(string poId, [FromBody] PoRequestSubmissionDto request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_UpdatePo", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@PoId", poId);
                command.Parameters.AddWithValue("@Vendor", (object)request.Vendor ?? DBNull.Value);
                command.Parameters.AddWithValue("@PoDescription", (object)request.PoDescription ?? DBNull.Value);
                // FIX (2026-09-14) - PO_UpdatePo's @Requester param feeds pchord.usrnme directly
                // (no separate @CreatedBy param exists on this proc); sending request.CreatedBy
                // (the real authenticated user) here instead of request.Requester (free text) so
                // usrnme reflects who actually made the edit, matching Save/SubmitPoRequest's fix.
                command.Parameters.AddWithValue("@Requester", (object)request.CreatedBy ?? DBNull.Value);
                command.Parameters.AddWithValue("@DateRequested", ParseDateOrNull(request.DateRequested));
                command.Parameters.AddWithValue("@RequiredOnSite", ParseDateOrNull(request.RequiredOnSite));
                command.Parameters.AddWithValue("@ItemsJson", JsonSerializer.Serialize(request.Items ?? new(), CamelCase));
                command.Parameters.AddWithValue("@BackChargesJson", JsonSerializer.Serialize(request.BackCharges ?? new(), CamelCase));
                command.Parameters.AddWithValue("@PhaseNumber", (object)request.PhaseNumber ?? DBNull.Value);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return Ok(new { saved = true });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while updating the PO.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the PO.", Details = ex.Message });
            }
        }

        // Sage's PO_GetPoById returns dates pre-formatted as MM/DD/YYYY text (CONVERT(..., 101));
        // reformat to yyyy-MM-dd for the Angular <input type="date"> fields, same convention
        // GetPoDraft already uses for its (raw `date`-typed) columns.
        private static string ParseSageDate(object value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            return DateTime.TryParseExact(value.ToString(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt.ToString("yyyy-MM-dd")
                : string.Empty;
        }

        [HttpDelete("api/PoQueue/DeletePoDraft/{draftId}", Name = "DeletePoDraft")]
        public async Task<IActionResult> DeletePoDraft(int draftId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("PO_DeleteDraft", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@DraftId", draftId);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return Ok(new { deleted = true });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while deleting the PO draft.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the PO draft.", Details = ex.Message });
            }
        }
    }
}
