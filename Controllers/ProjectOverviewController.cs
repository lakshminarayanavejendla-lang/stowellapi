using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
using System.Data;

namespace StowellCoAPI.Controllers
{
    [ApiController]
    public class ProjectOverviewController : ControllerBase
    {
       
            private readonly IConfiguration _configuration;
            private readonly ILogger<ProjectOverviewController> _logger;
            //CurrentCostSummaryViewModel ccsVm = new CurrentCostSummaryViewModel();

            public ProjectOverviewController(ILogger<ProjectOverviewController> logger, IConfiguration configuration)
            {
                _logger = logger;
                _configuration = configuration;
            }
        [HttpGet("api/[controller]/GetCurrentCostSummaryViewModel/{recnum}", Name = "GetCurrentCostSummaryViewModel")]
        public async Task<IActionResult> GetCurrentCostSummaryViewModel(string recnum, string costCode = "", string phase = "")
        {
            try
            {
                // PERFORMANCE FIX: these six calls are fully independent (separate SqlConnections,
                // no shared state) but were being awaited one at a time, so the total wait time was
                // the SUM of all six instead of the MAX. On a job whose SPCostSummary and
                // SPJobCashToDateDetails calls are individually slow (~30s each, confirmed via
                // direct SQL testing against this environment's local database), sequential
                // execution alone could add up to a minute or more before this endpoint ever
                // responds. A parallel version was previously attempted (see the commented-out
                // block below this method) but reverted back to sequential awaits - restoring it
                // properly here, plus wrapping the two previously-unguarded calls (GetCashFlowRecords,
                // GetBudgetPaymentsData) in try/catch so one slow/failing query degrades to an empty
                // default instead of taking the whole response down.
                var costCodeSummaryTask = GetCostCodeSummaryRecords(recnum, costCode, phase);
                var jobsTask = GetJobCodes();
                var costCodesTask = GetCostCodesList(recnum);
                var phasesTask = GetPhasesList(recnum);
                var cashFlowTask = GetCashFlowRecords(recnum);
                var budgetPaymentTask = GetBudgetPaymentsData(recnum);

                await Task.WhenAll(costCodeSummaryTask, jobsTask, costCodesTask, phasesTask, cashFlowTask, budgetPaymentTask);

                var model = new CurrentCostSummaryViewModel
                {
                    CostCodeSummaryModel = costCodeSummaryTask.Result,
                    Jobs = jobsTask.Result ?? new List<Job>(),
                    CostCodes = costCodesTask.Result ?? new List<CostCode>(),
                    Phases = phasesTask.Result ?? new List<Phase>(),
                    CashFlowRecord = cashFlowTask.Result,
                    BudgetPayment = budgetPaymentTask.Result,
                    ShareFolderLink = GetFolderLocation(recnum)
                };

                // FIX (2026-09-24, Mike Smith - Total Budget differed between "All phases" and a phase): the budget box's
                // all-phases figures came from VwBudgetPayments, which adds hours + every budget column + the ORIGINAL amounts
                // on top of the current budget (job 10120: $17.8M against a $9.59M budget) and reports customer payments as
                // "Paid". Report the same figures the cost summary (and every phase view) uses instead: budget + changes,
                // costs to date, and the balance before open-PO commitments (the PO tab subtracts those itself).
                var summary = model.CostCodeSummaryModel;
                if (summary?.CostCodeSummary != null && summary.CostCodeSummary.Count > 0)
                {
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    model.BudgetPayment = new BudgetPayment
                    {
                        Recnum = recnum,
                        Budget = summary.SumOfBudgetAndChanges.ToString("0.00", inv),
                        Paid = summary.SumOfToDate.ToString("0.00", inv),
                        // Remaining already has the open POs' commitments taken off (by the PO's own phase).
                        Balance = summary.SumOfRemaining.ToString("0.00", inv)
                    };
                }

                var jobObj = model.Jobs.FirstOrDefault(x => x.Recnum.Equals(recnum, StringComparison.OrdinalIgnoreCase));
                if (jobObj != null)
                {
                    model.SelectedJobName = jobObj.JobName;
                    model.SelectedJobId = jobObj.Recnum;
                }

                return Ok(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building CurrentCostSummaryViewModel for job {Recnum}", recnum);
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving cost summary.",
                    Details = ex.Message
                });
            }
        }
        //public async Task<CurrentCostSummaryViewModel> GetCurrentCostSummaryViewModel(string recnum, string costCode = "", string phase = "")
        //    {
        //    try
        //    {
        //        CurrentCostSummaryViewModel model = new CurrentCostSummaryViewModel();

