using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using NPOI.XSSF.UserModel;
using StowellCoAPI.DTO;
using System.Data;
using System.Linq;

namespace StowellCoAPI.Controllers
{
    [ApiController]
    public class ProjectBudgetConfigurationController : ControllerBase
    {
        private readonly ILogger<ProjectBudgetConfigurationController> _logger;
        private readonly IConfiguration _configuration;
       // private readonly GraphServiceClient _graphServiceClient;
        // private readonly UserInfoService _userInfoService;


        public ProjectBudgetConfigurationController(ILogger<ProjectBudgetConfigurationController> logger, IConfiguration configuration)
            //, GraphServiceClient graphServiceClient)
        //UserInfoService userInfoService)
        {
            _configuration = configuration;
           // _graphServiceClient = graphServiceClient;
            // _userInfoService = userInfoService;
            _logger = logger;
        }

        [HttpGet("api/[controller]/GetProjectBudgetRecords/{email}")]
        public async Task<IActionResult> GeProjectBudgettRecords(string email)
        {
            var result = new List<ProjectBudgetQueueItem>();
            try
            {
                string cs = _configuration.GetConnectionString("SageSBQConnection");
                using var conn = new SqlConnection(cs);
                //string email = HttpContext.User.Identity.Name;
                using var cmd = new SqlCommand("dbo.sp_Projectconfigurationselect", conn);
                //cmd.Parameters.AddWithValue("@EmailID", email);
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new ProjectBudgetQueueItem
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("ID")),
                        JobID = reader.IsDBNull(reader.GetOrdinal("JobID")) ? string.Empty : reader["JobID"].ToString(),
                        JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                        Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : reader["Address"].ToString(),
                        Phase = reader.IsDBNull(reader.GetOrdinal("Phase")) ? string.Empty : reader["Phase"].ToString(),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader["Description"].ToString(),
                        Source = reader.IsDBNull(reader.GetOrdinal("Source")) ? string.Empty : reader["Source"].ToString()
                    });
                }

                return Ok(result);
            }
            catch (SqlException sqlEx)
            {
                // Log SQL-specific errors (e.g., connection or syntax issues)
                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving jobs.",
                    Details = sqlEx.Message
                });
                _logger.LogError(sqlEx, sqlEx.Message);
            }
            catch (Exception ex)
            {
                // Catch all unexpected exceptions
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving jobs.",
                    Details = ex.Message
                });
                _logger.LogError(ex, ex.Message);
            }
        }

        [HttpPost("api/[controller]/GetProjectBudgetRecords")]
        public async Task<IActionResult> BudgetData([FromBody] Phase input)
        {
            try
            {
                if (input != null && !string.IsNullOrEmpty(input.Source))
                {
                    if (input.Source.ToLower() == "sage")
                    {
                        var records = await GetBudgetData(input, "dbo.GetSageJobCostBudgetTran");

                        return Ok(records);
                    }
                    else
                    {
                        var records = await GetBudgetData(input, "dbo.GetLocalJobCostBudgetTran");
                        return Ok(records);
                    }
                }
                else
                {
                    var records = await GetBudgetData(input, "dbo.GetLocalJobCostBudgetTran");

                    return Ok(records);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Budget Loading failed.");
                return Ok(ex.Message);
            }
        }
        private async Task<List<BudgetRecord>> GetBudgetData(Phase input, string spname)
        {
            //CostCodeSummaryModel costCodeSummaryModel = new CostCodeSummaryModel();
            List<BudgetRecord> costCodeSummaryRecords = new List<BudgetRecord>();

            //string connectionString = _configuration.GetConnectionString("StowellConnection");
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
            {
                using (var command = new SqlCommand(spname, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    if (!string.IsNullOrEmpty(input.Recnum))
                    {
                        command.Parameters.AddWithValue("@jobid", input.Recnum);
                    }
                    if (!string.IsNullOrEmpty(input.PhaseNum))
                    {
                        command.Parameters.AddWithValue("@Phsnum", input.PhaseNum);
                    }
                    connection.Open();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var record = new BudgetRecord
                            {
                                JobNumber = reader.IsDBNull(reader.GetOrdinal("JobID")) ? null : reader["JobID"].ToString(),
                                CostCode = reader.IsDBNull(reader.GetOrdinal("CostCode")) ? null : reader["CostCode"].ToString(),
                                CostCodeDescription = reader.IsDBNull(reader.GetOrdinal("CostCodeDescription")) ? null : reader["CostCodeDescription"].ToString(),
                                TotalBudget = reader.IsDBNull(reader.GetOrdinal("Budget")) ? 0 : Convert.ToDecimal(reader["Budget"]),
                                UpdateBudget = reader.IsDBNull(reader.GetOrdinal("Budget")) ? 0 : Convert.ToDecimal(reader["Budget"]),
                                // Populate other properties as needed
                            };
                            costCodeSummaryRecords.Add(record);
                        }
                    }
                }
            }
            //costCodeSummaryModel.CostCodeSummary = costCodeSummaryRecords;
            return costCodeSummaryRecords;
        }

        // Mike Smith, 2026-09-26: "After initial budget is set, no one can change it - all changes come through Change Orders." The Budget
        // Configuration page already shows a phase that has a budget as view-only; this enforces the same rule on the server so a direct call
        // cannot change it either. A job/phase is locked as soon as Sage (bdglin) holds budget lines for it; a phase with none yet (a new
        // job, or an added phase) can still have its initial budget created. Returns a message for the first locked job/phase, else null.
        private async Task<string?> FindLockedBudgetAsync(IEnumerable<BudgetRecord?> records)
        {
            var pairs = records
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.JobNumber))
                .Select(r => new { Job = r!.JobNumber!, Phase = r.PhaseNum ?? 0 })
                .Distinct()
                .ToList();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            foreach (var p in pairs)
            {
                await using var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM StowellSandbox.dbo.bdglin WHERE recnum = TRY_CAST(@job AS bigint) AND phsnum = @phase", conn);
                cmd.Parameters.AddWithValue("@job", p.Job);
                cmd.Parameters.AddWithValue("@phase", p.Phase);
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0)
                {
                    string phaseText = p.Phase == 0 ? "None" : p.Phase.ToString();
                    return $"The initial budget for job {p.Job}, phase {phaseText} is already set and can't be changed here. Budget changes are made through Change Orders.";
                }
            }
            return null;
        }

        [HttpPost("api/[controller]/SaveCreateBatchCostCodes", Name = "SaveCreateBatchCostCodes")]
        public async Task<ActionResult<IEnumerable<BudgetRecord>>> SaveCreateBatchCostCodes([FromBody] List<BudgetRecord> input)
        {
            try
            {
                if (input == null)
                {
                    return BadRequest("Invalid input");
                }

                var lockedMessage = await FindLockedBudgetAsync(input);
                if (lockedMessage != null)
                {
                    return Conflict(new { success = false, message = lockedMessage });
                }

                var savedRecords = new List<BudgetRecord>();

                foreach (var item in input)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Source))
                    {
                        if (item.Source.ToLower() == "sage")
                        {

                            await SaveCostCodeFunction(item, "dbo.sp_SaveSageBudgetTransaction");
                            savedRecords.Add(item);
                        }
                        else
                        {
                            await SaveCostCodeFunction(item, "dbo.sp_SaveBudgetTransaction");
                            savedRecords.Add(item);
                        }
                    }
                    //else
                    //{
                    //    await SaveCostCodeFunction(item, "dbo.sp_SaveBudgetTransaction");
                    //    savedRecords.Add(item);
                    //}

                }

                return Ok(savedRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save CostCode failed.");
                return StatusCode(500, new { success = false, message = "An error occurred while saving cost codes." });
            }

        }
        private async Task SaveCostCodeFunction(BudgetRecord input, string spName = null)
        {
            if (string.IsNullOrEmpty(spName))
            {
                spName = "dbo.sp_SaveBudgetTransaction";
            }

            //string email = "p360admin@stowellinc.com";
           // string email= HttpContext.User.Identity.Name;
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(spName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", input.JobNumber);
                    cmd.Parameters.AddWithValue("@BudgetSubmittedBy", input.Email);
                    cmd.Parameters.AddWithValue("@costcodedescription", input.CostCodeDescription ?? "");
                    cmd.Parameters.AddWithValue("@budget", input.ActionType== "CreateBudget" ? (input.TotalBudget ?? 0): (input.UpdateBudget ?? 0));
                    cmd.Parameters.AddWithValue("@PhaseNumber", input.PhaseNum ?? 0);
                    // Mike Smith, 2026-09-26: the budget log showed a NULL phase name (phase 0 "None" in particular) because the name was never
                    // passed - the procedures store whatever @PhaseName they receive.
                    cmd.Parameters.AddWithValue("@PhaseName", (object?)await ResolvePhaseNameAsync(conn, input.JobNumber, input.PhaseNum ?? 0) ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>The job's name for a phase: Sage's phase list first, then phases added in the app (BidPhase); phase 0 is always "None".</summary>
        private static async Task<string?> ResolvePhaseNameAsync(SqlConnection conn, string? jobNumber, decimal phase)
        {
            if (!string.IsNullOrWhiteSpace(jobNumber))
            {
                await using var cmd = new SqlCommand(
                    @"SELECT TOP 1 name FROM (
                          SELECT PhasesName AS name, 1 AS src FROM dbo.vwphases WHERE recnum = TRY_CAST(@job AS int) AND phsnum = @phase
                          UNION ALL
                          SELECT Description, 2 FROM dbo.BidPhase WHERE JobID = @job AND TRY_CAST(Phase AS int) = @phase AND COALESCE(IsDeleted, 0) = 0
                      ) x WHERE name IS NOT NULL AND LTRIM(RTRIM(name)) <> '' ORDER BY src", conn);
                cmd.Parameters.AddWithValue("@job", jobNumber);
                cmd.Parameters.AddWithValue("@phase", phase);
                var found = await cmd.ExecuteScalarAsync();
                if (found != null && found != DBNull.Value) return Convert.ToString(found);
            }
            return phase == 0 ? "None" : $"Phase {phase:0}";
        }

        [HttpPost("api/[controller]/SubmitBatchCostCodes", Name = "SubmitBatchCostCodes")]
        public async Task<IActionResult> SubmitBatchCostCodes([FromBody] List<BudgetRecord> input)
        {
            try
            {
                if (input == null)
                {
                    return BadRequest(new { success = false, message = "Invalid input" });
                }

                var lockedMessage = await FindLockedBudgetAsync(input);
                if (lockedMessage != null)
                {
                    return Conflict(new { success = false, message = lockedMessage });
                }

                var savedRecords = new List<BudgetRecord>();
                foreach (var item in input)
                {
                    await SaveCostCodeFunction(item, "dbo.sp_InsertBudgetTransaction");
                    savedRecords.Add(item);
                }

                // Mike Smith, 2026-09-26: the temporary demo auto-approve that used to post straight to Sage here is REMOVED. A submitted
                // initial budget now waits in the Accounting Queue (BudgetTransactions, not approved / not rejected) until Accounting approves it
                // (AccountingController.BudgetApproveRequest -> sp_ApproveBudgetTransaction, which posts to Sage) or rejects it
                // (BudgetRejectRequest -> sp_RejectBudgetTransaction, which takes it out of the queue).

                return Ok(savedRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Submit CostCode failed.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("api/[controller]/GetCostCodesAsync/{filter}", Name = "GetCostCodesAsync")]
        public Task<List<string>> GetCostCodesAsync(string filter)
        {
            var allCostCodes = new List<string>
    {
        "Cost Code 1",
        "Cost Code 2",
        "Cost Code 3",
        "Cost Code 4"
    };

            if (string.IsNullOrWhiteSpace(filter))
                return Task.FromResult(allCostCodes);

            var filtered = allCostCodes
                .Where(c => c.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(filtered);
        }
        //[HttpPost("api/[controller]/UploadAddExcel", Name = "UploadAddExcel")]
        //public async Task<IActionResult> UploadAddExcel(IFormFile file, string jobId, string phaseNumber)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("No file uploaded.");

        //    try
        //    {
        //        await ProcessExcel(file, jobId, phaseNumber, true);
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Save CostCode failed.");
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}
        private async Task ProcessExcel(IFormFile file, string jobId, string phaseNumber, bool isNew)
        {
            var result = new List<BudgetRecord>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                XSSFWorkbook workbook = new XSSFWorkbook(stream);
                var sheet = workbook.GetSheetAt(0); // First sheet

                // Start from row 1 (0-indexed, row 0 = header)
                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null) continue;

                    string fullCostCode = row.GetCell(0)?.ToString()?.Trim(); // Column A
                    string newAmount = row.GetCell(1)?.ToString()?.Trim();    // Column B
                    string updatedAmount = row.GetCell(2)?.ToString()?.Trim(); // Column C

                    if (string.IsNullOrWhiteSpace(fullCostCode)) continue;

                    decimal _updateBudget = decimal.TryParse(updatedAmount, out _) ? decimal.Parse(updatedAmount) : 0.0m;
                    decimal _totalBudget = decimal.TryParse(newAmount, out _) ? decimal.Parse(newAmount) : 0.0m;

                    if (isNew)
                    {
                        if (_totalBudget == 0) continue;
                    }

                    if (_updateBudget == 0)
                    {
                        if (_totalBudget == 0) continue;
                    }

                    var hyphenIndex = fullCostCode.IndexOf('-');
                    string code = hyphenIndex > 0 ? fullCostCode.Substring(0, hyphenIndex).Trim() : fullCostCode;
                    string description = hyphenIndex > 0 ? fullCostCode.Substring(hyphenIndex + 1).Trim() : "";

                    result.Add(new BudgetRecord
                    {
                        CostCode = code,
                        CostCodeDescription = description,
                        TotalBudget = decimal.TryParse(newAmount, out _) ? decimal.Parse(newAmount) : 0.0m,
                        UpdateBudget = _updateBudget,
                        JobNumber = jobId,
                        PhaseNum = decimal.TryParse(phaseNumber, out _) ? decimal.Parse(phaseNumber) : 0
                    });
                }
            }

            foreach (var i in result)
            {
                if (isNew)
                    await SaveCostCodeFunction(i);
                else
                    await SaveModifyBudgetFunction(i);
            }
        }

        private async Task SaveModifyBudgetFunction(BudgetRecord input, string spName = null)
        {
            if (string.IsNullOrEmpty(spName))
            {
                spName = "dbo.sp_SaveSageBudgetTransaction";
            }

           // string email = HttpContext.User.Identity.Name;
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(spName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", input.JobNumber);
                    cmd.Parameters.AddWithValue("@User", input.Email);
                    cmd.Parameters.AddWithValue("@costcodedescription", input.CostCodeDescription ?? "");
                    cmd.Parameters.AddWithValue("@budget", input.TotalBudget ?? 0);
                    cmd.Parameters.AddWithValue("@phsnum", input.PhaseNum ?? 0);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        [HttpPost("api/[controller]/UploadAddExcel", Name = "UploadAddExcel")]
        public async Task<IActionResult> UploadAddExcel(List<BudgetRecord> budgetRecord)
        {

            try
            {
                foreach (var item in budgetRecord)
                {
                    await SaveCostCodeFunction(item);
                }
                return Ok(new { success = true, message = "All budget records uploaded successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save CostCode failed.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
