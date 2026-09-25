using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
using StowellCoAPI.Models;
using System.Data;

namespace StowellCoAPI.Controllers
{
    [ApiController]
    public class AdminPanelController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProjectOverviewController> _logger;

        public AdminPanelController(ILogger<ProjectOverviewController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        [HttpGet("api/[controller]/GetProjectIds", Name = "GetProjectIds")]
        public async Task<IActionResult> GetProjectIds()
        {
            // FIX (2026-09-16) - was querying "StowellCompany.dbo.actrec" via SageSBQConnection -
            // StowellCompany isn't the real jobs database this app actually uses anywhere else
            // (CostCodeController.GetCostCodeRecords, InsertActRec, ConvertBidToProject, etc. all
            // use StowellConnection -> StowellSandbox.dbo.actrec), so this endpoint always
            // returned stale/wrong/empty data. This is what backs the Admin Panel's per-project
            // access list, so a newly won bid's project never appeared there - not because
            // ProjectManagementUsers had no row for it (expected, nobody's been assigned yet),
            // but because the "which projects exist" source itself pointed at the wrong DB.
            List<CostCodeRecord> costCodeRecords = new List<CostCodeRecord>();
            try
            {
                string connectionString = _configuration.GetConnectionString("StowellConnection");
                string query = "select recnum,jobnme from actrec order by recnum";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    
                    command.CommandType = CommandType.Text;

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

        [HttpGet("api/[controller]/GetAdminPanelUsers", Name = "GetAdminPanelUsers")]
        public async Task<IActionResult> GetAdminPanelUsers()
        {
            List<StowellUser> _stowellUsers = new List<StowellUser>();
            string cs = _configuration.GetConnectionString("SageSBQConnection");
            try
            {
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand($"select distinct Email,Id,Isnull(Role,0) as Role from StowellUsers order by Email", conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    StowellUser record = new StowellUser
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader["Email"].ToString(),
                        Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? string.Empty : reader["Role"].ToString()

                    };
                    _stowellUsers.Add(record);
                }

                return Ok(_stowellUsers);
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

        [HttpGet("api/[controller]/GetUserRoles", Name = "GetUserRoles")]
        public async Task<IActionResult> GetUserRoles()
        {
            List<ProjectRoles> _projectRoles = new List<ProjectRoles>();
            string cs = _configuration.GetConnectionString("SageSBQConnection");
            try
            {
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand($"select RoleID, RoleName from projectroles", conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ProjectRoles record = new ProjectRoles
                    {
                        //Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        RoleId = reader.GetInt32(reader.GetOrdinal("RoleID")),
                        RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? string.Empty : reader["RoleName"].ToString()

                    };
                    _projectRoles.Add(record);
                }

                return Ok(_projectRoles);
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

        [HttpGet("api/[controller]/GetAccessRecords", Name = "GetAccessRecords")]
        public async Task<List<AccessRequest>> GetAccessRecords()
        {
            List<AccessRequest> costCodeRecords = new List<AccessRequest>();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            string query = "SPGetProjectDetails";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                //command.Parameters.AddWithValue("@Status", status);
                command.CommandType = CommandType.StoredProcedure;

                connection.Open();
                SqlDataReader reader = await command.ExecuteReaderAsync();

                while (reader.Read())
                {
                    AccessRequest record = new AccessRequest
                    {
                        JobId = reader.IsDBNull(reader.GetOrdinal("recnum")) ? 0 : Convert.ToInt32(reader["recnum"]),
                        JobName = reader.IsDBNull(reader.GetOrdinal("JobNumber")) ? string.Empty : reader["JobNumber"].ToString(),
                        ProjectManager = reader.IsDBNull(reader.GetOrdinal("PMEmail")) ? string.Empty : reader["PMEmail"].ToString(),
                        AssistantProjectManager = reader.IsDBNull(reader.GetOrdinal("APMEmail")) ? string.Empty : reader["APMEmail"].ToString(),
                        BranchManagerGroupId = reader.IsDBNull(reader.GetOrdinal("Division")) ? string.Empty : reader["Division"].ToString()
                    };

                    costCodeRecords.Add(record);
                }
            }

            return costCodeRecords;
        }

        [HttpGet("api/[controller]/GetProjectEmails", Name = "GetProjectEmails")]
        public async Task<IActionResult> GetProjectEmails()
        {
            try
            {
                List<ProjectManagementUser> projectManagementUsers = new List<ProjectManagementUser>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SP_GetProjectEmails";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    //command.Parameters.AddWithValue("@Status", status);
                    command.CommandType = CommandType.StoredProcedure;

                    connection.Open();
                    SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (reader.Read())
                    {
                        ProjectManagementUser record = new ProjectManagementUser
                        {
                            JobNumber = reader.IsDBNull(reader.GetOrdinal("JobNumber")) ? 0 : Convert.ToInt32(reader["JobNumber"]),
                            EmailId = reader.IsDBNull(reader.GetOrdinal("EmailID")) ? string.Empty : reader["EmailID"].ToString(),
                            UserRoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? string.Empty : reader["RoleName"].ToString(),
                        };

                        projectManagementUsers.Add(record);
                    }
                }
                return Ok(projectManagementUsers);
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

        [HttpPost("api/[controller]/InsertProjectManagementUser", Name = "InsertProjectManagementUser")]
        public async Task<IActionResult> InsertProjectManagementUser(ProjectManagementUser projectManagementUser)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("SP_InsertProjectManagementUser", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobNumber", projectManagementUser.JobNumber);
                    cmd.Parameters.AddWithValue("@EmailID", projectManagementUser.EmailId); // Assuming roleName = 'PM' or 'Sr. PM'
                    cmd.Parameters.AddWithValue("@UserRole", projectManagementUser.UserRole); // Assuming this is recNum

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { success = true });
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "InsertProjectManagementUser Error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("api/[controller]/DeleteProjectManagementUser", Name = "DeleteProjectManagementUser")]
        public async Task<IActionResult> DeleteProjectManagementUser([FromQuery] int jobNumber, [FromQuery] string emailId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("sp_DeleteProjectManagementUser", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@JobNumber", jobNumber);
                    cmd.Parameters.AddWithValue("@EmailID", emailId);

                    conn.Open();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int rowsDeleted = reader.GetInt32(reader.GetOrdinal("RowsDeleted"));
                            string message = reader.GetString(reader.GetOrdinal("Message"));

                            if (rowsDeleted > 0)
                            {
                                return Ok(new
                                {
                                    success = true,
                                    rowsDeleted,
                                    message
                                });
                            }

                            return NotFound(new
                            {
                                success = false,
                                message = "No matching record found to delete."
                            });
                        }
                    }
                }

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Unexpected error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteProjectManagementUser Error");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

    }
}