        //        // Initialize tasks
        //        Task<CostCodeSummaryModel> costCodeSummaryRecordsTask = GetCostCodeSummaryRecords(recnum, costCode, phase);
        //        Task<List<Job>> jobsTask = GetJobCodes();
        //        Task<List<CostCode>> costCodesTask = GetCostCodesList(recnum);
        //        Task<List<Phase>> phasesTask = GetPhasesList(recnum);
        //        Task<CashFlowRecord> cashFlowTask = GetCashFlowRecords(recnum);
        //        Task<BudgetPayment> budgetPayment = GetBudgetPaymentsData(recnum);
        //        // Await completion of all tasks
        //        await Task.WhenAll(costCodeSummaryRecordsTask, jobsTask, costCodesTask, phasesTask, cashFlowTask, budgetPayment);

        //        // Retrieve results
        //        model.CostCodeSummaryModel = costCodeSummaryRecordsTask.Result;
        //        model.Jobs = jobsTask.Result;
        //        model.CostCodes = costCodesTask.Result;
        //        model.Phases = phasesTask.Result;
        //        model.CashFlowRecord = cashFlowTask.Result;
        //        model.BudgetPayment = budgetPayment.Result;

        //        var jobObj = model.Jobs.FirstOrDefault(x => x.Recnum.Equals(recnum, StringComparison.OrdinalIgnoreCase));
        //        if (jobObj != null)
        //        {
        //            model.SelectedJobName = jobObj.JobName;
        //            model.SelectedJobId = jobObj.Recnum;
        //        }
        //        model.ShareFolderLink = GetFolderLocation(recnum);
        //        return Ok(model);
        //    }
        //    catch (SqlException sqlEx)
        //    {
        //        // Log SQL-specific errors (e.g., connection or syntax issues)
        //        return StatusCode(500, new
        //        {
        //            Message = "A database error occurred while retrieving jobs.",
        //            Details = sqlEx.Message
        //        });
        //        _logger.LogError(sqlEx, sqlEx.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Catch all unexpected exceptions
        //        return StatusCode(500, new
        //        {
        //            Message = "An unexpected error occurred while retrieving jobs.",
        //            Details = ex.Message
        //        });
        //        _logger.LogError(ex, ex.Message);
        //    }
        //}

