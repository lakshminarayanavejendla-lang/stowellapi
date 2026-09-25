using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.Models;
using System.Data;

namespace StowellCoAPI.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ILogger<ReportsController> _logger;
        private readonly IConfiguration _configuration;

        public ReportsController(ILogger<ReportsController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }
        [HttpGet("api/[controller]/GetCashFlowForecastReportData")]
        public async Task<IActionResult> GetCashFlowForecastReportData()
        {
            try
            {
                string connectionString =  _configuration.GetConnectionString("SageSBQConnection");

                var result = new CashFlowForecastResponse();

                using SqlConnection connection = new SqlConnection(connectionString);

                await connection.OpenAsync();

                #region Budget Forecast

                using (SqlCommand cmd = new SqlCommand("SP_CashFlowBudget", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var row = new CashFlowForecastRow();

                        row.JobNumber = reader["Job Number"]?.ToString();
                        row.JobName = reader["Job Name"]?.ToString();

                        row.TotalAmount =
                            reader["Budget Amount"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Budget Amount"])
                            : 0;

                        // Find "Costs as of..." column dynamically
                        string costColumn = Enumerable.Range(0, reader.FieldCount)
                            .Select(i => reader.GetName(i))
                            .FirstOrDefault(c =>
                                c.StartsWith("Costs as of",
                                    StringComparison.OrdinalIgnoreCase));

                        row.CurrentAmount =
                            !string.IsNullOrEmpty(costColumn) &&
                            reader[costColumn] != DBNull.Value
                            ? Convert.ToDecimal(reader[costColumn])
                            : 0;

                        row.Remaining =
                            reader["Remaining"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Remaining"])
                            : 0;

                        // Monthly forecast columns
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);

                            if (columnName.Contains("-"))
                            {
                                row.ForecastMonths[columnName] =
                                    reader.IsDBNull(i)
                                    ? 0
                                    : Convert.ToDecimal(reader[i]);
                            }
                        }

                        result.BudgetForecast.Add(row);
                    }
                }

                #endregion

                #region Contract Forecast

                using (SqlCommand cmd = new SqlCommand("sp_cashflowcontract", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var row = new CashFlowForecastRow();

                        row.JobNumber = reader["Job Number"]?.ToString();
                        row.JobName = reader["Job Name"]?.ToString();

                        row.TotalAmount =
                            reader["Total Contract Amount"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Total Contract Amount"])
                            : 0;

                        // Find "Billed as of..." column dynamically
                        string billedColumn = Enumerable.Range(0, reader.FieldCount)
                            .Select(i => reader.GetName(i))
                            .FirstOrDefault(c =>
                                c.StartsWith("Billed as of",
                                    StringComparison.OrdinalIgnoreCase));

                        row.CurrentAmount =
                            !string.IsNullOrEmpty(billedColumn) &&
                            reader[billedColumn] != DBNull.Value
                            ? Convert.ToDecimal(reader[billedColumn])
                            : 0;

                        row.Remaining =
                            reader["Remaining"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Remaining"])
                            : 0;

                        // Monthly forecast columns
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);

                            if (columnName.Contains("-"))
                            {
                                row.ForecastMonths[columnName] =
                                    reader.IsDBNull(i)
                                    ? 0
                                    : Convert.ToDecimal(reader[i]);
                            }
                        }

                        result.ContractForecast.Add(row);
                    }
                }

                #endregion

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "Error loading Cash Flow Forecast report.",
                    Details = ex.Message
                });
            }
        }
    }
}
