using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StowellCoAPI.Controllers
{
    // NEW controller (2026-09-23) - backs the Invoices tab on the Project Overview page. The Angular tab
    // (invoices-tab.component.ts -> JobInvoiceService.getJobInvoices) has always called
    // api/Invoices/GetJobInvoices/{jobId}, but no such endpoint existed (404), so the grid was always
    // empty. Reads through SAGESBQ's Invoices_GetJobInvoices, which selects the job's Sage invoices
    // (StowellSandbox.dbo.acrinv) together with each invoice's phase, so the tab can filter by phase.
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly ILogger<InvoicesController> _logger;
        private readonly IConfiguration _configuration;

        public InvoicesController(ILogger<InvoicesController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("api/Invoices/GetJobInvoices/{jobId}", Name = "GetJobInvoices")]
        public async Task<IActionResult> GetJobInvoices(long jobId)
        {
            var invoices = new List<JobInvoiceRecord>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("Invoices_GetJobInvoices", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@JobId", jobId);

                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    invoices.Add(new JobInvoiceRecord
                    {
                        InvoiceId = reader.IsDBNull(reader.GetOrdinal("InvoiceId")) ? string.Empty : reader["InvoiceId"].ToString(),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader["Name"].ToString(),
                        Amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0 : Convert.ToDecimal(reader["Amount"]),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader["Status"].ToString(),
                        Phase = reader.IsDBNull(reader.GetOrdinal("Phase")) ? (int?)null : Convert.ToInt32(reader["Phase"]),
                        InvoiceDate = reader.IsDBNull(reader.GetOrdinal("InvoiceDate")) ? string.Empty : Convert.ToDateTime(reader["InvoiceDate"]).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture)
                    });
                }

                return Ok(new { invoices });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
                return StatusCode(500, new { Message = "A database error occurred while retrieving the job's invoices.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the job's invoices.", Details = ex.Message });
            }
        }
    }

    public class JobInvoiceRecord
    {
        public string InvoiceId { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        // Sage phase number (acrinv.phsnum) - lets the Invoices tab show one phase at a time.
        public int? Phase { get; set; }
        public string InvoiceDate { get; set; }
    }
}
