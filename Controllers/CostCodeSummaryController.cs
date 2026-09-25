using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using StowellCoAPI.DTO;
using System.Data;

namespace StowellCoAPI.Controllers
{
    // [Route("api/[controller]")]
    [ApiController]
    public class CostCodeSummaryController : ControllerBase
    {
        private readonly ILogger<CostCodeSummaryController> _logger;
        private readonly IConfiguration _configuration;


        public CostCodeSummaryController(ILogger<CostCodeSummaryController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }
        [HttpGet("api/[controller]/GetCostCodeRecords1", Name = "GetCostCodeRecords1")]
        public async Task<IActionResult> GetCostCodeRecords()
        {
            List<CostCodeRecord> costCodeRecords = new List<CostCodeRecord>();

            string connectionString = _configuration.GetConnectionString("StowellConnection");
            string query = "SELECT recnum, jobnme, addrs1+''+addrs2 AS Address, ctynme AS City, state_ AS State, zipcde AS ZipCode FROM actrec";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    //command.Parameters.AddWithValue("@Status", status);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        CostCodeRecord record = new CostCodeRecord
                        {
                            RecNum = reader.IsDBNull(reader.GetOrdinal("recnum")) ? 0 : Convert.ToInt32(reader["recnum"]),
                            JobNumber = reader.IsDBNull(reader.GetOrdinal("jobnme")) ? string.Empty : reader["jobnme"].ToString(),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : reader["Address"].ToString(),
                            City = reader.IsDBNull(reader.GetOrdinal("City")) ? string.Empty : reader["City"].ToString(),
                            State = reader.IsDBNull(reader.GetOrdinal("State")) ? string.Empty : reader["State"].ToString(),
                            ZipCode = reader.IsDBNull(reader.GetOrdinal("ZipCode")) ? string.Empty : reader["ZipCode"].ToString()
                        };

                        costCodeRecords.Add(record);
                    }
                }

                return Ok(costCodeRecords);
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
        //[Route("/CostCodeSummary")]
        [NonAction]
        public async Task<IActionResult> Index()
        {
            CurrentCostSummaryViewModel model = new CurrentCostSummaryViewModel();
            model.Jobs = await GetJobCodes();
            model.CostCodes = new List<CostCode>();
            model.CostCodeSummaryModel = new CostCodeSummaryModel();
            model.CostCodeSummaryModel.CostCodeSummary = new List<CostCodeSummaryRecord>();
            model.Phases = new List<Phase>();
            return Ok(model);
        }

