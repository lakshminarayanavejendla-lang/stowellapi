using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
using System.Data;

namespace StowellCoAPI.Controllers
{
     [Route("api/[controller]")]
    [ApiController]
    public class JobSummaryController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JobSummaryController> _logger;

        public JobSummaryController(ILogger<JobSummaryController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        [HttpGet("GetJobCostSummaryRecordsWithRecnum/{recnum}")]
        public async Task<IActionResult> GetJobCostSummaryRecordsWithRecnum(string recnum = "")
        {
            var result = new List<CostCodeSummaryRecord>();
            try
            {
                string selectCostCodes = "";

                string query = "SPJobSummary";
                //string connectionString = _configuration.GetConnectionString("StowellConnection");
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
                {
                    using (var command = new SqlCommand("dbo.SPJobSummary", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        if (!string.IsNullOrEmpty(recnum))
                        {
                            command.Parameters.AddWithValue("@JobNumber", recnum);
                        }
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
                                    Remaining = reader.IsDBNull(reader.GetOrdinal("Remaining")) ? 0 : Convert.ToDecimal(reader["Remaining"])
                                    // Populate other properties as needed
                                };
                                result.Add(record);
                            }
                        }
                    }
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
        [HttpGet("GetJobCostSummaryRecords")]
        public async Task<IActionResult> GetJobCostSummaryRecords()
        {
            var result = new List<CostCodeSummaryRecord>();
            try
            {
                string selectCostCodes = "";

                string query = "SPJobSummary";
                //string connectionString = _configuration.GetConnectionString("StowellConnection");
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection")))
                {
                    using (var command = new SqlCommand("dbo.SPJobSummary", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
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
                                    Remaining = reader.IsDBNull(reader.GetOrdinal("Remaining")) ? 0 : Convert.ToDecimal(reader["Remaining"])
                                    // Populate other properties as needed
                                };
                                result.Add(record);
                            }
                        }
                    }
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
    }
}

