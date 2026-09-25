using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
using System.Data;
using System.Text.Json;

namespace StowellCoAPI.Controllers
{
    // Stowell PO/CO Module - Change Order Budget Allocation (Moqups page FORM-CO Reallocation,
    // link shared 2026-09-13). Save/Submit write to the app-owned dbo.CoBudgetAllocations/-Phases/
    // -Lines tables (SAGESBQ) via CO_SaveBudgetReallocation/CO_SubmitBudgetReallocation - this
    // deliberately does not post against real Sage budget/CO records (StowellSandbox's prmchg/
    // bdglin) yet, same as co-budget-allocation.service.ts's header comment already documents.
    [ApiController]
    public class CoBudgetAllocationController : ControllerBase
    {
        private readonly ILogger<CoBudgetAllocationController> _logger;
        private readonly IConfiguration _configuration;
        private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public CoBudgetAllocationController(ILogger<CoBudgetAllocationController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("api/CoBudgetAllocation/Save", Name = "SaveCoBudgetAllocation")]
        public async Task<IActionResult> Save([FromBody] CoBudgetAllocationRequestDto request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_SaveBudgetReallocation", connection) { CommandType = CommandType.StoredProcedure };
                AddCommonParameters(command, request);

                await connection.OpenAsync();
                var allocationId = await command.ExecuteScalarAsync();

                return Ok(new { saved = true, allocationId });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while saving the CO budget allocation.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while saving the CO budget allocation.", Details = ex.Message });
            }
        }

        [HttpPost("api/CoBudgetAllocation/Submit", Name = "SubmitCoBudgetAllocation")]
        public async Task<IActionResult> Submit([FromBody] CoBudgetAllocationRequestDto request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_SubmitBudgetReallocation", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@CoNumber", string.IsNullOrWhiteSpace(request.CoNumber) ? (object)DBNull.Value : request.CoNumber);
                command.Parameters.AddWithValue("@ProfitOnSell", request.ProfitOnSell);
                AddCommonParameters(command, request);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                string coNumber = null;
                int allocationId = 0;
                if (await reader.ReadAsync())
                {
                    coNumber = reader["CoNumber"].ToString();
                    allocationId = Convert.ToInt32(reader["AllocationId"]);
                }

