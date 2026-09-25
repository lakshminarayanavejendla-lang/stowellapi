using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using StowellCoAPI.DTO;
using StowellCoApp.Util;
using System.Data;

namespace StowellCoAPI.Controllers
{
    [ApiController]
    public class CostCodeController : ControllerBase
    {
        private readonly ILogger<CostCodeController> _logger;
        private readonly IConfiguration _configuration;
        //private readonly GraphServiceClient _graphServiceClient;
       // private readonly UserInfoService _userInfoService;


        public CostCodeController(ILogger<CostCodeController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("api/[controller]/GetCostCodeRecords", Name = "GetCostCodeRecords")]
        public async Task<IActionResult> GetCostCodeRecords()
        {
            List<CostCodeRecord> costCodeRecords = new List<CostCodeRecord>();

            string connectionString = _configuration.GetConnectionString("StowellConnection");
            string query = "SELECT recnum, jobnme, addrs1+''+addrs2 AS Address, ctynme AS City, state_ AS State, zipcde AS ZipCode FROM actrec order by recnum";
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
        [HttpGet("api/[controller]/GetCostCodeRecordsByStatus/{status}", Name = "GetCostCodeRecordsByStatus")]
        public List<CostCodeRecord> GetCostCodeRecordsByStatus(int status)
        {
            List<CostCodeRecord> costCodeRecords = new List<CostCodeRecord>();

            string connectionString = _configuration.GetConnectionString("StowellConnection");
            string query = "SELECT recnum, jobnme, addrs1+''+addrs2 AS Address, ctynme AS City, state_ AS State, zipcde AS ZipCode FROM actrec WHERE status = @Status";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Status", status);

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

            return costCodeRecords;
        }
        [HttpGet("api/[controller]/GetBids", Name = "GetBids")]
        public async Task<IActionResult> GetBids()
        {
            var result = new List<BidItem>();
            string cs = _configuration.GetConnectionString("SageSBQConnection");
            try
            {
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand($"SELECT ID, JobID, BidDate, Address, Submitter, BidStatus FROM VwPendingBids order by JobID", conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new BidItem
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("ID")),
                        JobID = reader["JobID"].ToString(),
                        BidDate = reader.GetDateTime(reader.GetOrdinal("BidDate")),
                        Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : reader["Address"].ToString(),
                        Submitter = reader.IsDBNull(reader.GetOrdinal("Submitter")) ? string.Empty : reader["Submitter"].ToString(),
                        BidStatus = reader.IsDBNull(reader.GetOrdinal("BidStatus")) ? string.Empty : reader["BidStatus"].ToString()
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
        [HttpGet("api/[controller]/GetAllStatusesData", Name = "GetAllStatusesData")]
        public async Task<IActionResult> GetAllStatusesData()
        {
            try
            {
                List<StatusModel> costCodeRecords = new List<StatusModel>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SELECT * FROM VwCashFlowStatus order by Id";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        var record = new StatusModel
                        {
                            Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? 0 : Convert.ToInt32(reader["Id"]),
                            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader["Status"].ToString()
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
        [HttpGet("api/CostCode/GetAllUserJobsByStatus/{statusId}", Name = "GetAllUserJobsByStatus")]
        public async Task<IActionResult> GetAllUserJobsByStatus(int statusId)
        {
            List<CostCodeRecord> costCodeRecords = new List<CostCodeRecord>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SP_GetCashFlowJobs";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Status", statusId);
                    command.CommandType = CommandType.StoredProcedure;

                    connection.Open();
                    SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (reader.Read())
                    {
                        CostCodeRecord record = new CostCodeRecord
                        {
                            RecNum = reader.IsDBNull(reader.GetOrdinal("recnum")) ? 0 : Convert.ToInt32(reader["recnum"]),
                            JobNumber = reader.IsDBNull(reader.GetOrdinal("jobnme")) ? string.Empty : reader["jobnme"].ToString()
                        };

                        costCodeRecords.Add(record);
                    }
                }
                costCodeRecords = costCodeRecords.OrderBy(x => x.RecNum).ToList();
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
        [HttpGet("api/[controller]/GetAllUserJobs/{email}", Name = "GetAllUserJobs")]
        public async Task<IActionResult> GetAllUserJobs(string email)
        {
            List<CostCodeRecord> visibleRecords = new List<CostCodeRecord>();
            try
            {
                List<CostCodeRecord> costCodeRecords = GetCostCodeRecords(4);
                costCodeRecords.AddRange(GetCostCodeRecords(5));
                costCodeRecords.AddRange(GetCostCodeRecords(6));
                visibleRecords = await GetUserJobs(costCodeRecords,email);
                visibleRecords = visibleRecords.OrderBy(x => x.RecNum).ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All User Jobs Error");
            }
            return Ok(visibleRecords);
        }
        private List<CostCodeRecord> GetCostCodeRecords(int status)
        {
            List<CostCodeRecord> costCodeRecords = new List<CostCodeRecord>();

            string connectionString = _configuration.GetConnectionString("StowellConnection");
            string query = "SELECT recnum, jobnme, addrs1+''+addrs2 AS Address, ctynme AS City, state_ AS State, zipcde AS ZipCode FROM actrec WHERE status = @Status order by recnum";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Status", status);

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
                        ZipCode = reader.IsDBNull(reader.GetOrdinal("ZipCode")) ? string.Empty : reader["ZipCode"].ToString(),
                    };

                    costCodeRecords.Add(record);
                }
            }

            return costCodeRecords;
        }
        private async Task<List<CostCodeRecord>> GetUserJobs(List<CostCodeRecord> costCodeRecords,string userId)
        {
            List<CostCodeRecord> visibleRecords = new List<CostCodeRecord>();
           // var userId = "p360admin@stowellinc.com";

            //var userId = HttpContext.User.Identity.Name;
            //await _userInfoService.InitializeUserInfo(userId);

            List<ProjectUser> projectUsers = GetUserProjects(userId, false);

            //var userInfo = _userInfoService.GetUserInfo(userId);
            //if (userInfo.Groups.Any(x => x.Value != null && (x.Value.Equals("Executive", StringComparison.OrdinalIgnoreCase) || x.Value.Equals("SR Accounting", StringComparison.OrdinalIgnoreCase) || x.Value.Equals("Accounting", StringComparison.OrdinalIgnoreCase))))
            //{
            visibleRecords = costCodeRecords;
            //}
            //else
            //{
            //foreach (var grp in userInfo.Groups)
            //{
            //    List<ProjectUser> divisionUsers = GetUserProjects(grp.Key, true);
            //    HashSet<int> divisionUsersRecNums = new HashSet<int>(divisionUsers.Select(p => p.RecNum));
            //    var tempRecords = costCodeRecords
            //        .Where(c => divisionUsersRecNums.Contains(c.RecNum))
            //        .ToList();
            //    visibleRecords.AddRange(tempRecords);
            //}
            //if (projectUsers.Count > 0)
            //{
            //    // Get a set of RecNum values from projectUsers for efficient lookup
            //    HashSet<int> projectUserRecNums = new HashSet<int>(projectUsers.Select(p => p.RecNum));

            //    // Filter costCodeRecords to keep only those with RecNum in projectUserRecNums
            //    var tempRecords1 = costCodeRecords
            //        .Where(c => projectUserRecNums.Contains(c.RecNum))
            //        .ToList();

            //    visibleRecords.AddRange(tempRecords1);
            //}


            //}

            // Ensure distinct results by using Distinct with a custom IEqualityComparer (if needed)
            visibleRecords = visibleRecords.Distinct(new CostCodeRecordComparer()).ToList();

            //visibleRecords = System.Linq.Enumerable.Distinct(visibleRecords, new CostCodeRecordComparer()).ToList();


            return visibleRecords;
        }
        private List<ProjectUser> GetUserProjects(string param, bool isdiv)
        {
            List<ProjectUser> records = new List<ProjectUser>();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            string query = "SPGetProjectDetails";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                if (isdiv)
                {
                    command.Parameters.AddWithValue("@division", param);
                }
                else
                {
                    command.Parameters.AddWithValue("@email", param);
                }
                command.CommandType = CommandType.StoredProcedure;

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    ProjectUser record = new ProjectUser
                    {
                        RecNum = reader.IsDBNull(reader.GetOrdinal("recnum")) ? 0 : Convert.ToInt32(reader["recnum"]),
                        JobName = reader.IsDBNull(reader.GetOrdinal("JobNumber")) ? string.Empty : reader["JobNumber"].ToString(),
                        PM1 = reader.IsDBNull(reader.GetOrdinal("PMEmail")) ? string.Empty : reader["PMEmail"].ToString(),
                        PM2 = reader.IsDBNull(reader.GetOrdinal("APMEmail")) ? string.Empty : reader["APMEmail"].ToString()
                    };

                    records.Add(record);
                }
            }

            return records;
        }
        [HttpGet("api/[controller]/GetJobPhasesData/{jobId}", Name = "GetJobPhasesData")]
        public async Task<IActionResult> GetJobPhasesData(int jobId)
        {
            List<Phase> costCodeRecords = new List<Phase>();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            string query = "BidPhases_Select";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@JobID", jobId);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Phase record = new Phase
                        {
                            PhaseNum = reader.IsDBNull(reader.GetOrdinal("Phase")) ? string.Empty : reader["Phase"].ToString(),
                            PhaseName = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader["Description"].ToString()
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
        [HttpGet("api/[controller]/GetAllPhases", Name = "GetAllPhases")]
        public async Task<IActionResult> GetAllPhases()
        {
            try
            {
                List<Phase> costCodeRecords = GetAllPhasesData();

                var visibleRecords = costCodeRecords
                    .OrderBy(x => x.PhaseNum)
                    .ToList();

                return Ok(new { data = visibleRecords });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllPhases Error");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.Message }
                );
            }
        }
        private List<Phase> GetAllPhasesData()
        {
            List<Phase> costCodeRecords = new List<Phase>();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            string query = "SELECT  * from VWPhasesList";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Phase record = new Phase
                    {
                        PhaseNum = reader.IsDBNull(reader.GetOrdinal("phsnum")) ? string.Empty : reader["phsnum"].ToString(),
                        PhaseName = reader.IsDBNull(reader.GetOrdinal("phsnme")) ? string.Empty : reader["phsnme"].ToString()
                    };

                    costCodeRecords.Add(record);
                }
            }

            return costCodeRecords;
        }
        [HttpGet("api/[controller]/GetAllPeriodsData", Name = "GetAllPeriodsData")]
        public async Task<IActionResult> GetAllPeriodsData()
        {
            try
            {
                List<string> costCodeRecords = new List<string>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SELECT * FROM VwMonth";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        var record = new
                        {
                            MonthYear = reader.IsDBNull(reader.GetOrdinal("MonthYear")) ? string.Empty : reader["MonthYear"].ToString()
                        };

                        costCodeRecords.Add(record.ToString());
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
        
    }
}
