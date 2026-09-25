using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;

namespace StowellCoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StowellAdminController : ControllerBase
    {
        private readonly ILogger<StowellAdminController> _logger;
        private readonly IConfiguration _configuration;

        public StowellAdminController(ILogger<StowellAdminController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }
        [HttpPost("ProcessDisconnectUsers")]
        public async Task<IActionResult> ProcessDisconnectUsers()
        {
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            string query = @"
        USE [master];
        DECLARE @kill varchar(8000) = '';
        SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'
        FROM sys.dm_exec_sessions
        WHERE is_user_process = 1
          AND session_id <> @@SPID;

        EXEC(@kill);
    ";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            return Ok();
        }

    }
}
