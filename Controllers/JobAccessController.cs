using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
using System.Data;

namespace StowellCoAPI.Controllers
{
    // Job access audit trail (new feature, not a Blazor port) - a standalone-page grid of
    // "who opened which job, and when". Mirrors CoQueueController.cs's SAGESBQ connection-string
    // pattern. LogAccess is called (fire-and-forget from the Angular side) by
    // ProjectOverviewComponent.ngOnInit; GetLog backs the un-navigated job-access-log page.
    [ApiController]
    public class JobAccessController : ControllerBase
    {
        private readonly ILogger<JobAccessController> _logger;
        private readonly IConfiguration _configuration;

        public JobAccessController(ILogger<JobAccessController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("api/JobAccess/LogAccess", Name = "LogJobAccess")]
        public async Task<IActionResult> LogAccess([FromBody] JobAccessLogRequestDto request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("JobAccess_Log", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@JobId", (object)request.JobId ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserEmail", (object)request.UserEmail ?? DBNull.Value);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return Ok(new { logged = true });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while logging job access.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while logging job access.", Details = ex.Message });
            }
        }

        [HttpGet("api/JobAccess/GetLog", Name = "GetJobAccessLog")]
        public async Task<IActionResult> GetLog()
        {
            var records = new List<JobAccessLogRecord>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("JobAccess_GetLog", connection) { CommandType = CommandType.StoredProcedure };

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new JobAccessLogRecord
                    {
                        Id = Convert.ToInt64(reader["Id"]),
                        JobId = reader["JobId"].ToString(),
                        JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                        UserEmail = reader["UserEmail"].ToString(),
                        AccessTime = Convert.ToDateTime(reader["AccessTime"])
                    });
                }

                return Ok(records);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the job access log.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the job access log.", Details = ex.Message });
            }
        }
    }
}