                return Ok(new { submitted = true, coNumber, allocationId });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while submitting the CO budget allocation.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while submitting the CO budget allocation.", Details = ex.Message });
            }
        }

        // Live preview of the CO Number Submit would auto-assign right now (Prime="BC"+year+seq /
        // Internal="CO"+year+seq) - see CO_PeekNextBudgetReallocationNumber. Read-only, no lock,
        // not a reservation: the real number is still (re)computed under lock at actual Submit
        // time, so this can never cause a duplicate CoNumber even if two users preview at once.
        [HttpGet("api/CoBudgetAllocation/NextNumber", Name = "GetNextCoBudgetAllocationNumber")]
        public async Task<IActionResult> GetNextNumber([FromQuery] string coType)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_PeekNextBudgetReallocationNumber", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@CoType", (object)coType ?? DBNull.Value);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();

                return Ok(new { coNumber = result?.ToString() });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while previewing the next CO number.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while previewing the next CO number.", Details = ex.Message });
            }
        }

        // NEW (2026-09-20, Mike Smith - "these dropdowns should be from stowellsandbox") - backs the CO
        // Reason / CO Status dropdowns instead of the hard-coded lists that used to live in the Angular
        // component. Both lists come straight from StowellSandbox (Sage):
        //   statuses = chgtyp.typnme   (Sage's own change order type/status list: REQUEST, DIRECTIVE,
        //                               NO COST, APPROVED, BUDGET CHANGE)
        //   reasons  = distinct, non-blank prmchg.reason values already used on real change orders
        //              (Sage has no separate reason lookup table - reason is free text on prmchg).
        [HttpGet("api/CoBudgetAllocation/Lookups", Name = "GetCoBudgetAllocationLookups")]
        public async Task<IActionResult> GetLookups()
        {
            var reasons = new List<string>();
            var statuses = new List<string>();
            try
            {
                string connectionString = _configuration.GetConnectionString("StowellSandboxConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using (SqlCommand statusCmd = new SqlCommand(
                    "SELECT LTRIM(RTRIM(typnme)) AS Name FROM dbo.chgtyp WHERE typnme IS NOT NULL AND LTRIM(RTRIM(typnme)) <> '' ORDER BY recnum", connection))
                using (SqlDataReader reader = await statusCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) statuses.Add(reader["Name"].ToString());
                }

                using (SqlCommand reasonCmd = new SqlCommand(
                    "SELECT DISTINCT LTRIM(RTRIM(reason)) AS Name FROM dbo.prmchg WHERE reason IS NOT NULL AND LTRIM(RTRIM(reason)) <> '' ORDER BY 1", connection))
                using (SqlDataReader reader = await reasonCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) reasons.Add(reader["Name"].ToString());
                }

                return Ok(new { reasons, statuses });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving CO reasons/statuses.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving CO reasons/statuses.", Details = ex.Message });
            }
        }

        // NEW (2026-09-20, Mike Smith - "I need to be able to see all cost codes in the dropdown. This
        // way I can debit 1 cost code and credit another") - the full cost code master list
        // (SAGESBQ.dbo.CostCodeList, the same list sp_BudgetTran maps budget lines from), expressed as
        // this job's own cost codes ("<jobId>.<PreferredCode>", e.g. 12162420.100) so they line up with
        // the codes GetJobCostCodes returns. Unlike GetJobCostCodes it is NOT limited to codes the
        // job already has a budget line for, so a code the job hasn't budgeted yet can still be used.
        [HttpGet("api/CoBudgetAllocation/AllCostCodes/{jobId}", Name = "GetCoBudgetAllocationAllCostCodes")]
        public async Task<IActionResult> GetAllCostCodes(long jobId)
        {
            var codes = new List<object>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand(
                    "SELECT CONVERT(decimal(15,3), CONCAT(@JobId, '.', PreferredCode)) AS CostCode, CostCodeDescription FROM dbo.CostCodeList ORDER BY ID", connection);
                command.Parameters.AddWithValue("@JobId", jobId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    codes.Add(new
                    {
                        costCode = Convert.ToDecimal(reader["CostCode"]),
                        costCodeDescription = reader.IsDBNull(reader.GetOrdinal("CostCodeDescription")) ? string.Empty : reader["CostCodeDescription"].ToString().Trim()
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

        // NEW (2026-09-24, Mike Smith - "Submit failed ... 500" on an Internal allocation): each cost code's CURRENT budget per
        // phase for a job (StowellSandbox.dbo.bdglin, summed per cost code + phase). The allocation form uses it to check, before
        // Submit, the same two rules CO_PostBudgetAllocationToSage enforces: a net deduction needs a budget line under the chosen
        // phase, and no cost code may end up below zero - and to say which phase a code's budget is actually under.
        [HttpGet("api/CoBudgetAllocation/PhaseBudgets/{jobId}", Name = "GetCoBudgetAllocationPhaseBudgets")]
        public async Task<IActionResult> GetPhaseBudgets(long jobId)
        {
            var rows = new List<object>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand(
                    // Budget under a phase = its budget line + the (non-void) change orders on that phase
                    // (an allocation now writes only change order lines - see CO_PostBudgetAllocationToSage).
                    "SELECT cstcde, phsnum, SUM(amt) AS Budget FROM (" +
                    " SELECT cstcde, phsnum, ISNULL(ttlbdg, 0) AS amt FROM StowellSandbox.dbo.bdglin WHERE recnum = @JobId" +
                    " UNION ALL" +
                    " SELECT s.cstcde, ISNULL(p.phsnum, 0), ISNULL(s.bdgprc, 0) FROM StowellSandbox.dbo.sbcgln s" +
                    " JOIN StowellSandbox.dbo.prmchg p ON p.recnum = s.recnum WHERE p.jobnum = @JobId AND ISNULL(p.status, 0) <> 5 AND s.cstcde IS NOT NULL" +
                    ") x GROUP BY cstcde, phsnum ORDER BY cstcde, phsnum", connection);
                command.Parameters.AddWithValue("@JobId", jobId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new
                    {
                        costCode = Convert.ToDecimal(reader["cstcde"]),
                        phase = Convert.ToInt32(reader["phsnum"]),
                        budget = reader.IsDBNull(reader.GetOrdinal("Budget")) ? 0m : Convert.ToDecimal(reader["Budget"])
                    });
                }

                return Ok(rows);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the job's budget by phase.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the job's budget by phase.", Details = ex.Message });
            }
        }

        // NEW (2026-09-20, Mike Smith - "adding amounts to budget when deleted ... get allocation doing
        // same") - deletes an allocation. If it had been submitted, CO_DeleteBudgetReallocation reverses
        // its posting on the real Sage budget lines and removes the change order it created (see
        // StowellCoAPI_DB_Changes_2026-09-20_BudgetPosting.sql); a draft is simply marked deleted.
        [HttpDelete("api/CoBudgetAllocation/{allocationId}", Name = "DeleteCoBudgetAllocation")]
        public async Task<IActionResult> Delete(int allocationId, [FromQuery] string deletedBy)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_DeleteBudgetReallocation", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@AllocationId", allocationId);
                command.Parameters.AddWithValue("@DeletedBy", (object)deletedBy ?? DBNull.Value);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                string coNumber = null;
                if (await reader.ReadAsync() && !reader.IsDBNull(reader.GetOrdinal("CoNumber")))
                    coNumber = reader["CoNumber"].ToString();

                return Ok(new { deleted = true, coNumber });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while deleting the CO budget allocation.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the CO budget allocation.", Details = ex.Message });
            }
        }

        [HttpGet("api/CoBudgetAllocation/{allocationId}", Name = "GetCoBudgetAllocationById")]
        public async Task<IActionResult> GetById(int allocationId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("CO_GetBudgetReallocationById", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@AllocationId", allocationId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                CoBudgetAllocationDetail detail = null;
                if (await reader.ReadAsync())
                {
                    detail = new CoBudgetAllocationDetail
                    {
                        AllocationId = Convert.ToInt32(reader["AllocationId"]),
                        CoNumber = reader.IsDBNull(reader.GetOrdinal("CoNumber")) ? string.Empty : reader["CoNumber"].ToString(),
                        JobId = reader.IsDBNull(reader.GetOrdinal("JobId")) ? string.Empty : reader["JobId"].ToString(),
                        JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                        CoType = reader.IsDBNull(reader.GetOrdinal("CoType")) ? string.Empty : reader["CoType"].ToString(),
                        CoDescription = reader.IsDBNull(reader.GetOrdinal("CoDescription")) ? string.Empty : reader["CoDescription"].ToString(),
                        CoReason = reader.IsDBNull(reader.GetOrdinal("CoReason")) ? string.Empty : reader["CoReason"].ToString(),
                        CoStatus = reader.IsDBNull(reader.GetOrdinal("CoStatus")) ? string.Empty : reader["CoStatus"].ToString(),
                        OverallBudget = Convert.ToDecimal(reader["OverallBudget"]),
                        BudgetChangeTotal = Convert.ToDecimal(reader["BudgetChangeTotal"]),
                        ProfitOnSell = Convert.ToDecimal(reader["ProfitOnSell"]),
                        RequestedAmount = reader.IsDBNull(reader.GetOrdinal("RequestedAmount")) ? null : Convert.ToDecimal(reader["RequestedAmount"]),
                        ApprovedAmount = reader.IsDBNull(reader.GetOrdinal("ApprovedAmount")) ? null : Convert.ToDecimal(reader["ApprovedAmount"]),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader["Status"].ToString(),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? string.Empty : reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }

                if (detail == null) return NotFound(new { Message = $"CO budget allocation {allocationId} not found." });

                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    detail.Phases.Add(reader["Phase"].ToString());
                }

                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    detail.Lines.Add(new CoBudgetLineDto
                    {
                        CostCode = reader.IsDBNull(reader.GetOrdinal("CostCode")) ? string.Empty : reader["CostCode"].ToString(),
                        CostCodeDescription = reader.IsDBNull(reader.GetOrdinal("CostCodeDescription")) ? string.Empty : reader["CostCodeDescription"].ToString(),
                        Debit = Convert.ToDecimal(reader["Debit"]),
                        Credit = Convert.ToDecimal(reader["Credit"])
                    });
                }

                return Ok(detail);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the CO budget allocation.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the CO budget allocation.", Details = ex.Message });
            }
        }

        // Shared by Save/Submit - both procs take the same header/phases/lines shape.
        // CO_SaveBudgetReallocation has no @CoNumber/@ProfitOnSell parameters at all (a number is
        // only assigned at Submit time, per the mockup's Internal="BC.."/Prime="CO.." numbering
        // rule) - Submit's action adds those two itself before calling this.
        private static void AddCommonParameters(SqlCommand command, CoBudgetAllocationRequestDto request)
        {
            command.Parameters.AddWithValue("@JobId", (object)request.JobId ?? DBNull.Value);
            command.Parameters.AddWithValue("@JobName", (object)request.JobName ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoType", (object)request.CoType ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoDescription", (object)request.CoDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoReason", (object)request.CoReason ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoStatus", (object)request.CoStatus ?? DBNull.Value);
            command.Parameters.AddWithValue("@OverallBudget", request.OverallBudget);
            command.Parameters.AddWithValue("@RequestedAmount", (object)request.RequestedAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("@ApprovedAmount", (object)request.ApprovedAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedBy", (object)request.CreatedBy ?? DBNull.Value);
            // CO_SaveBudgetReallocation/CO_SubmitBudgetReallocation both expect each phase as
            // {"phase": "..."} - not a plain string array - to read via JSON_VALUE(value, '$.phase').
            var phasesPayload = (request.Phases ?? new()).Select(p => new { phase = p });
            command.Parameters.AddWithValue("@PhasesJson", JsonSerializer.Serialize(phasesPayload, CamelCase));
            command.Parameters.AddWithValue("@LinesJson", JsonSerializer.Serialize(request.Lines ?? new(), CamelCase));
        }
    }
}
