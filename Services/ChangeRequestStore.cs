using Microsoft.Data.SqlClient;
using System.Data;

namespace StowellCoAPI.Services
{
    /// <summary>
    /// Changes to records that are already in Sage (edit a PO or CO, delete a posted CO budget allocation) are not
    /// written to Sage when the PM saves. They are saved here as Pending change requests (dbo.ChangeRequests) and
    /// Sage is only changed when Accounting approves. See db/change-requests-2026-10-04.sql.
    /// </summary>
    public static class ChangeRequestStore
    {
        public const string Po = "PO";
        public const string Co = "CO";
        public const string CoAllocation = "COALLOC";

        /// <summary>Saves a Pending request and returns its id. A SqlException with Number 50000 carries a message meant for the user.</summary>
        public static async Task<int> RequestAsync(IConfiguration configuration, string entity, string entityId, string changeType, string? payloadJson, string requestedBy)
        {
            using var connection = new SqlConnection(configuration.GetConnectionString("SageSBQConnection"));
            using var command = new SqlCommand("dbo.Change_Request", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Entity", entity);
            command.Parameters.AddWithValue("@EntityId", entityId);
            command.Parameters.AddWithValue("@ChangeType", changeType);
            command.Parameters.AddWithValue("@PayloadJson", (object?)payloadJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@RequestedBy", string.IsNullOrWhiteSpace(requestedBy) ? "Unknown user" : requestedBy);
            await connection.OpenAsync();
            var id = await command.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        /// <summary>True for an error the procedure raised on purpose (already waiting, record not found...).</summary>
        public static bool IsUserFacing(SqlException ex) => ex.Number == 50000;
    }
}