        //[Route("/CostCodeSummary/{recnum}")]
        //[TypeFilter(typeof(CheckUserRecordAccessAttribute))]
        //public async Task<IActionResult> Index(string recnum)
        //{
        //    var vm = await GetCurrentCostSummaryViewModel(recnum);
        //    return View(vm);
        //}
        [HttpGet("api/[controller]/GetCurrentCostSummaryViewModel1", Name = "GetCurrentCostSummaryViewModel1")]
        public async Task<IActionResult> GetCurrentCostSummaryViewModel(string recnum,string costCode = null, string phase = "", string startDate = "", string endDate = "")
        {
            //private async Task<CurrentCostSummaryViewModel> GetCurrentCostSummaryViewModel(string recnum, List<string> costCode = null, string phase = "", string startDate = "", string endDate = "")
            {
                CurrentCostSummaryViewModel model = new CurrentCostSummaryViewModel();
                try
                {
                    // Initialize tasks
                    Task<CostCodeSummaryModel> costCodeSummaryRecordsTask = GetCostCodeSummaryRecords(recnum, costCode, phase, startDate, endDate);
                    Task<List<Job>> jobsTask = GetJobCodes();
                    Task<List<CostCode>> costCodesTask = GetCostCodesList(recnum);
                    Task<List<Phase>> phasesTask = GetPhasesList(recnum);

                    // Await completion of all tasks
                    await Task.WhenAll(costCodeSummaryRecordsTask, jobsTask, costCodesTask, phasesTask);

                    // Retrieve results
                    model.CostCodeSummaryModel = costCodeSummaryRecordsTask.Result;
                    model.Jobs = jobsTask.Result;
                    model.CostCodes = costCodesTask.Result;
                    model.Phases = phasesTask.Result;

                    var jobObj = model.Jobs.FirstOrDefault(x => x.Recnum.Equals(recnum, StringComparison.OrdinalIgnoreCase));
                    if (jobObj != null)
                    {
                        model.SelectedJobName = jobObj.JobName;
                        model.SelectedJobId = jobObj.Recnum;
                    }
                    return Ok(model);
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
        }

        //[HttpPost]
        //[Route("/CostCodeSummary/Submit")]
        //public async Task<IActionResult> Submit(CurrentCostSummaryViewModel model)
        //{

        //    var vm = await GetCurrentCostSummaryViewModel(model.SelectedJobId, model.SelectedCostCodeIds, model.SelectedPhaseId, model.StartDate, model.EndDate);
        //    if (vm == null)
        //    {
        //        vm.SelectedCostCodeIds = model.SelectedCostCodeIds;
        //        vm.SelectedPhaseId = model.SelectedPhaseId;
        //    }
        //    vm.StartDate = model.StartDate;
        //    vm.EndDate = model.EndDate;

        //    return View("Index", vm);
        //}
        [HttpGet("api/[controller]/GetCostCodesForReport/{jobID}", Name = "GetCostCodesForReport")]
        public async Task<IActionResult> GetCostCodesForReport(string jobId)
        {
            var costCodes = await GetCostCodesList(jobId);
            return Ok(costCodes);
        }
        [HttpGet("api/[controller]/GetJobCodesForReport", Name = "GetJobCodesForReport")]
        public async Task<IActionResult> GetJobCodesForReport()
        {
            var jobcodes = await GetJobCodes();
            return Ok(jobcodes);
        }
        //[HttpGet("api/[controller]/GetPhasesForReport/{jobID}", Name = "GetPhasesForReport")]
        //public async Task<IActionResult> GetPhasesForReport(string jobID)
        //{
        //    var phases = await GetPhases(jobID);
        //    return Ok(phases);
        //}
        //[HttpPost]
        //[Route("/CostCodeSummary/GetJobName")]
        [NonAction]
        public async Task<IActionResult> GetJobName(string recnum)
        {
            var jobs = await GetJobCodes();
            var jobObj = jobs.FirstOrDefault(x => x.Recnum.Equals(recnum, StringComparison.OrdinalIgnoreCase));

            return Ok(jobObj.JobName);
        }


        //[HttpPost]
        //[Route("/CostCodeSummary/GetPhases")]
        [HttpGet("api/[controller]/GetPhasesForReport/{jobID}", Name = "GetPhasesForReport")]
        public async Task<IActionResult> GetPhasesForReport(string jobID)
        {
            var phases = await GetPhasesList(jobID);
            return Ok(phases);
        }
        [NonAction]
        public async Task<CostCodeSummaryModel> GetCostCodeSummaryRecords(string recnum, string costCode="", string phase = "", string startDate = "", string endDate = "")
        {
            CostCodeSummaryModel costCodeSummaryModel = new CostCodeSummaryModel();
            List<CostCodeSummaryRecord> costCodeSummaryRecords = new List<CostCodeSummaryRecord>();

            string selectCostCodes = "";

            //if (costCode != null && costCode.Any())
            //{
            //    selectCostCodes = string.Join(",", costCode.Where(c => c != "select-all"));
            //}

            string query = "SPCostSummary";
            //string connectionString = _configuration.GetConnectionString("StowellConnection");
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
            {
                using (var command = new SqlCommand("dbo.SPCostSummary", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@JobNumber", recnum);
                    command.Parameters.AddWithValue("@costcode", costCode);
                    command.Parameters.AddWithValue("@startphase", phase);
                    command.Parameters.AddWithValue("@endphase", phase);
                    command.Parameters.AddWithValue("@StartDate", string.IsNullOrEmpty(startDate) ? DBNull.Value : startDate.Replace("-", ""));
                    command.Parameters.AddWithValue("@EndDate", string.IsNullOrEmpty(endDate) ? DBNull.Value : endDate.Replace("-", ""));
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
            return costCodeSummaryModel;
        }
        [NonAction]
        public async Task<CostCodeSummaryModel> GetCostCodeSummaryRecordsByModel(string recnum)
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

        [NonAction]
        public async Task<List<Job>> GetJobCodes()
        {
            List<Job> jobCodes = new List<Job>();

            string query = "select * from VWJobNumber order by recnum";
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
        [NonAction]
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
        [NonAction]
        private async Task<List<Phase>> GetPhasesList(string jobId)
        {
            List<Phase> records = new List<Phase>();

            // FIX (2026-09-23, Mike Smith - "phase not showing") - see ProjectOverviewController.GetPhasesList:
            // include phases added in the app (dbo.BidPhase), which Sage's jobphs never receives.
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