        private async Task<BudgetPayment> GetBudgetPaymentsData(string jobId)
            {
                try
                {
                    string query = "select * from VwBudgetPayments where recnum = @recnum";
                    string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                    using (SqlConnection sqlConn = new SqlConnection(connectionString))
                    {
                        using (var command = new SqlCommand(query, sqlConn) { CommandTimeout = 15 })
                        {
                            command.Parameters.AddWithValue("@recnum", jobId);
                            sqlConn.Open();

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var record = new BudgetPayment
                                    {
                                        Recnum = reader.IsDBNull(reader.GetOrdinal("Recnum")) ? null : reader["Recnum"].ToString(),
                                        Budget = reader.IsDBNull(reader.GetOrdinal("totalbudget")) ? null : reader["totalbudget"].ToString(),
                                        Paid = reader.IsDBNull(reader.GetOrdinal("AmountPaid")) ? null : reader["AmountPaid"].ToString(),
                                        Balance = reader.IsDBNull(reader.GetOrdinal("Balance")) ? null : reader["Balance"].ToString()
                                    };
                                    return record;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetBudgetPaymentsData failed for job {JobId}", jobId);
                }
                return new BudgetPayment();
            }

            private string GetFolderLocation(string recnum)
            {
                string folderLocation = string.Empty;
                var mapping = new Dictionary<string, string>();
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "GetJobLocation";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@JobID", recnum);
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                folderLocation = reader["FolderLocation"] == DBNull.Value ? string.Empty : reader["FolderLocation"].ToString();
                                break;
                            }
                        }
                    }
                }


                //string siteUrl = "https://stowell.sharepoint.com/sites/Stowell/";
                //folderLocation = Regex.Replace(folderLocation, "/sites/Stowell/", "", RegexOptions.IgnoreCase);

                //var sharepointLink = siteUrl+ folderLocation;

                return folderLocation;
            }

            //[Route("/ProjectOverview/GetAllContractorClients")]
            //public async Task<IActionResult> GetAllContractorClients()
            //{
            //    List<ContractorClient> visibleRecords = new List<ContractorClient>();
            //    try
            //    {
            //        List<ContractorClient> costCodeRecords = await GetCostCodeRecords(4);
            //        visibleRecords = costCodeRecords;
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "All User Jobs Error");
            //    }
            //    return Json(new { data = visibleRecords });
            //}

            private async Task<List<ContractorClient>> GetCostCodeRecords(int status)
            {
                List<ContractorClient> costCodeRecords = new List<ContractorClient>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SELECT ID, su.DisplayName as UserName FROM StowellUsers su ORDER BY su.DisplayName";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);

                    connection.Open();
                    SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (reader.Read())
                    {
                        ContractorClient record = new ContractorClient
                        {
                            Id = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : Convert.ToInt32(reader["ID"]),
                            Name = reader.IsDBNull(reader.GetOrdinal("UserName")) ? string.Empty : reader["UserName"].ToString(),
                        };

                        costCodeRecords.Add(record);
                    }
                }

                return costCodeRecords;
            }

            //[HttpPost]
            //[Route("/ProjectOverview/InsertEmployee")]
            //public JsonResult InsertEmployee(string jobId, int empId, string roleName)
            //{
            //    try
            //    {
            //        string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            //        using (var conn = new SqlConnection(connectionString))
            //        using (var cmd = new SqlCommand("BidEmployee_Insert", conn))
            //        {
            //            cmd.CommandType = CommandType.StoredProcedure;
            //            cmd.Parameters.AddWithValue("@JobID", jobId);
            //            cmd.Parameters.AddWithValue("@Role", roleName); // Assuming roleName = 'PM' or 'Sr. PM'
            //            cmd.Parameters.AddWithValue("@Employee", empId); // Assuming this is recNum

            //            conn.Open();
            //            cmd.ExecuteNonQuery();
            //        }

            //        return Json(new { success = true });
            //    }
            //    catch (Exception ex)
            //    {
            //        return Json(new { success = false, message = ex.Message });
            //    }
            //}

            //[HttpGet]
            //[Route("/ProjectOverview/GetEmployees")]
            //public JsonResult GetEmployees(string jobId)
            //{
            //    var employees = new List<object>();
            //    string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            //    using (var conn = new SqlConnection(connectionString))
            //    using (var cmd = new SqlCommand("BidEmployee_Select", conn))
            //    {
            //        cmd.CommandType = CommandType.StoredProcedure;
            //        cmd.Parameters.AddWithValue("@JobID", jobId);
            //        conn.Open();

            //        using (var reader = cmd.ExecuteReader())
            //        {
            //            while (reader.Read())
            //            {
            //                employees.Add(new
            //                {
            //                    Id = reader.IsDBNull(reader.GetOrdinal("ID")) ? string.Empty : reader["ID"].ToString(),
            //                    Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? string.Empty : reader["Role"].ToString(),
            //                    Employee = reader.IsDBNull(reader.GetOrdinal("Employee")) ? string.Empty : reader["Employee"].ToString()
            //                });
            //            }
            //        }
            //    }

            //    return Json(employees);
            //}


            //[HttpPost]
            //[Route("/ProjectOverview/Submit")]
            //public async Task<IActionResult> Submit(CurrentCostSummaryViewModel model)
            //{

            //    var vm = await GetCurrentCostSummaryViewModel(model.SelectedJobId, model.SelectedCostCodeIds, model.SelectedPhaseId);
            //    if (vm == null)
            //    {
            //        vm.SelectedCostCodeIds = model.SelectedCostCodeIds;
            //        vm.SelectedPhaseId = model.SelectedPhaseId;
            //    }
            //    return View("Index", vm);
            //}
            //[HttpPost]
            //[Route("/ProjectOverview/GetCostCodes")]
            //public async Task<IActionResult> GetCostCodes(string jobId)
            //{
            //    var costCodes = await GetCostCodesList(jobId);
            //    return Json(costCodes);
            //}

            //[HttpGet]
            //public async Task<IActionResult> GetUserProjects(int id)
            //{
            //    var record = await GetUserProject(id); // Use a service layer for better abstraction
            //    if (record == null)
            //    {
            //        return NotFound("No project details found for the given parameter.");
            //    }
            //    return Json(record);
            //}

            private async Task<ProjectUser> GetUserProject(int param)
            {
                ProjectUser record = new ProjectUser();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                _logger.LogDebug("Using connection string: {ConnectionString}", connectionString);

                string query = "SPGetProjectDetails";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@recnum", param);
                    command.CommandType = CommandType.StoredProcedure;

                    connection.Open();
                    SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (reader.Read())
                    {
                        record = new ProjectUser
                        {
                            RecNum = reader.IsDBNull(reader.GetOrdinal("recnum")) ? 0 : Convert.ToInt32(reader["recnum"]),
                            JobName = reader.IsDBNull(reader.GetOrdinal("JobNumber")) ? string.Empty : reader["JobNumber"].ToString(),
                            PM1 = reader.IsDBNull(reader.GetOrdinal("PMEmail")) ? string.Empty : reader["PMEmail"].ToString(),
                            PM2 = reader.IsDBNull(reader.GetOrdinal("APMEmail")) ? string.Empty : reader["APMEmail"].ToString(),
                            Division = reader.IsDBNull(reader.GetOrdinal("Division")) ? string.Empty : reader["Division"].ToString()
                        };

                        return record;
                    }
                }

                return record;
            }


            //[HttpPost]
            //[Route("/ProjectOverview/GetPhases")]
            //public async Task<IActionResult> GetPhases(string costCodeId)
            //{
            //    var phases = await GetPhasesList(costCodeId);
            //    return Json(phases);
            //}

            //[HttpPost]
            //[Route("/ProjectOverview/SetJobAccess")]
            //public async Task<IActionResult> SetJobAccess([FromBody] AccessRequest request)
            //{
            //    var isSuccess = await SetAccessRequest(request);
            //    if (isSuccess)
            //    {
            //        return Ok(new { message = "Access has been successfully set." });
            //    }
            //    else
            //    {
            //        return BadRequest(new { message = "Something went wrong" });
            //    }
            //}

            //public async Task<bool> SetAccessRequest(AccessRequest request)
            //{
            //    try
            //    {
            //        string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            //        string query = "IU_ProjectManagement";

            //        using (SqlConnection connection = new SqlConnection(connectionString))
            //        {
            //            SqlCommand command = new SqlCommand(query, connection);
            //            command.CommandText = query;
            //            command.CommandType = CommandType.StoredProcedure;

            //            command.Parameters.AddWithValue("@JobNumber", request.JobId);
            //            command.Parameters.AddWithValue("@PMEmail", request.ProjectManager);
            //            command.Parameters.AddWithValue("@APMEmail", request.AssistantProjectManager);
            //            if (!string.IsNullOrEmpty(request.BranchManagerGroupId))
            //            {
            //                if (request.BranchManagerGroupId == "removeBranch")
            //                {
            //                    request.BranchManagerGroupId = "";
            //                }

            //                command.Parameters.AddWithValue("@Division", request.BranchManagerGroupId);
            //            }

            //            connection.Open();
            //            await command.ExecuteNonQueryAsync();
            //        }

            //        _logger.LogInformation($"Job id {request.JobId}, set to {request.ProjectManager} and {request.AssistantProjectManager}");

            //        return true;
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Set Access Request Error");
            //        return false;
            //    }
            //}

            private async Task<CashFlowRecord> GetCashFlowRecords(string recnum)
            {
                CashFlowRecord CodeRecords = new CashFlowRecord();

                try
                {
                    string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                    string query = "SPJobCashToDateDetails";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        SqlCommand command = new SqlCommand(query, connection) { CommandTimeout = 15 };
                        command.CommandText = query;
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@JobNumber", recnum);

                        connection.Open();
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                CashFlowRecord record = new CashFlowRecord
                                {
                                    RecNum = !reader.IsDBNull(reader.GetOrdinal("RecNum")) ? Convert.ToInt32(reader["RecNum"]) : 0,
                                    JobNumber = !reader.IsDBNull(reader.GetOrdinal("jobnme")) ? reader["jobnme"].ToString() : string.Empty,
                                    CashCollected = !reader.IsDBNull(reader.GetOrdinal("CashCollected")) ? Convert.ToDecimal(reader["CashCollected"]) : 0.0m,
                                    CashPaid = !reader.IsDBNull(reader.GetOrdinal("CashPaid")) ? Convert.ToDecimal(reader["CashPaid"]) : 0.0m,
                                    NetCash = !reader.IsDBNull(reader.GetOrdinal("NetCash")) ? Convert.ToDecimal(reader["NetCash"]) : 0.0m,
                                    InvoiceJ2D = !reader.IsDBNull(reader.GetOrdinal("InvoiceJ2D")) ? Convert.ToDecimal(reader["InvoiceJ2D"]) : 0.0m,
                                    A_REndBal_Today = !reader.IsDBNull(reader.GetOrdinal("A_REndBal_Today")) ? Convert.ToDecimal(reader["A_REndBal_Today"]) : 0.0m,
                                    jobcoststodate = !reader.IsDBNull(reader.GetOrdinal("jobcoststodate")) ? Convert.ToDecimal(reader["jobcoststodate"]) : 0.0m,
                                    A_PEndBal_Today = !reader.IsDBNull(reader.GetOrdinal("A_PEndBal_Today")) ? Convert.ToDecimal(reader["A_PEndBal_Today"]) : 0.0m,
                                    CashPaidOut = !reader.IsDBNull(reader.GetOrdinal("CashPaidOut")) ? Convert.ToDecimal(reader["CashPaidOut"]) : 0.0m,
                                    NetCashInOut = !reader.IsDBNull(reader.GetOrdinal("NetCashInOut")) ? Convert.ToDecimal(reader["NetCashInOut"]) : 0.0m  // Fixed column name
                                };

                                return record;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetCashFlowRecords failed for job {Recnum}", recnum);
                }

                return CodeRecords;
            }

        private async Task<CostCodeSummaryModel> GetCostCodeSummaryRecords(string recnum, string costCode = "", string phase = "")
        {
            CostCodeSummaryModel costCodeSummaryModel = new CostCodeSummaryModel();
            List<CostCodeSummaryRecord> costCodeSummaryRecords = new List<CostCodeSummaryRecord>();

            try
            {

                string query = "SPCostSummary";
                //string connectionString = _configuration.GetConnectionString("StowellConnection");
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
                {
                    using (var command = new SqlCommand("dbo.SPCostSummary", connection) { CommandTimeout = 15 })
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@JobNumber", recnum);
                        command.Parameters.AddWithValue("@costcode", costCode);
                        command.Parameters.AddWithValue("@startphase", phase);
                        command.Parameters.AddWithValue("@endphase", phase);
                        command.Parameters.AddWithValue("@StartDate", DBNull.Value);
                        command.Parameters.AddWithValue("@EndDate", DBNull.Value);
                        // "0 - None" means phase 0 only (0 alone is "no filter" in the start/end parameters).
                        if (phase?.Trim() == "0") command.Parameters.AddWithValue("@exactphase", 0);

                        connection.Open();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new CostCodeSummaryRecord
                                {
                                    JobNumber = reader.IsDBNull(reader.GetOrdinal("recnum")) ? null : reader["recnum"].ToString(),
                                    CostCode = reader.IsDBNull(reader.GetOrdinal("CostCode")) ? null : reader["CostCode"].ToString(),
                                    CostCodeDescription = reader.IsDBNull(reader.GetOrdinal("CostCodeDescription")) ? null : reader["CostCodeDescription"].ToString(),
                                    BudgetAndChanges = reader.IsDBNull(reader.GetOrdinal("Budget_Charges")) ? 0 : Convert.ToDecimal(reader["Budget_Charges"]),
                                    ToDate = reader.IsDBNull(reader.GetOrdinal("ToDate")) ? 0 : Convert.ToDecimal(reader["ToDate"]),
                                    ThisPeriod = reader.IsDBNull(reader.GetOrdinal("ThisPeriod")) ? 0 : Convert.ToDecimal(reader["ThisPeriod"]),
                                    Remaining = reader.IsDBNull(reader.GetOrdinal("Remaining")) ? 0 : Convert.ToDecimal(reader["Remaining"]),
                                    Committed = reader.HasColumn("Committed") && !reader.IsDBNull(reader.GetOrdinal("Committed")) ? Convert.ToDecimal(reader["Committed"]) : 0
                                    // Populate other properties as needed
                                };
                                costCodeSummaryModel.SumOfBudgetAndChanges += record.BudgetAndChanges;
                                costCodeSummaryModel.SumOfToDate += record.ToDate;
                                costCodeSummaryModel.SumOfThisPeriod += record.ThisPeriod;
                                costCodeSummaryModel.SumOfRemaining += record.Remaining;
                                costCodeSummaryModel.SumOfCommitted += record.Committed;

                                costCodeSummaryRecords.Add(record);
                            }
                        }
                    }
                }
                costCodeSummaryModel.CostCodeSummary = costCodeSummaryRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Set Access Request Error");
                costCodeSummaryModel = new CostCodeSummaryModel();
            }
            return costCodeSummaryModel;
        }

            private async Task<CostCodeSummaryModel> GetCostCodeSummaryRecordsByModel(string recnum)
            {
                CostCodeSummaryModel costCodeSummaryModel = new CostCodeSummaryModel();
                List<CostCodeSummaryRecord> costCodeSummaryRecords = new List<CostCodeSummaryRecord>();



                string query = "SELECT    c.recnum AS CostCode, c.cdenme AS CostCodeDescription, b.recnum, B.ttlbdg AS Budget_Changes, j.cstamt ToDate, TD.cstamt AS ThisPeriod, (B.ttlbdg - j.cstamt) AS Remaining \r\nfrom cstcde c \r\nleft outer JOIN (SELECT b.cstcde, sum(b.ttlbdg) as ttlbdg, b.recnum from bdglin b group by b.cstcde, b.recnum) AS B ON B.cstcde = c.recnum\r\nLEFT outer JOIN (SELECT  cstcde, sum(cstamt) as cstamt  FROM jobcst group by cstcde) AS j ON c.recnum = j.cstcde \r\nLEFT outer JOIN (SELECT  cstcde, sum(cstamt) as cstamt  FROM jobcst WHERE month(trndte) = month(getdate()) group by cstcde) AS TD ON c.recnum = TD.cstcde\r\nWHERE b.recnum = " + recnum;
                string connectionString = _configuration.GetConnectionString("StowellConnection");
                using (SqlConnection sqlConn = new SqlConnection(connectionString))
                {

                    using (var command = new SqlCommand(query, sqlConn))
                    {
                        sqlConn.Open();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new CostCodeSummaryRecord
                                {
                                    //Id = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader["Id"]),
                                    CostCode = reader.IsDBNull(reader.GetOrdinal("CostCode")) ? null : reader["CostCode"].ToString(),
                                    CostCodeDescription = reader.IsDBNull(reader.GetOrdinal("CostCodeDescription")) ? null : reader["CostCodeDescription"].ToString(),
                                    BudgetAndChanges = reader.IsDBNull(reader.GetOrdinal("Budget_Changes")) ? 0 : Convert.ToDecimal(reader["Budget_Changes"]),
                                    ToDate = reader.IsDBNull(reader.GetOrdinal("ToDate")) ? 0 : Convert.ToDecimal(reader["ToDate"]),
                                    ThisPeriod = reader.IsDBNull(reader.GetOrdinal("ThisPeriod")) ? 0 : Convert.ToDecimal(reader["ThisPeriod"]),
                                    Remaining = reader.IsDBNull(reader.GetOrdinal("Remaining")) ? 0 : Convert.ToDecimal(reader["Remaining"])
                                };

                                costCodeSummaryModel.SumOfBudgetAndChanges += record.BudgetAndChanges;
                                costCodeSummaryModel.SumOfToDate += record.ToDate;
                                costCodeSummaryModel.SumOfThisPeriod += record.ThisPeriod;
                                costCodeSummaryModel.SumOfRemaining += record.Remaining;

                                costCodeSummaryRecords.Add(record);

                            }
                        }
                    }
                }
                costCodeSummaryModel.CostCodeSummary = costCodeSummaryRecords;
                return costCodeSummaryModel;
            }
        [HttpGet("api/[controller]/GetJobDetails/{recnum}", Name = "GetJobDetails")]
        public async Task<IActionResult> GetJobDetails(string recnum)
        {
            Job jobcodedata = new Job();
            try
            {
                jobcodedata = await GetJobCodeDetails(recnum);
            }
            catch (Exception ex)
            {
            }
            return Ok(jobcodedata);
        }
        private async Task<List<Job>> GetJobCodes()
            {
                List<Job> jobCodes = new List<Job>();

                string query = "select * from VWJobNumber";
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (SqlConnection sqlConn = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand(query, sqlConn))
                    {
                        sqlConn.Open();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new Job
                                {
                                    FullJob = reader.IsDBNull(reader.GetOrdinal("FullJob")) ? null : reader["FullJob"].ToString(),
                                    JobName = reader.IsDBNull(reader.GetOrdinal("JobNme")) ? null : reader["JobNme"].ToString(),
                                    Recnum = reader.IsDBNull(reader.GetOrdinal("Recnum")) ? null : reader["Recnum"].ToString(),
                                };

                                jobCodes.Add(record);
                            }
                        }
                    }
                }
                return jobCodes;
            }
        private async Task<Job> GetJobCodeDetails(string recNum)
        {
            Job jobdetails = new Job();

            string query = "select * from VWJobNumber where recnum=" + recNum + "";
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            using (SqlConnection sqlConn = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand(query, sqlConn))
                {
                    sqlConn.Open();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            jobdetails = new Job
                            {
                                FullJob = reader.IsDBNull(reader.GetOrdinal("FullJob")) ? null : reader["FullJob"].ToString(),
                                JobName = reader.IsDBNull(reader.GetOrdinal("JobNme")) ? null : reader["JobNme"].ToString(),
                                Recnum = reader.IsDBNull(reader.GetOrdinal("Recnum")) ? null : reader["Recnum"].ToString(),
                            };

                            
                        }
                    }
                }
            }
            return jobdetails;
        }
        private async Task<List<CostCode>> GetCostCodesList(string jobId)
            {
                List<CostCode> records = new List<CostCode>();

                string query = "select distinct * from vwcostcode where id = " + jobId;
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (SqlConnection sqlConn = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand(query, sqlConn))
                    {
                        sqlConn.Open();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new CostCode
                                {
                                    Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? null : reader["Id"].ToString(),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                                };

                                records.Add(record);
                            }
                        }
                    }
                }
                return records;
            }

            private async Task<List<Phase>> GetPhasesList(string jobId)
            {
                List<Phase> records = new List<Phase>();

                // FIX (2026-09-23, Mike Smith - "phase not showing" / "see second phase"): vwphases only
                // reads Sage's jobphs, but phases added in the app ("Additional Phases" ->
                // BidPhase_Insert) are stored in dbo.BidPhase and never reach jobphs, so they never
                // appeared in the phase dropdowns. Include them (skipping any Sage already has).
                string query = @"select recnum, phsnum, PhasesName from vwphases where recnum = @jobId
                                 union all
                                 select TRY_CAST(b.JobID as int), TRY_CAST(b.Phase as int), b.Description
                                 from dbo.BidPhase b
                                 where b.JobID = @jobIdText and COALESCE(b.IsDeleted, 0) = 0
                                   and TRY_CAST(b.Phase as int) is not null
                                   and not exists (select 1 from vwphases v where v.recnum = TRY_CAST(b.JobID as int) and v.phsnum = TRY_CAST(b.Phase as int))
                                 order by phsnum";
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (SqlConnection sqlConn = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand(query, sqlConn))
                    {
                        command.Parameters.AddWithValue("@jobId", int.Parse(jobId));
                        command.Parameters.AddWithValue("@jobIdText", jobId);
                        sqlConn.Open();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new Phase
                                {
                                    PhaseName = reader.IsDBNull(reader.GetOrdinal("PhasesName")) ? null : reader["PhasesName"].ToString(),
                                    PhaseNum = reader.IsDBNull(reader.GetOrdinal("phsnum")) ? null : reader["phsnum"].ToString(),
                                    Recnum = reader.IsDBNull(reader.GetOrdinal("Recnum")) ? null : reader["Recnum"].ToString(),
                                };

                                records.Add(record);
                            }
                        }
                    }
                }
                return records;
        }
    }
}
