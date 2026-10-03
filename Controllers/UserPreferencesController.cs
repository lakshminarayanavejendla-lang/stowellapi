using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StowellCoAPI.Controllers
{
    /// <summary>
    /// Per-user settings stored on the server (dbo.UserPreferences, see db/user-preferences-2026-09-26.sql) so they follow the user to any
    /// browser or device. Today: "density" (UI density - comfortable / standard / compact). Only known keys and values are accepted.
    /// </summary>
    [ApiController]
    public class UserPreferencesController : ControllerBase
    {
        private readonly ILogger<UserPreferencesController> _logger;
        private readonly IConfiguration _configuration;

        // the settings the app understands, and the values each may hold
        private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            ["density"] = new[] { "comfortable", "standard", "compact" },
        };

        public UserPreferencesController(ILogger<UserPreferencesController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public class UserPreferenceDto
        {
            public string? Email { get; set; }
            public string? Key { get; set; }
            public string? Value { get; set; }
        }

        /// <summary>All saved settings for a user, as { key: value }.</summary>
        [HttpGet("api/UserPreferences/{email}", Name = "GetUserPreferences")]
        public async Task<IActionResult> Get(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { Message = "Email is required." });
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                using var command = new SqlCommand("UserPreference_Get", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@Email", email);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                var result = new Dictionary<string, string>();
                while (await reader.ReadAsync())
                {
                    result[reader["PrefKey"].ToString()!] = reader["PrefValue"].ToString()!;
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loading user preferences failed");
                return StatusCode(500, new { Message = "Could not load the user's settings.", Details = ex.Message });
            }
        }

        /// <summary>Save one setting for a user.</summary>
        [HttpPost("api/UserPreferences", Name = "SaveUserPreference")]
        public async Task<IActionResult> Save([FromBody] UserPreferenceDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Key) || request.Value == null)
                return BadRequest(new { Message = "Email, key and value are required." });
            if (!Allowed.TryGetValue(request.Key, out var values) || !values.Contains(request.Value, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Unknown setting or value." });

            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("SageSBQConnection"));
                using var command = new SqlCommand("UserPreference_Set", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@Email", request.Email);
                command.Parameters.AddWithValue("@PrefKey", request.Key.ToLowerInvariant());
                command.Parameters.AddWithValue("@PrefValue", request.Value.ToLowerInvariant());
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return Ok(new { saved = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saving a user preference failed");
                return StatusCode(500, new { Message = "Could not save the user's setting.", Details = ex.Message });
            }
        }
    }
}
