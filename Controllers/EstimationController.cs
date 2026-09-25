using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NPOI.HSSF.Record;
using StowellCoAPI.DTO;
using StowellCoAPI.Models;
using System.Data;
using System.Globalization;

namespace StowellCoAPI.Controllers
{
    [ApiController]
    public class EstimationController : ControllerBase
    {


        private readonly IConfiguration _configuration;
        private readonly ILogger<ProjectOverviewController> _logger;

        public EstimationController(ILogger<ProjectOverviewController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }


        [HttpGet("api/[controller]/GetOpenBids", Name = "GetOpenBids")]
        public async Task<IActionResult> GetOpenBids()
        {
            var result = new List<BidItem>();
            string cs = _configuration.GetConnectionString("SageSBQConnection");
            try
            {
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand($"SELECT ID, JobID, BidDate, Address, Submitter, BidStatus FROM VwOpenBids order by JobID", conn);
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

        [HttpGet("api/[controller]/GetClosedBids", Name = "GetClosedBids")]
        public async Task<IActionResult> GetClosedBids()
        {
            var result = new List<BidItem>();
            string cs = _configuration.GetConnectionString("SageSBQConnection");
            try
            {
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand($"SELECT ID, JobID, BidDate, Address, Submitter, BidStatus FROM VwClosedBids order by JobID", conn);
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

        [HttpGet("api/[controller]/GetPendingBids", Name = "GetPendingBids")]
        public async Task<IActionResult> GetPendingBids()
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


        [HttpGet("api/[controller]/GetBidsAmountAsync/{jobId}", Name = "GetBidsAmountAsync")]
        public async Task<IActionResult> GetBidsAmountAsync(string jobId)
        {
            var bids = new List<BidAmount>();
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("BidAmount_Select", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@JobID", jobId);

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                bids.Add(new BidAmount
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                    JobId = jobId,

                    BidDate = reader.IsDBNull(reader.GetOrdinal("BidDate"))
                        ? null
                        : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("BidDate"))),

                    BidAmount1 = reader.IsDBNull(reader.GetOrdinal("BidAmount"))
                        ? null
                        : reader.GetDecimal(reader.GetOrdinal("BidAmount"))
                });
            }

            return Ok(bids);
        }

        [HttpGet("api/[controller]/GetAllEmployees", Name = "GetAllEmployees")]
        public async Task<IActionResult> GetAllEmployees()
        {
            List<ContractorClient> visibleRecords = new List<ContractorClient>();
            try
            {
                visibleRecords = await GetCostCodeRecords();
                return Ok(visibleRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All User Jobs Error - GetAllEmployees");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving employees");
            }
        }
        private async Task<List<ContractorClient>> GetCostCodeRecords()
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

        [HttpGet("api/[controller]/GetBidDetails/{jobId}", Name = "GetBidDetails")]
        public async Task<IActionResult> GetBidDetails(string jobId)
        {
            var result = new BidInfoDto();
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("Bids_Select", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@JobID", jobId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result = new BidInfoDto
                                {
                                    JobID = reader.IsDBNull(reader.GetOrdinal("JobID")) ? string.Empty : reader["JobID"].ToString(),
                                    Bidder = reader.IsDBNull(reader.GetOrdinal("Bidder")) ? string.Empty : reader["Bidder"].ToString(),
                                    Phase = reader.IsDBNull(reader.GetOrdinal("Phase")) ? string.Empty : reader["Phase"].ToString(),
                                    BidStatus = reader.IsDBNull(reader.GetOrdinal("BidStatus")) ? string.Empty : reader["BidStatus"].ToString(),
                                    Division = reader.IsDBNull(reader.GetOrdinal("Division")) ? string.Empty : reader["Division"].ToString(),
                                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? string.Empty : reader["Department"].ToString(),
                                    JobType = reader.IsDBNull(reader.GetOrdinal("JobType")) ? string.Empty : reader["JobType"].ToString(),
                                    JobName = reader.IsDBNull(reader.GetOrdinal("JobName")) ? string.Empty : reader["JobName"].ToString(),
                                    ShortName = reader.IsDBNull(reader.GetOrdinal("ShortName")) ? string.Empty : reader["ShortName"].ToString(),
                                    ContractNumber = reader.IsDBNull(reader.GetOrdinal("ContractNumber")) ? string.Empty : reader["ContractNumber"].ToString(),
                                    ContractDate = reader.IsDBNull(reader.GetOrdinal("ContractDate")) ? null : Convert.ToDateTime(reader["ContractDate"]).ToString("MM/dd/yyy",CultureInfo.InvariantCulture),
                                    ContractAmount = reader.IsDBNull(reader.GetOrdinal("ContractAmount")) ? (decimal?)null : (decimal)reader["ContractAmount"],
                                    EstStartDate = reader.IsDBNull(reader.GetOrdinal("EstStartDate"))
    ? null
    : Convert.ToDateTime(reader["EstStartDate"])
        .ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),

                                   // EstStartDate = reader.IsDBNull(reader.GetOrdinal("EstStartDate")) ? string.Empty : Convert.ToDateTime(reader["EstStartDate"]).ToString("MM/dd/yyyy"),
                                    EstCompletionDate = reader.IsDBNull(reader.GetOrdinal("EstCompletionDate")) ? null : Convert.ToDateTime(reader["EstCompletionDate"]).ToString("MM/dd/yyyy",CultureInfo.InvariantCulture)
                                };
                            }
                        }
                    }
                    using (var cmd = new SqlCommand("ProjectLocation_Select", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@JobID", jobId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.ProjectLocation = new ProjectLocationDto
                                {
                                    Jobsite = reader.IsDBNull(reader.GetOrdinal("Jobsite")) ? string.Empty : reader["Jobsite"].ToString(),
                                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : reader["Address"].ToString(),
                                    Address2 = reader.IsDBNull(reader.GetOrdinal("Address2")) ? string.Empty : reader["Address2"].ToString(),
                                    City = reader.IsDBNull(reader.GetOrdinal("City")) ? string.Empty : reader["City"].ToString(),
                                    State = reader.IsDBNull(reader.GetOrdinal("State")) ? string.Empty : reader["State"].ToString(),
                                    SalesTaxDistrict = reader.IsDBNull(reader.GetOrdinal("SalesTaxDistrict")) ? string.Empty : reader["SalesTaxDistrict"].ToString()
                                };

                            }
                        }
                    }
                    using (var cmd = new SqlCommand("ContractorInfo_Select", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@JobID", jobId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.ContractorInfo = new ContractorInfoDto
                                {
                                    Client = reader.IsDBNull(reader.GetOrdinal("Client")) ? string.Empty : reader["Client"].ToString(),
                                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : reader["Address"].ToString(),
                                    Address2 = reader.IsDBNull(reader.GetOrdinal("Address2")) ? string.Empty : reader["Address2"].ToString(),
                                    City = reader.IsDBNull(reader.GetOrdinal("City")) ? string.Empty : reader["City"].ToString(),
                                    State = reader.IsDBNull(reader.GetOrdinal("State")) ? string.Empty : reader["State"].ToString(),
                                    Zip = reader.IsDBNull(reader.GetOrdinal("Zip")) ? string.Empty : reader["Zip"].ToString()
                                };

                            }
                        }
                    }
                    using (var cmd = new SqlCommand("ProjectInfo_Select", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@JobID", jobId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.ProjectInfo = new ProjectInfoDto
                                {
                                    InsuranceType = reader.IsDBNull(reader.GetOrdinal("InsuranceType")) ? 0 : Convert.ToInt32(reader["InsuranceType"].ToString()),
                                    Bonded = reader.IsDBNull(reader.GetOrdinal("Bonded")) ? 0 : Convert.ToInt32(reader["Bonded"]),
                                    TaxExcempt = reader.IsDBNull(reader.GetOrdinal("TaxExcempt")) ? 0 : Convert.ToInt32(reader["TaxExcempt"]),
                                    InvoiceSubmittal = reader.IsDBNull(reader.GetOrdinal("InvoiceSubmittal")) ? 0 : Convert.ToInt32(reader["InvoiceSubmittal"]),
                                    CertifiedPayroll = reader.IsDBNull(reader.GetOrdinal("CertifiedPayroll")) ? 0 : Convert.ToInt32(reader["CertifiedPayroll"]),
                                    NetTermsDate = reader.IsDBNull(reader.GetOrdinal("NetTermsDate")) ? null : Convert.ToDateTime(reader["NetTermsDate"]).ToString("MM/dd/yyyy",CultureInfo.InvariantCulture),
                                    RetainagePerc = reader.IsDBNull(reader.GetOrdinal("RetainagePerc")) ? (decimal?)null : (decimal)reader["RetainagePerc"]
                                };

                            }
                        }
                    }
                    if (DateTime.TryParse(result.ContractDate, out var parsedDate)
     && parsedDate.Year == 1900)
                    {
                        result.ContractDate = null;
                    }
                    if (DateTime.TryParse(result.EstStartDate, out var parsedStartDate)
     && parsedStartDate.Year == 1900)
                    {
                        result.EstStartDate = null;
                    }
                    if (DateTime.TryParse(result.EstCompletionDate, out var parsedCompletionDate)
   && parsedCompletionDate.Year == 1900)
                    {
                        result.EstCompletionDate = null;
                    }
                    if (result.ProjectInfo != null &&
     DateTime.TryParse(result.ProjectInfo.NetTermsDate, out var parsedNetTerms)
     && parsedNetTerms.Year == 1900)
                    {
                        result.ProjectInfo.NetTermsDate = null;
                    }
                    return Ok(result);
                }
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

        [HttpGet("api/[controller]/GetEmployees/{jobId}", Name = "GetEmployees")]
        public async Task<IActionResult> GetEmployees(string jobId)
        {
            var employees = new List<EmployeeDetails>();
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("BidEmployee_Select", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", jobId);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new EmployeeDetails
                            {
                                Id = reader.IsDBNull(reader.GetOrdinal("ID")) ? string.Empty : reader["ID"].ToString(),
                                Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? string.Empty : reader["Role"].ToString(),
                                Employee = reader.IsDBNull(reader.GetOrdinal("Employee")) ? string.Empty : reader["Employee"].ToString()
                            });
                        }
                    }
                }

                return Ok(employees);
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
        [HttpGet("api/[controller]/GetBidEmployees/{jobId}")]

        public async Task<IActionResult> GetBidEmployees(string jobId)

        {

            if (string.IsNullOrWhiteSpace(jobId))

                return BadRequest("JobId is required.");

            var employees = new List<BidEmployee>();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            try

            {

                await using var conn = new SqlConnection(connectionString);

                await using var cmd = new SqlCommand("BidEmployee_Select", conn)

                {

                    CommandType = CommandType.StoredProcedure

                };

                cmd.Parameters.Add("@JobID", SqlDbType.VarChar).Value = jobId;

                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())

                {

                    employees.Add(new BidEmployee

                    {

                        Id = reader["ID"] != DBNull.Value

                                ? Convert.ToInt32(reader["ID"])

                                : 0,

                        JobId = jobId,

                        Role = reader["Role"] as string,

                        Employee = reader["Employee"] as string

                    });

                }

                return Ok(employees);

            }

            catch (SqlException sqlEx)

            {

                _logger.LogError(sqlEx,

                    "SQL error in GetBidEmployees for JobId {JobId}", jobId);

                return StatusCode(500, new

                {

                    Message = "A database error occurred while retrieving bid employees.",

                    Details = sqlEx.Message

                });

            }

            catch (Exception ex)

            {

                _logger.LogError(ex,

                    "Unexpected error in GetBidEmployees for JobId {JobId}", jobId);

                return StatusCode(500, new

                {

                    Message = "An unexpected error occurred while retrieving bid employees.",

                    Details = ex.Message
                });
            }
        }

        [HttpGet("api/[controller]/GetNewBidNumber", Name = "GetNewBidNumber")]
        public async Task<string> GetNewBidNumber()
        {
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            string query = "SELECT filenumber FROM GetNextFileNumber";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                object result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToString(result) : string.Empty;
            }
        }

        [HttpPost("api/Estimation/InsertEmployee", Name = "InsertEmployee")]
        public async Task<IActionResult> InsertEmployee(BidEmployee bidEmployee)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("BidEmployee_Insert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", bidEmployee.JobId);
                    cmd.Parameters.AddWithValue("@Role", bidEmployee.Role); // Assuming roleName = 'PM' or 'Sr. PM'
                    cmd.Parameters.AddWithValue("@Employee", bidEmployee.EmployeeId); // Assuming this is recNum

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { success = true });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsertEmployee Error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPost("api/Estimation/InsertBid", Name = "InsertBid")]
        public async Task<IActionResult> InsertBid(BidAmount bidAmount)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("BidAmount_Insert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", bidAmount.JobId);
                    cmd.Parameters.AddWithValue("@BidDate", bidAmount.BidDate); // Assuming roleName = 'PM' or 'Sr. PM'
                    cmd.Parameters.AddWithValue("@BidAmount", bidAmount.BidAmount1); // Assuming this is recNum

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { success = true });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsertBid Error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet("api/[controller]/GetPhases/{jobId}", Name = "GetPhases")]
        public async Task<IActionResult> GetPhases(string jobId)
        {
            var phases = new List<BidPhase>();
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("BidPhase_Select", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", jobId);

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            phases.Add(new BidPhase
                            {
                                Id = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID"]),
                                JobId = jobId,
                                Phase = reader["Phase"] == DBNull.Value ? null : reader["Phase"].ToString(),
                                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString()
                            });
                        }
                    }
                }

                return Ok(phases);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("api/Estimation/InsertPhase", Name = "InsertPhase")]
        public async Task<IActionResult> InsertPhase(BidPhase bidPhase)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("BidPhase_Insert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", bidPhase.JobId);
                    cmd.Parameters.AddWithValue("@Phase", bidPhase.Phase); // Assuming roleName = 'PM' or 'Sr. PM'
                    cmd.Parameters.AddWithValue("@Description", bidPhase.Description); // Assuming this is recNum

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { success = true });
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "InsertPhase Error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet("api/[controller]/GetAllContractorClients", Name = "GetAllContractorClients")]
        public async Task<IActionResult> GetAllContractorClients()
        {
            try
            {
                var contractorClients = await GetContractorClientRecords();

                return Ok(new
                {
                    data = contractorClients ?? new List<ContractorClient>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllContractorClients");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving contractor clients",
                    data = new List<ContractorClient>()
                });
            }
        }

        private async Task<List<ContractorClient>> GetContractorClientRecords(string id = null)
        {
            List<ContractorClient> contractorClientRecords = new List<ContractorClient>();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            string query = string.IsNullOrEmpty(id)
                ? "SELECT * FROM VwClients ORDER BY ID"
                : "SELECT * FROM VwClients WHERE ID = @Id ORDER BY ID";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    command.Parameters.AddWithValue("@Id", id);
                }

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ContractorClient record = new ContractorClient
                        {
                            Id = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                            Name = reader["clientname"]?.ToString() ?? string.Empty,
                            ShortName = reader["shtnme"]?.ToString() ?? string.Empty,
                            Address1 = reader["address1"]?.ToString() ?? string.Empty,
                            Address2 = reader["address2"]?.ToString() ?? string.Empty,
                            City = reader["city"]?.ToString() ?? string.Empty,
                            State = reader["state"]?.ToString() ?? string.Empty,
                            ZipCode = reader["zip"]?.ToString() ?? string.Empty
                        };

                        contractorClientRecords.Add(record);
                    }
                }
            }

            return contractorClientRecords;
        }

        [HttpGet("api/[controller]/GetContractorClientById/{clientId}", Name = "GetContractorClientById")]
        public async Task<IActionResult> GetContractorClientById(string clientId)
        {
            try
            {
                // Fetch all clients for this ID
                var clients = await GetContractorClientRecords(clientId);

                // Return the first matching client, or null
                var client = clients.FirstOrDefault();

                return Ok(client); // returns JSON directly
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching client by ID");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost("api/Estimation/SubmitBid", Name = "SubmitBid")]
        public async Task<IActionResult> SubmitBid([FromBody] BidInfoDto bid)
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
            try
            {
                var result = await InsertBidData(bid);
                return Ok(new { success = result.Success, message = result.Message });
            }
            catch (SqlException ex)
            {
                // 🔥 THIS captures RAISERROR messages
                string sqlMessage = ex.Message;

                // You can log it
                _logger.LogError(ex, "Bids_Insert failed");
                return Ok(new { success = false, message = $"{sqlMessage}" });
                // Or return to UI / API
                // throw new Exception(sqlMessage);
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Failed to save the project details." });
                _logger.LogError(ex, ex.Message);
            }
        }
        private async Task<(bool Success, string Message)> InsertBidData(BidInfoDto bid)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                await using (var cmd = new SqlCommand("Bids_Insert", conn))
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

                await using (var cmd = new SqlCommand("ProjectLocation_Insert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobID", bid.ProjectLocation.JobID);
                    cmd.Parameters.AddWithValue("@Jobsite", bid.ProjectLocation.Jobsite);
                    cmd.Parameters.AddWithValue("@Address", bid.ProjectLocation.Address);
                    cmd.Parameters.AddWithValue("@Address2", bid.ProjectLocation.Address2);
                    cmd.Parameters.AddWithValue("@City", bid.ProjectLocation.City);
                    cmd.Parameters.AddWithValue("@State", bid.ProjectLocation.State);
                    cmd.Parameters.AddWithValue("@SalesTaxDistrict", string.IsNullOrEmpty(bid.ProjectLocation.SalesTaxDistrict) ? DBNull.Value : bid.ProjectLocation.SalesTaxDistrict);
                    await cmd.ExecuteNonQueryAsync();
                }


                await using (var cmd = new SqlCommand("ContractorInfo_Insert", conn))
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

                await using (var cmd = new SqlCommand("ProjectInfo_Insert", conn))
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
                return (true, "Project details saved successfully.");
            }
            catch (SqlException ex)
            {
                // 🔥 THIS captures RAISERROR messages
                string sqlMessage = ex.Message;

                // You can log it
                _logger.LogError(ex, "InsertBindData failed");
                return (false, $"{sqlMessage}");
                // Or return to UI / API
                // throw new Exception(sqlMessage);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to save the project details.");
                _logger.LogError(ex, ex.Message);
            }
        }
        [HttpPost("api/Estimation/UpdateBid", Name = "UpdateBid")]
        public async Task<IActionResult> UpdateBid([FromBody] BidInfoDto bid)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .Select(e => new { Field = e.Key, Errors = e.Value.Errors.Select(x => x.ErrorMessage) });
                return BadRequest(errors);
            }
            if (bid == null || string.IsNullOrWhiteSpace(bid.JobID))
                return BadRequest("Invalid project data");

            // Save data using repository
            var result = await UpdateBidData(bid);
            return Ok(new { success = result.Success, message = result.Message });
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
        [HttpPost("api/Estimation/DeleteBidAmount", Name = "DeleteBidAmount")]
        public async Task<IActionResult> DeleteBidAmount([FromBody] string id)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("BidAmount_Delete", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteBidAmount Error");
               
            }
            return Ok(new { success = false });
        }
        [HttpPost("api/Estimation/DeletePhase", Name = "DeletePhase")]
        public async Task<IActionResult> DeletePhase([FromBody] string id)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("BidPhase_Delete", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletePhase Error");
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("api/Estimation/DeleteEmployee", Name = "DeleteEmployee")]
        public async Task<IActionResult> DeleteEmployee([FromBody] string id)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("BidEmployee_Delete", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteEmployee Error");
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }
}
