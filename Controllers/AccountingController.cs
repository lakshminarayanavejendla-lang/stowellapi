using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using StowellCoAPI.DTO;
using StowellCoAPI.Models;
using StowellCoAPI.Services;
using System.Data;

namespace StowellCoAPI.Controllers
{
    [ApiController]
    public class AccountingController : ControllerBase
    {

        private readonly ILogger<AccountingController> _logger;
        private readonly IConfiguration _configuration;
        // private readonly GraphServiceClient _graphServiceClient;
        //private readonly UserInfoService _userInfoService;
        // private readonly UserInfoService _userInfoService;


        public AccountingController(ILogger<AccountingController> logger, IConfiguration configuration)
        //, GraphServiceClient graphServiceClient,
        //UserInfoService userInfoService)
        {
            _configuration = configuration;
            // _graphServiceClient = graphServiceClient;
            // _userInfoService = userInfoService;
            _logger = logger;
        }

        [HttpGet("api/[controller]/GetCostCodes/{filter}", Name = "GetCostCodes")]
        public List<CostCodeList> GetCostCodes(string filter)
        {
            var result = new List<CostCodeList>();
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("select CostCodeDescription FROM costcodelist where CostCodeDescription like @filter", conn))
                {
                    cmd.Parameters.AddWithValue("@filter", "%" + filter + "%");  // Add "%" around the filter
                    cmd.CommandType = CommandType.Text;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var record = new CostCodeList
                            {
                                Description = reader.IsDBNull(reader.GetOrdinal("CostCodeDescription")) ? string.Empty : reader["CostCodeDescription"].ToString()
                            };

                            result.Add(record);
                        }
                    }
                }
                return result;
            }
        }

        [HttpGet("api/[controller]/GetRecords/{email}", Name = "GetRecords")]
        public async Task<IActionResult> GetRecords(string email)
        {
            try
            {
                string viewName = "VwAccountingQueue";
                var result = new List<AccountingItem>();
                string cs = _configuration.GetConnectionString("SageSBQConnection");
                using var conn = new SqlConnection(cs);
                //string email = HttpContext.User.Identity.Name; //"p360admin@stowellinc.com"; //
                using var cmd = new SqlCommand($"SELECT ID, JobID,JobName,JobType, BidDate, Address, Submitter, BidStatus,StatusID FROM {viewName} where Submitter = @email order by JobID", conn);
                cmd.Parameters.AddWithValue("@email", email);
                conn.Open();
                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    result.Add(new AccountingItem
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("ID")),
                        JobID = reader.IsDBNull(reader.GetOrdinal("JobID")) ? string.Empty : reader["JobID"].ToString(),
                        JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                        JobType = reader.IsDBNull(reader.GetOrdinal("JobType")) ? string.Empty : reader["JobType"].ToString(),
                        BidDate = reader.GetDateTime(reader.GetOrdinal("BidDate")),
                        Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : reader["Address"].ToString(),
                        Submitter = reader.IsDBNull(reader.GetOrdinal("Submitter")) ? string.Empty : reader["Submitter"].ToString(),
                        BidStatus = reader.IsDBNull(reader.GetOrdinal("BidStatus")) ? string.Empty : reader["BidStatus"].ToString(),
                        StatusID = reader.IsDBNull(reader.GetOrdinal("StatusID")) ? string.Empty : reader["StatusID"].ToString()
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
        [HttpPost("api/[controller]/GetApproveBudgetData", Name = "GetApproveBudgetData")]
        public async Task<IActionResult> GetApproveBudgetData([FromBody] Phase input)
        {
            try
            {
                //CostCodeSummaryModel costCodeSummaryModel = new CostCodeSummaryModel();
                List<BudgetRecord> costCodeSummaryRecords = new List<BudgetRecord>();

                //string connectionString = _configuration.GetConnectionString("StowellConnection");
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
                {
                    using (var command = new SqlCommand("dbo.Budget_selectAccountingQueue", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        if (!string.IsNullOrEmpty(input.Recnum))
                        {
                            command.Parameters.AddWithValue("@jobid", input.Recnum);
                        }
                        if (!string.IsNullOrEmpty(input.PhaseNum))
                        {
                            command.Parameters.AddWithValue("@phasenum", input.PhaseNum);
                        }
                        connection.Open();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new BudgetRecord
                                {
                                    JobNumber = reader.IsDBNull(reader.GetOrdinal("recnum")) ? null : reader["recnum"].ToString(),
                                    CostCode = reader.IsDBNull(reader.GetOrdinal("cstcde")) ? null : reader["cstcde"].ToString(),
                                    CostCodeDescription = reader.IsDBNull(reader.GetOrdinal("cdenme")) ? null : reader["cdenme"].ToString(),
                                    TotalBudget = reader.IsDBNull(reader.GetOrdinal("ttlbdg")) ? 0 : Convert.ToDecimal(reader["ttlbdg"]),
                                    UpdateBudget = reader.IsDBNull(reader.GetOrdinal("ttlbdg")) ? 0 : Convert.ToDecimal(reader["ttlbdg"]),

                                    // Populate other properties as needed
                                };
                                costCodeSummaryRecords.Add(record);
                            }
                        }
                    }
                }
                return Ok(costCodeSummaryRecords);
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
        [HttpPost("api/[controller]/GetModifyBudgetData", Name = "GetModifyBudgetData")]
        public async Task<IActionResult> GetModifyBudgetData([FromBody] Phase input)
        {
            try
            {
                List<BudgetRecord> costCodeSummaryRecords = new List<BudgetRecord>();
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
                {
                    using (var command = new SqlCommand("dbo.GetSageJobCostBudgetTran", connection))
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
                                    TotalBudget = reader.IsDBNull(reader.GetOrdinal("Budget")) ? 0 : Convert.ToDecimal(reader["Budget"])
                                    // Populate other properties as needed
                                };
                                costCodeSummaryRecords.Add(record);
                            }
                        }
                    }
                }
                //costCodeSummaryModel.CostCodeSummary = costCodeSummaryRecords;
                return Ok(costCodeSummaryRecords);
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
        [HttpPost("api/[controller]/BudgetApproveRequest", Name = "BudgetApproveRequest")]
        public async Task<IActionResult> BudgetApproveRequest([FromBody] Phase input)
        {
            try
            {
                if (input == null || string.IsNullOrEmpty(input.Recnum) || string.IsNullOrEmpty(input.PhaseNum))
                    return BadRequest(new
                    {
                        success = false,
                        message = "JobId and PhaseNum are mandatory."
                    });

                // string email =HttpContext.User.Identity.Name;//"p360admin@stowellinc.com"; //
                //string connectionString = _configuration.GetConnectionString("StowellConnection");
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
                {
                    using (var command = new SqlCommand("dbo.sp_ApproveBudgetTransaction", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        if (!string.IsNullOrEmpty(input.Recnum))
                        {
                            command.Parameters.AddWithValue("@jobid", input.Recnum);
                        }
                        if (!string.IsNullOrEmpty(input.PhaseNum))
                        {
                            command.Parameters.AddWithValue("@phasenum", input.PhaseNum);
                        }

                        command.Parameters.AddWithValue("@BudgetApprovedBy", input.Email);

                        connection.Open();

                        // FIX (2026-09-16) - was ExecuteNonQueryAsync(), which discards
                        // sp_ApproveBudgetTransaction's own result set entirely. That proc
                        // deliberately catches errors from EXEC sp_BudgetTran internally (e.g.
                        // "Cost code does not exist in CostCodeList") and returns them as a
                        // SELECT (ErrorNumber/ErrorMessage/RowsAffected=0) instead of raising a
                        // .NET-visible SqlException - so ExecuteNonQueryAsync saw no exception
                        // and this endpoint always reported "Budget Approved Sucessfully!" even
                        // when nothing was actually approved. Now reads that result set and
                        // reports the real outcome.
                        string errorMessage = null;
                        int rowsAffected = 0;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int errorMsgOrdinal;
                                try { errorMsgOrdinal = reader.GetOrdinal("ErrorMessage"); }
                                catch (IndexOutOfRangeException) { errorMsgOrdinal = -1; }

                                if (errorMsgOrdinal >= 0 && !reader.IsDBNull(errorMsgOrdinal))
                                {
                                    errorMessage = reader.GetString(errorMsgOrdinal);
                                }

                                int rowsOrdinal;
                                try { rowsOrdinal = reader.GetOrdinal("RowsAffected"); }
                                catch (IndexOutOfRangeException) { rowsOrdinal = -1; }

                                if (rowsOrdinal >= 0 && !reader.IsDBNull(rowsOrdinal))
                                {
                                    rowsAffected = Convert.ToInt32(reader.GetValue(rowsOrdinal));
                                }
                            }
                        }

                        if (errorMessage != null)
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, new
                            {
                                success = false,
                                message = errorMessage
                            });
                        }

                        if (rowsAffected == 0)
                        {
                            return Ok(new
                            {
                                success = false,
                                data = input,
                                message = "No pending budget items found to approve for this job/phase."
                            });
                        }
                    }
                    return Ok(new
                    {
                        success = true,
                        data = input,
                        message = "Budget Approved Sucessfully!"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Approve Budget failed.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Approve Budget failed."
                });
            }
        }
        [HttpPost("api/[controller]/BudgetRejectRequest", Name = "BudgetRejectRequest")]
        public async Task<IActionResult> BudgetRejectRequest([FromBody] Phase input)
        {
            try
            {
                if (input == null || string.IsNullOrEmpty(input.Recnum))
                    return BadRequest(new
                    {
                        success = false,
                        message = "JobId and PhaseNum are mandatory."
                    });

                // string email = HttpContext.User.Identity.Name; //"p360admin@stowellinc.com"; //
                //string connectionString = _configuration.GetConnectionString("StowellConnection");
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
                {
                    using (var command = new SqlCommand("dbo.sp_RejectBudgetTransaction", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        if (!string.IsNullOrEmpty(input.Recnum))
                        {
                            command.Parameters.AddWithValue("@jobid", input.Recnum);
                        }
                        if (!string.IsNullOrEmpty(input.PhaseNum))
                        {
                            command.Parameters.AddWithValue("@phasenumber", input.PhaseNum);
                        }

                        command.Parameters.AddWithValue("@RejectedBy", input.Email);

                        connection.Open();
                        await command.ExecuteNonQueryAsync();
                    }
                    return Ok(new
                    {
                        success = true,
                        data = input,
                        Message = "Budget Rejected Sucessfully!"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reject Budget failed.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Reject Budget failed."
                });
            }
        }
        [HttpGet("api/[controller]/GetUserName/{email}")]
        public async Task<IActionResult> GetUserName(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required");

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            try
            {
                const string query = @"
     SELECT COALESCE(Bidder, Email, '')
     FROM VwGetBidderName
     WHERE Email = @email";

                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@email", email);

                await connection.OpenAsync();
                var userName = await command.ExecuteScalarAsync();

                return Ok(userName?.ToString() ?? string.Empty);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error in GetUserName");
                return StatusCode(500, "Database connection failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetUserName");
                return StatusCode(500, "Unexpected server error.");
            }
        }
        [HttpPost("api/[controller]/SubmitToSage", Name = "SubmitToSage")]
        public async Task<IActionResult> SubmitToSage([FromBody] BidInfoDto bid)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .Select(e => new { Field = e.Key, Errors = e.Value.Errors.Select(x => x.ErrorMessage) });
                    return BadRequest(errors);
                }

                if (bid == null || string.IsNullOrWhiteSpace(bid.JobID))
                    return BadRequest("Invalid bid data");

                await UpdateBidData(bid);
                var result = new BidInfoDto();
                //string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                //using (var conn = new SqlConnection(connectionString))
                //{
                //    conn.Open();
                //    using (var cmd = new SqlCommand("InsertNewSageJob", conn))
                //    {
                //        cmd.CommandType = CommandType.StoredProcedure;
                //        cmd.Parameters.AddWithValue("@JobID", bid.JobID);
                //        await cmd.ExecuteNonQueryAsync();
                //    }
                //}
                //string returnMessage = null;
                bool isSuccess = true;
                //string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                //using (var conn = new SqlConnection(connectionString))
                //{
                //    conn.Open();
                //    using (var cmd = new SqlCommand("InsertNewSageJob", conn))
                //    {
                //        cmd.CommandType = CommandType.StoredProcedure;
                //        cmd.Parameters.AddWithValue("@JobID", bid.JobID);

                //        using (var reader = await cmd.ExecuteReaderAsync())
                //        {
                //            if (reader.HasRows && await reader.ReadAsync())
                //            {
                //                returnMessage = reader["ReturnMessage"].ToString();
                //                isSuccess = Convert.ToInt32(reader["IsSuccess"]) == 1;
                //            }
                //        }
                //    }
                //}
                if (isSuccess)
                {
                    string folderName = $"{bid.JobName} - {bid.JobID}";
                    string parentFolder = _configuration["SharepointParentFolder"];
                    string fullfoldername = $"{parentFolder}/{folderName}";
                    _logger.LogInformation($"Job - {bid.JobID} created successfully");
                    await CopySharePointFolderAsync(folderName);
                    await UpdateFolder(bid.JobID, fullfoldername);
                }
                //var folderStructureResult = CreateNewFolderStrcuture(folderName);

                //if (folderStructureResult is JsonResult jsonResult)
                //{
                //    var folderStructureResponse = jsonResult.Value as dynamic;// The response from SharePointController

                //    // Process the result as needed
                //    // For example, if folder structure creation was successful, proceed with job creation
                //    if (folderStructureResponse != null)
                //    {
                //        bool success = folderStructureResponse.success;
                //        string message = folderStructureResponse.message;
                //        string parentFolder = _configuration["SharepointParentFolder"];
                //        string fullfoldername = $"{parentFolder}/{folderName}";
                //        UpdateFolder(bid.JobID, fullfoldername);
                //        _logger.LogInformation($"Job - {bid.JobID} created successfully, Folder Creation - {success}");
                //        return Ok(new { success = true, message = $"Job - {bid.JobID} created successfully, Folder Creation - {success}" });

                //    }
                //}
                //if (!isSuccess)
                //{
                //    // already exists
                //    return Ok(new { success = true, message = returnMessage, IsExist = true });
                //}
                return Ok(new { success = true, message = $"Job - {bid.JobID} created successfully.", IsExist = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Submit to Sage failed.");
                return Ok(new { success = false, message = ex.Message });
            }

        }

        //private async Task UpdateFolder(string jobId, string folderName)
        //{
        //    try
        //    {
        //        string connectionString = _configuration.GetConnectionString("SageSBQConnection");
        //        using (var conn = new SqlConnection(connectionString))
        //        {
        //            conn.Open();

        //            using (var cmd = new SqlCommand("Jobs_insert", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@JobID", jobId);
        //                cmd.Parameters.AddWithValue("@folderlocation", folderName);
        //                cmd.ExecuteNonQuery();
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching client by ID");
        //    }
        //}
        private async Task UpdateFolder(string jobId, string folderName)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();  // Open connection asynchronously

                    using (var cmd = new SqlCommand("Jobs_insert", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@JobID", jobId); // Ensure this matches parameter name in the procedure
                        cmd.Parameters.AddWithValue("@folderlocation", folderName); // Make sure this is correct too

                        await cmd.ExecuteNonQueryAsync();  // Execute asynchronously
                    }
                }
            }
            catch(SqlException ex)
            {
                // 🔥 THIS captures RAISERROR messages
                string sqlMessage = ex.Message;

                // You can log it
                _logger.LogError(ex, "Jobs_insert failed");

                // Or return to UI / API
                // throw new Exception(sqlMessage);
            }
            catch (Exception ex)
            {
                // Improved logging message for clarity
                _logger.LogError(ex, "Error updating folder location for JobID: {JobId}", jobId);
            }
        }
        private async Task<(bool Success, string Message)> UpdateBidData(BidInfoDto bid)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // -------------------- Bids_Update --------------------
                await using (var cmd = new SqlCommand("Bids_Update", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", bid.JobID);
                    cmd.Parameters.AddWithValue("@Bidder", bid.Bidder);
                    cmd.Parameters.AddWithValue("@Phase", bid.Phase);
                    cmd.Parameters.AddWithValue("@BidStatus", bid.BidStatus);
                    cmd.Parameters.AddWithValue("@Division", bid.Division);
                    cmd.Parameters.AddWithValue("@Department", bid.Department);
                    cmd.Parameters.AddWithValue("@JobType", bid.JobType);
                    cmd.Parameters.AddWithValue("@JobName", bid.JobName);
                    cmd.Parameters.AddWithValue("@ShortName", bid.ShortName);
                    cmd.Parameters.AddWithValue("@ContractNumber", bid.ContractNumber);
                    cmd.Parameters.AddWithValue("@ContractDate", !string.IsNullOrEmpty(bid.ContractDate) ? Convert.ToDateTime(bid.ContractDate) : "");
                    cmd.Parameters.AddWithValue("@ContractAmount", bid.ContractAmount ?? 0.0m);
                    cmd.Parameters.AddWithValue("@EstStartDate", !string.IsNullOrEmpty(bid.EstStartDate) ? Convert.ToDateTime(bid.EstStartDate) : "");
                    cmd.Parameters.AddWithValue("@EstCompletionDate", !string.IsNullOrEmpty(bid.EstCompletionDate) ? Convert.ToDateTime(bid.EstCompletionDate) : "");
                    await cmd.ExecuteNonQueryAsync();
                }

                if (bid.ProjectLocation != null)
                {
                    await using (var cmd = new SqlCommand("ProjectLocation_Update", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@JobID", bid.ProjectLocation.JobID);
                        cmd.Parameters.AddWithValue("@Jobsite", bid.ProjectLocation.Jobsite);
                        cmd.Parameters.AddWithValue("@Address", bid.ProjectLocation.Address);
                        cmd.Parameters.AddWithValue("@Address2", bid.ProjectLocation.Address2);
                        cmd.Parameters.AddWithValue("@City", bid.ProjectLocation.City);
                        cmd.Parameters.AddWithValue("@State", bid.ProjectLocation.State);
                        cmd.Parameters.AddWithValue("@SalesTaxDistrict", bid.ProjectLocation.SalesTaxDistrict);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                if (bid.ContractorInfo != null)
                {
                    await using (var cmd = new SqlCommand("ContractorInfo_Update", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@JobID", bid.ContractorInfo.JobID);
                        cmd.Parameters.AddWithValue("@Client", bid.ContractorInfo.Client);
                        cmd.Parameters.AddWithValue("@Address", bid.ContractorInfo.Address);
                        cmd.Parameters.AddWithValue("@Address2", bid.ContractorInfo.Address2);
                        cmd.Parameters.AddWithValue("@City", bid.ContractorInfo.City);
                        cmd.Parameters.AddWithValue("@State", bid.ContractorInfo.State);
                        cmd.Parameters.AddWithValue("@zip", bid.ContractorInfo.Zip);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                if (bid.ProjectInfo != null)
                {
                    await using (var cmd = new SqlCommand("ProjectInfo_Update", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@JobID", bid.ProjectInfo.JobID);
                        cmd.Parameters.AddWithValue("@InsuranceType", bid.ProjectInfo.InsuranceType);
                        cmd.Parameters.AddWithValue("@Bonded", bid.ProjectInfo.Bonded);
                        cmd.Parameters.AddWithValue("@TaxExcempt", bid.ProjectInfo.TaxExcempt);
                        cmd.Parameters.AddWithValue("@InvoiceSubmittal", bid.ProjectInfo.InvoiceSubmittal);
                        cmd.Parameters.AddWithValue("@CertifiedPayroll", bid.ProjectInfo.CertifiedPayroll);
                        cmd.Parameters.AddWithValue("@NetTermsDate", !string.IsNullOrEmpty(bid.ProjectInfo.NetTermsDate) ? Convert.ToDateTime(bid.ProjectInfo.NetTermsDate) : "");
                        cmd.Parameters.AddWithValue("@RetainagePerc", bid.ProjectInfo.RetainagePerc ?? 0.0m);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return (true, "Project details updated successfully.");
            }
            catch (SqlException ex)
            {
                // 🔥 THIS captures RAISERROR messages
                string sqlMessage = ex.Message;

                // You can log it
                _logger.LogError(ex, "UpdateBidData failed");
                return (false, $"{sqlMessage}");
                // Or return to UI / API
                // throw new Exception(sqlMessage);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to update the project details.");
                _logger.LogError(ex, ex.Message);
            }
        }
        //private void UpdateBidData(BidInfoDto bid)
        //{
        //    try
        //    {
        //        string connectionString = _configuration.GetConnectionString("SageSBQConnection");
        //        using (var conn = new SqlConnection(connectionString))
        //        {
        //            conn.Open();

        //            using (var cmd = new SqlCommand("Bids_Update", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@JobID", bid.JobID);
        //                cmd.Parameters.AddWithValue("@Bidder", bid.Bidder);
        //                cmd.Parameters.AddWithValue("@Phase", bid.Phase);
        //                cmd.Parameters.AddWithValue("@BidStatus", bid.BidStatus);
        //                cmd.Parameters.AddWithValue("@Division", bid.Division);
        //                cmd.Parameters.AddWithValue("@Department", bid.Department);
        //                cmd.Parameters.AddWithValue("@JobType", bid.JobType);
        //                cmd.Parameters.AddWithValue("@JobName", bid.JobName);
        //                cmd.Parameters.AddWithValue("@ShortName", bid.ShortName);
        //                cmd.Parameters.AddWithValue("@ContractNumber", bid.ContractNumber);
        //                cmd.Parameters.AddWithValue("@ContractDate", !string.IsNullOrEmpty(bid.ContractDate) ? Convert.ToDateTime(bid.ContractDate) : "");
        //                cmd.Parameters.AddWithValue("@ContractAmount", bid.ContractAmount ?? 0.0m);
        //                cmd.Parameters.AddWithValue("@EstStartDate", !string.IsNullOrEmpty(bid.EstStartDate) ? Convert.ToDateTime(bid.EstStartDate) : "");
        //                cmd.Parameters.AddWithValue("@EstCompletionDate",!string.IsNullOrEmpty(bid.EstCompletionDate)? Convert.ToDateTime(bid.EstCompletionDate):"");
        //                cmd.ExecuteNonQuery();
        //            }

        //            using (var cmd = new SqlCommand("ProjectLocation_Update", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@JobID", bid.ProjectLocation.JobID);
        //                cmd.Parameters.AddWithValue("@Jobsite", bid.ProjectLocation.Jobsite);
        //                cmd.Parameters.AddWithValue("@Address", bid.ProjectLocation.Address);
        //                cmd.Parameters.AddWithValue("@Address2", bid.ProjectLocation.Address2);
        //                cmd.Parameters.AddWithValue("@City", bid.ProjectLocation.City);
        //                cmd.Parameters.AddWithValue("@State", bid.ProjectLocation.State);
        //                cmd.Parameters.AddWithValue("@SalesTaxDistrict", bid.ProjectLocation.SalesTaxDistrict);
        //                cmd.ExecuteNonQuery();
        //            }

        //            using (var cmd = new SqlCommand("ContractorInfo_Update", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@JobID", bid.ContractorInfo.JobID);
        //                cmd.Parameters.AddWithValue("@Client", bid.ContractorInfo.Client);
        //                cmd.Parameters.AddWithValue("@Address", bid.ContractorInfo.Address);
        //                cmd.Parameters.AddWithValue("@Address2", bid.ContractorInfo.Address2);
        //                cmd.Parameters.AddWithValue("@City", bid.ContractorInfo.City);
        //                cmd.Parameters.AddWithValue("@State", bid.ContractorInfo.State);
        //                cmd.Parameters.AddWithValue("@zip", bid.ContractorInfo.Zip);
        //                cmd.ExecuteNonQuery();
        //            }

        //            using (var cmd = new SqlCommand("ProjectInfo_Update", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@JobID", bid.ProjectInfo.JobID);
        //                cmd.Parameters.AddWithValue("@InsuranceType", bid.ProjectInfo.InsuranceType);
        //                cmd.Parameters.AddWithValue("@Bonded", bid.ProjectInfo.Bonded);
        //                cmd.Parameters.AddWithValue("@TaxExcempt", bid.ProjectInfo.TaxExcempt);
        //                cmd.Parameters.AddWithValue("@InvoiceSubmittal", bid.ProjectInfo.InvoiceSubmittal);
        //                cmd.Parameters.AddWithValue("@CertifiedPayroll", bid.ProjectInfo.CertifiedPayroll);
        //                cmd.Parameters.AddWithValue("@NetTermsDate", !string.IsNullOrEmpty(bid.ProjectInfo.NetTermsDate) ? Convert.ToDateTime(bid.ProjectInfo.NetTermsDate) : "");
        //                cmd.Parameters.AddWithValue("@RetainagePerc", bid.ProjectInfo.RetainagePerc ?? 0.0m);
        //                cmd.ExecuteNonQuery();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching client by ID");
        //    }
        //}
        //private IActionResult CreateNewFolderStrcuture(string folderName)
        //{
        //    try
        //    {
        //        string siteUrl = "https://stowell.sharepoint.com/sites/Stowell/";
        //        string clientId = "117ef92b-4957-4495-8386-337a87f712d7";
        //        string clientSecret = "<redacted>";                
        //        string sourceFolderUrl = _configuration["SourceFolder"];
        //        string parentFolder = _configuration["SharepointParentFolder"];

        //        // Initialize SharePoint context
        //        using (var context = new AuthenticationManager().GetACSAppOnlyContext(siteUrl, clientId, clientSecret))
        //        //using (var context = new AuthenticationManager().GetContext(siteUrl, clientId, clientSecret, tenantId))
        //        {
        //            Web web = context.Web;

        //            // Get the source folder
        //            Folder sourceFolder = web.GetFolderByServerRelativeUrl(sourceFolderUrl);
        //            context.Load(sourceFolder);
        //            context.Load(sourceFolder.Folders); // Load the subfolders
        //            context.ExecuteQuery();

        //            Folder parent = web.GetFolderByServerRelativeUrl(parentFolder);
        //            context.Load(parent);
        //            context.ExecuteQuery();

        //            // Create a new folder under the parent folder
        //            string newFolderUrl = $"{parentFolder}/{folderName}";
        //            Folder newFolder = parent.Folders.Add(newFolderUrl);
        //            context.Load(newFolder);
        //            context.ExecuteQuery();

        //            // Create destination folder
        //            Folder destinationFolder = web.GetFolderByServerRelativeUrl(newFolderUrl);
        //            context.Load(destinationFolder);
        //            context.ExecuteQuery();

        //            // Recursively copy the folder structure
        //            CopySubfoldersRecursive(sourceFolder, destinationFolder, context);

        //            return Ok(new { success = true, message = "Folder structure copied successfully!" });
        //        }
        //    }
        //    catch (ServerException ex)
        //    {
        //        return Ok(new { success = false, message = $"SharePoint error: {ex.Message}" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok(new { success = false, message = $"An error occurred: {ex.Message}" });
        //    }
        //}
        //private void CopySubfoldersRecursive(Folder sourceFolder, Folder destinationFolder, ClientContext context)
        //{
        //    // Load subfolders for the source folder
        //    context.Load(sourceFolder.Folders);
        //    context.ExecuteQuery();

        //    foreach (Folder subFolder in sourceFolder.Folders)
        //    {
        //        // Create the new folder under the destination
        //        Folder newFolder = destinationFolder.Folders.Add(subFolder.Name);
        //        context.Load(newFolder);
        //        context.ExecuteQuery();

        //        // Recursively copy the subfolders of the current subfolder
        //        CopySubfoldersRecursive(subFolder, newFolder, context);
        //    }
        //}
        [HttpGet("api/[controller]/TestCreateFolder")]
        public async Task<IActionResult> TestCreateFolder()
        {
            await CopySharePointFolderAsync(
    
    destinationFolderName: "Test Project"
//sourceFolderPath: "Shared Documents/04-Project Management/z-Job Template",
//parentFolderPath: "Shared Documents/04-Project Management/Main Jobs Folder/z-Sandbox Testing"
);
            return Ok(new { success = true, message = $"Folder created successfully." });

        }
        //    private async Task CopySharePointFolderAsync(
        //        string tenantId,
        //        string clientId,
        //        string clientSecret,
        //        string siteHostName,      // "stowell.sharepoint.com"
        //        string sitePath,          // "/sites/Stowell"
        //        string sourceFolderPath,  // "Shared Documents/SourceFolder"
        //        string parentFolderPath   // "Shared Documents/04-Project Management/Main Jobs Folder/z-Sandbox Testing"
        //    )
        //    {
        //        try
        //        {
        //            // 1. Authenticate
        //            var credential = new ClientSecretCredential(
        //                tenantId,
        //                clientId,
        //                clientSecret
        //            );

        //            var graphClient = new GraphServiceClient(credential);

        //            // 2. Get SharePoint Site (v4 syntax)
        //            var site = await graphClient
        //                .Sites
        //                .GetByPath(sitePath, siteHostName)
        //                .Request()
        //                .GetAsync();

        //            if (site == null)
        //                throw new Exception("Site not found.");
        //            var rootItems = await graphClient
        //.Sites[site.Id]
        //.Drive
        //.Root
        //.Children
        //.Request()
        //.GetAsync();

        //            foreach (var item in rootItems)
        //            {
        //                Console.WriteLine(item.Name);
        //            }

        //            // 3. Get Source Folder
        //            var sourceFolder = await graphClient
        //                .Sites[site.Id]
        //                .Drive
        //                .Root
        //                .ItemWithPath(sourceFolderPath)
        //                .Request()
        //                .GetAsync();

        //            if (sourceFolder == null)
        //                throw new Exception("Source folder not found.");

        //            // 4. Get Parent Folder
        //            var parentFolder = await graphClient
        //                .Sites[site.Id]
        //                .Drive
        //                .Root
        //                .ItemWithPath(parentFolderPath)
        //                .Request()
        //                .GetAsync();

        //            if (parentFolder == null)
        //                throw new Exception("Parent folder not found.");

        //            // 5. Create Destination Folder (same name as source)
        //            var newFolder = new DriveItem
        //            {
        //                Name = sourceFolder.Name,
        //                Folder = new Folder(),
        //                AdditionalData = new Dictionary<string, object>
        //    {
        //        { "@microsoft.graph.conflictBehavior", "rename" }
        //    }
        //            };

        //            var destinationFolder = await graphClient
        //                .Sites[site.Id]
        //                .Drive
        //                .Items[parentFolder.Id]
        //                .Children
        //                .Request()
        //                .AddAsync(newFolder);

        //            // 6. Get Files from Source Folder
        //            var sourceItems = await graphClient
        //                .Sites[site.Id]
        //                .Drive
        //                .Items[sourceFolder.Id]
        //                .Children
        //                .Request()
        //                .GetAsync();

        //            // 7. Copy Files
        //            foreach (var item in sourceItems)
        //            {
        //                if (item.File != null)
        //                {
        //                    await graphClient
        //                        .Sites[site.Id]
        //                        .Drive
        //                        .Items[item.Id]
        //                        .Copy(
        //                            name: item.Name,
        //                            parentReference: new ItemReference
        //                            {
        //                                Id = destinationFolder.Id
        //                            })
        //                        .Request()
        //                        .PostAsync();
        //                }
        //            }
        //        }
        //        catch(Exception ex)
        //        {
        //            _logger.LogError(ex, ex.Message);
        //        }
        //    }
        private async Task CopySharePointFolderAsync1(
        string tenantId,
        string clientId,
        string clientSecret,
        string siteHostName,      // "stowell.sharepoint.com"
        string sitePath,          // "/sites/Stowell"
        string sourceFolderPath,  // "04-Project Management/z-Job Template"
        string parentFolderPath,  // "04-Project Management/Main Jobs Folder/z-Sandbox Testing"
        string destinationFolderName // "Test Project"
    )
        {
            try
            {
                // 1. Authenticate (App-only)
                var credential = new ClientSecretCredential(
                    tenantId,
                    clientId,
                    clientSecret
                );

                var graphClient = new GraphServiceClient(credential);

                // 2. Get SharePoint Site
                var site = await graphClient
                    .Sites
                    .GetByPath(sitePath, siteHostName)
                    .Request()
                    .GetAsync();

                // 3. Get Source Folder (z-Job Template)
                var sourceFolder = await graphClient
                    .Sites[site.Id]
                    .Drive
                    .Root
                    .ItemWithPath(sourceFolderPath)
                    .Request()
                    .GetAsync();

                // 4. Get Parent Folder (z-Sandbox Testing)
                var parentFolder = await graphClient
                    .Sites[site.Id]
                    .Drive
                    .Root
                    .ItemWithPath(parentFolderPath)
                    .Request()
                    .GetAsync();

                // 5. Create Destination Folder (Test Project)
                var newFolder = new DriveItem
                {
                    Name = destinationFolderName,
                    Folder = new Folder(),
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "rename" }
                    }
                };

                var destinationFolder = await graphClient
                    .Sites[site.Id]
                    .Drive
                    .Items[parentFolder.Id]
                    .Children
                    .Request()
                    .AddAsync(newFolder);

                // 6. Get Files from Source Folder
                var sourceItems = await graphClient
                    .Sites[site.Id]
                    .Drive
                    .Items[sourceFolder.Id]
                    .Children
                    .Request()
                    .GetAsync();

                // 7. Copy Files to Destination Folder
                foreach (var item in sourceItems)
                {
                    if (item.File != null)
                    {
                        await graphClient
                            .Sites[site.Id]
                            .Drive
                            .Items[item.Id]
                            .Copy(
                                name: item.Name,
                                parentReference: new ItemReference
                                {
                                    Id = destinationFolder.Id
                                })
                            .Request()
                            .PostAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
        private async Task CopySharePointFolderAsync(string destinationFolderName)
        {
            var clientId = _configuration["SharePoint:ClientId"];
            var tenantId = _configuration["SharePoint:TenantId"];
            var clientSecret = _configuration["SharePoint:ClientSecret"];
            var siteHostName = _configuration["SharePoint:SiteHostName"];
            var sitePath = _configuration["SharePoint:SitePath"];
            var sourceFolderPath = _configuration["SharePoint:SourceFolderPath"];
            var parentFolderPath= _configuration["SharePoint:ParentFolderPath"];
            try
            {
                // 1. Authenticate (App-only)
                var credential = new ClientSecretCredential(
                    tenantId,
                    clientId,
                    clientSecret
                );

                var graphClient = new GraphServiceClient(credential);

                // 2. Get SharePoint Site
                var site = await graphClient
                    .Sites
                    .GetByPath(sitePath, siteHostName)
                    .Request()
                    .GetAsync();

                // 3. Get Source Folder
                var sourceFolder = await graphClient
                    .Sites[site.Id]
                    .Drive
                    .Root
                    .ItemWithPath(sourceFolderPath)
                    .Request()
                    .GetAsync();

                // 4. Get Parent Folder
                var parentFolder = await graphClient
                    .Sites[site.Id]
                    .Drive
                    .Root
                    .ItemWithPath(parentFolderPath)
                    .Request()
                    .GetAsync();

                // 5. Get or Create Destination Folder
                DriveItem destinationFolder;
                var destinationFolderPath = $"{parentFolderPath}/{destinationFolderName}";

                try
                {
                    destinationFolder = await graphClient
                        .Sites[site.Id]
                        .Drive
                        .Root
                        .ItemWithPath(destinationFolderPath)
                        .Request()
                        .GetAsync();
                }
                catch (ServiceException ex) when
                    (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var newFolder = new DriveItem
                    {
                        Name = destinationFolderName,
                        Folder = new Folder()
                    };

                    destinationFolder = await graphClient
                        .Sites[site.Id]
                        .Drive
                        .Items[parentFolder.Id]
                        .Children
                        .Request()
                        .AddAsync(newFolder);
                }

                // 6. Copy everything recursively
                await CopyDriveItemsRecursiveAsync(
                    graphClient,
                    site.Id,
                    sourceFolder.Id,
                    destinationFolder.Id
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        //        private async Task CopyDriveItemsRecursiveAsync(
        //    GraphServiceClient graphClient,
        //    string siteId,
        //    string sourceFolderId,
        //    string destinationFolderId
        //)
        //        {
        //            var children = await graphClient
        //                .Sites[siteId]
        //                .Drive
        //                .Items[sourceFolderId]
        //                .Children
        //                .Request()
        //                .GetAsync();

        //            while (true)
        //            {
        //                foreach (var item in children)
        //                {
        //                    // FILE
        //                    if (item.File != null)
        //                    {
        //                        await graphClient
        //                            .Sites[siteId]
        //                            .Drive
        //                            .Items[item.Id]
        //                            .Copy(
        //                                name: item.Name,
        //                                parentReference: new ItemReference
        //                                {
        //                                    Id = destinationFolderId
        //                                })
        //                            .Request()
        //                            .PostAsync();
        //                    }
        //                    // FOLDER
        //                    else if (item.Folder != null)
        //                    {
        //                        var folderToCreate = new DriveItem
        //                        {
        //                            Name = item.Name,
        //                            Folder = new Folder()
        //                        };

        //                        var createdFolder = await graphClient
        //                            .Sites[siteId]
        //                            .Drive
        //                            .Items[destinationFolderId]
        //                            .Children
        //                            .Request()
        //                            .AddAsync(folderToCreate);

        //                        await CopyDriveItemsRecursiveAsync(
        //                            graphClient,
        //                            siteId,
        //                            item.Id,
        //                            createdFolder.Id
        //                        );
        //                    }
        //                }

        //                if (children.NextPageRequest == null)
        //                    break;

        //                children = await children.NextPageRequest.GetAsync();
        //            }
        //        }
        private async Task CopyDriveItemsRecursiveAsync(
            GraphServiceClient graphClient,
            string siteId,
            string sourceFolderId,
            string destinationFolderId
        )
        {
            var children = await graphClient
                .Sites[siteId]
                .Drive
                .Items[sourceFolderId]
                .Children
                .Request()
                .GetAsync();

            while (true)
            {
                foreach (var item in children)
                {
                    // FILE: Copy files to the destination
                    if (item.File != null)
                    {
                        await graphClient
                            .Sites[siteId]
                            .Drive
                            .Items[item.Id]
                            .Copy(
                                name: item.Name,
                                parentReference: new ItemReference
                                {
                                    Id = destinationFolderId
                                })
                            .Request()
                            .PostAsync();
                    }
                    // FOLDER: Check if the folder already exists before creating it
                    else if (item.Folder != null)
                    {
                        var destinationFolderPath = $"{destinationFolderId}/{item.Name}";

                        // Check if the folder already exists in the destination folder
                        DriveItem existingFolder = null;
                        try
                        {
                            existingFolder = await graphClient
                                .Sites[siteId]
                                .Drive
                                .Items[destinationFolderId]
                                .Children[item.Name]
                                .Request()
                                .GetAsync();
                        }
                        catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            // Folder does not exist, so we create it
                            var folderToCreate = new DriveItem
                            {
                                Name = item.Name,
                                Folder = new Folder()
                            };

                            existingFolder = await graphClient
                                .Sites[siteId]
                                .Drive
                                .Items[destinationFolderId]
                                .Children
                                .Request()
                                .AddAsync(folderToCreate);
                        }

                        // Recursively copy the items from the current folder
                        await CopyDriveItemsRecursiveAsync(
                            graphClient,
                            siteId,
                            item.Id,
                            existingFolder.Id
                        );
                    }
                }

                // If there are more pages of children, move to the next page
                if (children.NextPageRequest == null)
                    break;

                children = await children.NextPageRequest.GetAsync();
            }
        }

    }
}