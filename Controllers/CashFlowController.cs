using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using StowellCoAPI.DTO;
using System.Data;
using System.Globalization;

namespace StowellCoAPI.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class CashFlowController : ControllerBase
    {
        private readonly ILogger<CashFlowController> _logger;
        private readonly IConfiguration _configuration;

        public CashFlowController(ILogger<CashFlowController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }
        [HttpPost("api/[controller]/CashFlowAllDataByPeriod", Name = "CashFlowAllDataByPeriod")]
        public async Task<IActionResult> CashFlowAllDataByPeriod([FromBody] CashFlowQueryInput input)
        {
            try
            {
                string recnum; DateTime startDate; DateTime endDate;
                recnum = input.recnum;
                startDate = input.StartDate;
                endDate = input.EndDate;
                List<CashFlowRecord> records = new List<CashFlowRecord>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SP_GetCashFlowDetails";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.CommandText = query;
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@recnum", recnum);
                    command.Parameters.AddWithValue("@startDate", startDate);
                    command.Parameters.AddWithValue("@endDate", endDate);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        CashFlowRecord record = new CashFlowRecord
                        {
                            RecNum = !reader.IsDBNull(reader.GetOrdinal("RecNum")) ? Convert.ToInt32(reader["RecNum"]) : 0,
                            JobNumber = !reader.IsDBNull(reader.GetOrdinal("jobnme")) ? reader["jobnme"].ToString() : string.Empty,
                            CashCollected = !reader.IsDBNull(reader.GetOrdinal("CashCollected")) ? Convert.ToDecimal(reader["CashCollected"]) : 0.0m,
                            CashPaid = !reader.IsDBNull(reader.GetOrdinal("CashPaid")) ? Convert.ToDecimal(reader["CashPaid"]) : 0.0m,
                            NetCash = !reader.IsDBNull(reader.GetOrdinal("NetCash")) ? Convert.ToDecimal(reader["NetCash"]) : 0.0m,
                            InvoiceJ2D = !reader.IsDBNull(reader.GetOrdinal("InvoiceJ2D")) ? Convert.ToDecimal(reader["InvoiceJ2D"]) : 0.0m,
                            A_REndBal_Today = !reader.IsDBNull(reader.GetOrdinal("A_REndBal_Today")) ? Convert.ToDecimal(reader["A_REndBal_Today"]) : 0.0m,
                            jobcoststodate = !reader.IsDBNull(reader.GetOrdinal("jobcoststodate")) ? Convert.ToDecimal(reader["jobcoststodate"]) : 0.0m,
                            A_PEndBal_Today = !reader.IsDBNull(reader.GetOrdinal("A_PEndBal_Today")) ? Convert.ToDecimal(reader["A_PEndBal_Today"]) : 0.0m,
                            CashPaidOut = !reader.IsDBNull(reader.GetOrdinal("CashPaidOut")) ? Convert.ToDecimal(reader["CashPaidOut"]) : 0.0m,
                            NetCashInOut = !reader.IsDBNull(reader.GetOrdinal("NetCashInOut")) ? Convert.ToDecimal(reader["NetCashInOut"]) : 0.0m  // Fixed column name
                        };

                        records.Add(record);
                    }
                }

                return Ok(records);
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
        [HttpGet("api/[controller]/GetContractSummaryData/{jobNumber}", Name = "GetContractSummaryData")]
        public async Task<IActionResult> GetContractSummary(string jobNumber)
        {
            try
            {
                List<ContractSummary> records = new List<ContractSummary>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "ContractSummary"; // Stored Procedure Name

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@JobNumber", jobNumber);

                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ContractSummary record = new ContractSummary
                                {
                                    OriginalContractAmount = reader["originalcontractamount"] != DBNull.Value ? Convert.ToDecimal(reader["originalcontractamount"]) : 0.0m,
                                    ChangesToDate = reader["changestodate"] != DBNull.Value ? Convert.ToDecimal(reader["changestodate"]) : 0.0m,
                                    NewContract = reader["newcontract"] != DBNull.Value ? Convert.ToDecimal(reader["newcontract"]) : 0.0m,
                                    InvoicedToDate = reader["invoicedtodate"] != DBNull.Value ? Convert.ToDecimal(reader["invoicedtodate"]) : 0.0m,
                                    BalanceOnContract = reader["Balanceoncontract"] != DBNull.Value ? Convert.ToDecimal(reader["Balanceoncontract"]) : 0.0m,
                                    JobName = reader["jobname"] != DBNull.Value ? reader["jobname"].ToString() : string.Empty
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                return Ok(records);

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
        [HttpGet("api/[controller]/GetPrimeChangeList/{jobNumber}", Name = "GetPrimeChangeList")]
        public async Task<IActionResult> GetPrimeChangeList(string jobNumber)
        {
            try
            {
                List<PrimeChangeList> records = new List<PrimeChangeList>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "PrimeChangeList_6_4_4_21";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@jobnumber", jobNumber);

                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                PrimeChangeList record = new PrimeChangeList
                                {
                                    JobNum = reader["jobnum"] != DBNull.Value ? reader["jobnum"].ToString() : string.Empty,
                                    JobName = reader["jobnme"] != DBNull.Value ? reader["jobnme"].ToString() : string.Empty,
                                    RecNum = reader["recnum"] != DBNull.Value ? reader["recnum"].ToString() : string.Empty,
                                    ChgNum = reader["chgnum"] != DBNull.Value ? reader["chgnum"].ToString() : string.Empty,
                                    ChgDate = reader["chgdte"] != DBNull.Value ? Convert.ToDateTime(reader["chgdte"]) : DateTime.MinValue,
                                    Description = reader["dscrpt"] != DBNull.Value ? reader["dscrpt"].ToString() : string.Empty,
                                    Status = reader["status"] != DBNull.Value ? reader["status"].ToString() : string.Empty,
                                    ReqAmt = reader["reqamt"] != DBNull.Value ? Convert.ToDecimal(reader["reqamt"]) : 0.0m,
                                    AppAmt = reader["appamt"] != DBNull.Value ? Convert.ToDecimal(reader["appamt"]) : 0.0m
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                return Ok(records);

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
        [HttpGet("api/[controller]/GetInvoiceReceivable/{jobNumber}", Name = "GetInvoiceReceivable")]
        public async Task<IActionResult> GetInvoiceReceivable(string jobNumber)
        {
            try
            {

                List<InvoiceReceivable> records = new List<InvoiceReceivable>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "InvoiceReceivable";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@JobNumber", jobNumber);

                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                InvoiceReceivable record = new InvoiceReceivable
                                {
                                    RecNum = reader["recnum"] != DBNull.Value ? Convert.ToInt32(reader["recnum"]) : 0,
                                    InvNum = reader["invnum"] != DBNull.Value ? reader["invnum"].ToString() : string.Empty,
                                    InvDate = reader["invdte"] != DBNull.Value ? Convert.ToDateTime(reader["invdte"]).ToString("MM/dd/yyyy",CultureInfo.InvariantCulture) : string.Empty,
                                    Description = reader["dscrpt"] != DBNull.Value ? reader["dscrpt"].ToString() : string.Empty,
                                    DueDate = reader["duedte"] != DBNull.Value ? Convert.ToDateTime(reader["duedte"]).ToString("MM/dd/yyyy",CultureInfo.InvariantCulture) : string.Empty,
                                    Status = reader["status"] != DBNull.Value ? Convert.ToInt32(reader["status"]) : 0,
                                    InvTtl = reader["invttl"] != DBNull.Value ? Convert.ToDecimal(reader["invttl"]) : 0.0m,
                                    AmtPad = reader["amtpad"] != DBNull.Value ? Convert.ToDecimal(reader["amtpad"]) : 0.0m,
                                    DiscCred = reader["dsctkn"] != DBNull.Value ? Convert.ToDecimal(reader["dsctkn"]) : 0.0m,
                                    InvBal = reader["invbal"] != DBNull.Value ? Convert.ToDecimal(reader["invbal"]) : 0.0m,
                                    Retain = reader["retain"] != DBNull.Value ? Convert.ToDecimal(reader["retain"]) : 0.0m,
                                    InvNet = reader["invnet"] != DBNull.Value ? Convert.ToDecimal(reader["invnet"]) : 0.0m
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                return Ok(records);

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
        [HttpGet("api/[controller]/GetARInvoice/{jobNumber}", Name = "GetARInvoice")]
        public async Task<IActionResult> GetARInvoice(string jobNumber)
        {
            try
            {
                List<InvoicePayment> records = new List<InvoicePayment>();
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "CosttoCompleteBilling3_1_2";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@JobNumber", jobNumber);

                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                InvoicePayment record = new InvoicePayment
                                {
                                    RecNum = reader["recnum"] != DBNull.Value ? Convert.ToInt32(reader["recnum"]) : 0,
                                    JobName = reader.IsDBNull(reader.GetOrdinal("jobnme")) ? string.Empty : reader["jobnme"].ToString(),
                                    InvNum = reader["invnum"] != DBNull.Value ? reader["invnum"].ToString() : string.Empty,
                                    InvDate = reader["invdte"] != DBNull.Value ? Convert.ToDateTime(reader["invdte"]).ToString("MM/dd/yyyy",CultureInfo.InvariantCulture) : string.Empty,
                                    DueDate = reader["duedte"] != DBNull.Value ? Convert.ToDateTime(reader["duedte"]).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture) : string.Empty,
                                    InvoiceTotal = reader["invttl"] != DBNull.Value ? Convert.ToDecimal(reader["invttl"]) : 0.0m,
                                    Balance = reader["invbal"] != DBNull.Value ? Convert.ToDecimal(reader["invbal"]) : 0.0m
                                };

                                records.Add(record);
                            }
                        }
                    }
                }
                return Ok(records);

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
        [HttpPost("api/[controller]/CashFlowAllDataByMulitpleJobsAndPeriod", Name = "CashFlowAllDataByMulitpleJobsAndPeriod")]
        public IActionResult CashFlowAllDataByMulitpleJobsAndPeriod([FromBody] CashFlowQueryInput input)
        {
            List<CashFlowRecord> invoicePaymentRecords = CashFlowAllByMulitpleJobsAndPeriod(input.recnum, input.StartDate, input.EndDate);

            return Ok(invoicePaymentRecords);
        }
        private List<CashFlowRecord> CashFlowAllByMulitpleJobsAndPeriod(string recnum, DateTime startDate, DateTime endDate)
        {
            List<CashFlowRecord> records = new List<CashFlowRecord>();
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SP_GetCashFlowDetailsV2";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.CommandText = query;
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@recnum", recnum);
                    command.Parameters.AddWithValue("@startDate", startDate);
                    command.Parameters.AddWithValue("@endDate", endDate);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        CashFlowRecord record = new CashFlowRecord
                        {
                            RecNum = !reader.IsDBNull(reader.GetOrdinal("RecNum")) ? Convert.ToInt32(reader["RecNum"]) : 0,
                            JobNumber = !reader.IsDBNull(reader.GetOrdinal("jobnme")) ? reader["jobnme"].ToString() : string.Empty,
                            CashCollected = !reader.IsDBNull(reader.GetOrdinal("CashCollected")) ? Convert.ToDecimal(reader["CashCollected"]) : 0.0m,
                            CashPaid = !reader.IsDBNull(reader.GetOrdinal("CashPaid")) ? Convert.ToDecimal(reader["CashPaid"]) : 0.0m,
                            NetCash = !reader.IsDBNull(reader.GetOrdinal("NetCash")) ? Convert.ToDecimal(reader["NetCash"]) : 0.0m,
                            InvoiceJ2D = !reader.IsDBNull(reader.GetOrdinal("InvoiceJ2D")) ? Convert.ToDecimal(reader["InvoiceJ2D"]) : 0.0m,
                            A_REndBal_Today = !reader.IsDBNull(reader.GetOrdinal("A_REndBal_Today")) ? Convert.ToDecimal(reader["A_REndBal_Today"]) : 0.0m,
                            jobcoststodate = !reader.IsDBNull(reader.GetOrdinal("jobcoststodate")) ? Convert.ToDecimal(reader["jobcoststodate"]) : 0.0m,
                            A_PEndBal_Today = !reader.IsDBNull(reader.GetOrdinal("A_PEndBal_Today")) ? Convert.ToDecimal(reader["A_PEndBal_Today"]) : 0.0m,
                            CashPaidOut = !reader.IsDBNull(reader.GetOrdinal("CashPaidOut")) ? Convert.ToDecimal(reader["CashPaidOut"]) : 0.0m,
                            NetCashInOut = !reader.IsDBNull(reader.GetOrdinal("NetCashInOut")) ? Convert.ToDecimal(reader["NetCashInOut"]) : 0.0m  // Fixed column name
                        };

                        records.Add(record);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return records.OrderBy(x=>x.RecNum).ToList();
        }
    }
}
