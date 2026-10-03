using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using StowellCoAPI.DTO;
using StowellCoAPI.Services;
using System.Data;

namespace StowellCoAPI.Controllers
{
    [ApiController]
   // [Route("api/[controller]")]
    //[AllowAnonymous]
    public class PmHomeController : ControllerBase
    {
        private readonly ILogger<PmHomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly GraphCalendarService _calendarService;

        public PmHomeController(ILogger<PmHomeController> logger, IConfiguration configuration, GraphCalendarService calendarService)
        {
            _logger = logger;
            _configuration = configuration;
            _logger.LogInformation($"Constructor called {_configuration.GetConnectionString("SageSBQConnection")}");
            _calendarService = calendarService;
        }
       [HttpGet("api/[controller]/GetCurrentJobs/{email}", Name = "GetCurrentJobs")]

        public async Task<IActionResult> GetCurrentJobs(string email)
        {
            _logger.LogInformation($"API start");

            var url = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";

            _logger.LogInformation($"API called: {url}");

            // FIX (2026-09-16) - was querying a legacy "jobs"/"ProjectManagementUsers"-joined
            // view (VW_ProjectUser) with fabricated Status values (JobID % 3 -> Created/Rejected/
            // Pending) and a filter that only showed every job to 2 hardcoded admin emails,
            // otherwise only jobs with an explicit per-user row in ProjectManagementUsers. That
            // table/view is unrelated to StowellSandbox.dbo.actrec - the REAL job data Win Bid
            // (ConvertBidToProject/InsertActRec) and Project Queue both use - so a newly won
            // project never showed up here ("Project Management" home / Dashboard's Current Jobs
            // grid) even though it showed correctly in Project Queue. Per team decision, now
            // queries the same real actrec data Project Queue uses, unfiltered (matches Project
            // Queue's own behavior - no per-user job-access model exists against actrec yet).
            var records = new List<CurrentJob>();
            // 2026-10-04: deliberately stays on the sandbox copy (StowellConnection), not StowellCompany, so a project won
            // or created in the app shows here straight away. (Monthly Financials reads StowellCompany.)
            string connectionString = _configuration.GetConnectionString("StowellConnection");

            string query = @"SELECT recnum, jobnme, ISNULL(addrs1,'') + ' ' + ISNULL(addrs2,'') AS Address, status, pctcmp
                              FROM actrec ORDER BY recnum DESC";
            try
            {
                using (var sqlConn = new SqlConnection(connectionString))
                using (var command = new SqlCommand(query, sqlConn))
                {
                    await sqlConn.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var record = new CurrentJob
                            {
                                JobID = reader["recnum"]?.ToString() ?? string.Empty,
                                JobName = reader["jobnme"]?.ToString() ?? string.Empty,
                                Address = reader["Address"]?.ToString()?.Trim() ?? string.Empty,
                                CreatedBy = string.Empty,
                                Status = JobStatusName(reader["status"]),
                                PercentComplete = reader["pctcmp"] == DBNull.Value ? 0m : Math.Clamp(Convert.ToDecimal(reader["pctcmp"]), 0m, 100m)
                            };

                            records.Add(record);
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
        /// <summary>Sage job status code (actrec.status) as the label used by the cash-flow status list (VwCashFlowStatus).</summary>
        private static string JobStatusName(object code)
        {
            if (code == null || code == DBNull.Value) return string.Empty;
            return Convert.ToInt32(code) switch
            {
                1 => "Bid",
                2 => "Refused",
                3 => "Contract",
                4 => "Current",
                5 => "Complete",
                6 => "Closed",
                _ => string.Empty
            };
        }

       [HttpGet("api/[controller]/GetNetCashChart", Name = "GetNetCashChart")]
        public async Task<IActionResult>  GetNetCashChart()
        {
            // chartDatasets will hold multiple charts (if needed)
            List<ChartData> chartDatasets = new List<ChartData>();
            try
            {
                List<CashFlowDetail> rd = GetCashFlowDetailsData();

                // X-axis labels (shared across datasets)
                List<string> lbls = new List<string>();

                // First dataset (example: Retain Amounts)
                List<decimal> netCash = new List<decimal>();

                foreach (var r in rd)
                {
                    lbls.Add(r.JobName);
                    netCash.Add(r.NetCashOut); ;
                }

                // Build ChartData
                ChartData chartData = new ChartData
                {
                    Labels = lbls.ToArray(),
                    Datasets = new List<ChartDataset>
                {
                    new ChartDataset
                    {
                        Label = "Net Cash Out",
                        Data = netCash.ToArray(),
                        BackgroundColor = "darkgreen",
                        BorderColor = "darkgreen"
                    }
                }
                };

                chartDatasets.Add(chartData);
                return Ok(chartDatasets);
            }
            catch (SqlException sqlEx)
            {
                // Log SQL-specific errors (e.g., connection or syntax issues)
                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving jobs.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                // Catch all unexpected exceptions
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving jobs.",
                    Details = ex.Message
                });
            }
           

        }
        [HttpGet("api/[controller]/GetCashCollectedDatasets", Name = "GetCashCollectedDatasets")]
        public async Task<IActionResult> GetCashCollectedVsCashPaidChart()
        {
            // chartDatasets will hold multiple charts (if needed)
            List<ChartData> chartDatasets = new List<ChartData>();
            try
            {
                List<CashFlowDetail> rd = GetCashFlowDetailsData();

               

                // X-axis labels (shared across datasets)
                List<string> lbls = new List<string>();

                // First dataset (example: Retain Amounts)
                List<decimal> cashCollected = new List<decimal>();
                List<decimal> cashPaid = new List<decimal>();

                foreach (var r in rd)
                {
                    lbls.Add(r.JobName);
                    cashCollected.Add(r.CashCollected);
                    cashPaid.Add(r.CashPaid);
                }

                // Build ChartData
                ChartData chartData = new ChartData
                {
                    Labels = lbls.ToArray(),
                    Datasets = new List<ChartDataset>
                {
                    new ChartDataset
                    {
                        Label = "Cash Collected",
                        Data = cashCollected.ToArray(),
                        BackgroundColor = "lightblue",
                        BorderColor = "lightblue"
                    },
                    new ChartDataset
                    {
                        Label = "Cash Paid",
                        Data = cashPaid.ToArray(),
                        BackgroundColor = "darkred",
                        BorderColor = "darkred"
                    }
                }
                };

                chartDatasets.Add(chartData);
                return Ok(chartDatasets);
            }
            catch (SqlException sqlEx)
            {
                // Log SQL-specific errors (e.g., connection or syntax issues)
                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving jobs.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                // Catch all unexpected exceptions
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving jobs.",
                    Details = ex.Message
                });
            }
            
        }
        [HttpGet("api/[controller]/GetSampleChartDatasets/{email}", Name = "GetSampleChartDatasets")]
        public async Task<IActionResult> GetSampleChartDatasets(string email)
        {
            // chartDatasets will hold multiple charts (if needed)
            List<ChartData> chartDatasets = new List<ChartData>();
            try
            {
                List<InvoiceRetention> rd = GetChartsData(email);



                // X-axis labels (shared across datasets)
                List<string> lbls = new List<string>();

                // First dataset (example: Retain Amounts)
                List<decimal> retainAmounts = new List<decimal>();

                foreach (var r in rd)
                {
                    lbls.Add(r.filenumber);
                    retainAmounts.Add(r.total_retain);
                }

                // Build ChartData
                ChartData chartData = new ChartData
                {
                    Labels = lbls.ToArray(),
                    Datasets = new List<ChartDataset>
                {
                    new ChartDataset
                    {
                        Label = "Retain Amount",
                        Data = retainAmounts.ToArray(),
                        BackgroundColor = "blue",
                        BorderColor = "blue"
                    }
                }
                };

                chartDatasets.Add(chartData);
                return Ok(chartDatasets);
            }
            catch (SqlException sqlEx)
            {
                // Log SQL-specific errors (e.g., connection or syntax issues)
                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving jobs.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                // Catch all unexpected exceptions
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving jobs.",
                    Details = ex.Message
                });
            }
            
        }
        private List<InvoiceRetention> GetChartsData(string email)
        {
            List<InvoiceRetention> costCodeRecords = new List<InvoiceRetention>();
            // var currentUser = HttpContext.User.Identity.Name;
           // var currentUser = "p360admin@stowellinc.com";

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            //string query = "SELECT * FROM VW_Invoiceretention_ByEmail WHERE EmailID = @EmailID  ORDER by file_number";
            string query = "sp_GetInvoiceretentionByEmail";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@EmailID", email);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    InvoiceRetention record = new InvoiceRetention
                    {
                        filenumber = reader.IsDBNull(reader.GetOrdinal("file_number")) ? string.Empty : reader["file_number"].ToString(),
                        total_retain = reader.IsDBNull(reader.GetOrdinal("total_retain($)")) ? 0.0m : Convert.ToDecimal(reader["total_retain($)"])
                    };

                    costCodeRecords.Add(record);
                }
            }

            return costCodeRecords;
        }
        private List<CashFlowDetail> GetCashFlowDetailsData()
        {
            List<CashFlowDetail> costCodeRecords = new List<CashFlowDetail>();
           // var currentUser = HttpContext.User.Identity.Name;
            //var currentUser = "p360admin@stowellinc.com";
            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            string query = "SP_GetCashFlowDetails_AllJobs_Top5";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@startdate", "2024-08-15");
                command.Parameters.AddWithValue("@enddate", "2025-08-15");

           // command.Parameters.AddWithValue("@EmailID", currentUser);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    CashFlowDetail record = new CashFlowDetail
                    {
                        JobName = reader.IsDBNull(reader.GetOrdinal("jobnme")) ? string.Empty : reader["jobnme"].ToString(),
                        CashCollected = reader.IsDBNull(reader.GetOrdinal("CashCollected")) ? 0.0m : Convert.ToDecimal(reader["CashCollected"]),
                        CashPaid = reader.IsDBNull(reader.GetOrdinal("CashPaid")) ? 0.0m : Convert.ToDecimal(reader["CashPaid"]),
                        NetCashOut = reader.IsDBNull(reader.GetOrdinal("NetCashInOut")) ? 0.0m : Convert.ToDecimal(reader["NetCashInOut"])
                    };

                    costCodeRecords.Add(record);
                }
            }

            return costCodeRecords;
        }
        //  [HttpGet("{email}")]
        [HttpGet("api/[controller]/GetCalendarEvents", Name = "GetCalendarEvents")]
        public async Task<IActionResult> GetUserCalendar()
        {
            _logger.LogInformation($"API start");

            //var url = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";

            _logger.LogInformation($"API called: GetUserCalendar");
            try
            {
                //var records = new List<CurrentJob>();
                //string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                //_logger.LogInformation($"Connection string: {connectionString}");
                var events = await _calendarService.GetUserCalendarAsync(_configuration["EventsEmail"]);
                _logger.LogInformation($"events: {JsonConvert.SerializeObject(events)}");
                return Ok(events);
            }
            catch (SqlException sqlEx)
            {
                // Log SQL-specific errors (e.g., connection or syntax issues)
                return StatusCode(500, new
                {
                    Message = "sql exception",
                    Details = sqlEx.Message
                });
                _logger.LogError(sqlEx, sqlEx.Message);
            }
            catch (Exception ex)
            {
                // Catch all unexpected exceptions
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving calendar events.",
                    Details = ex.Message
                });
                _logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>Adds an event to the same shared calendar that GetCalendarEvents reads (EventsEmail).</summary>
        [HttpPost("api/[controller]/CreateCalendarEvent", Name = "CreateCalendarEvent")]
        public async Task<IActionResult> CreateCalendarEvent([FromBody] CreateCalendarEventRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Subject))
                return BadRequest(new { Message = "A title is required." });
            if (request.End <= request.Start)
                return BadRequest(new { Message = "The end must be after the start." });

            try
            {
                var created = await _calendarService.CreateEventAsync(_configuration["EventsEmail"], request.Subject.Trim(), request.Start, request.End, request.IsAllDay);
                return Ok(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateCalendarEvent failed");
                // most likely the app registration lacks the Calendars.ReadWrite application permission
                return StatusCode(502, new
                {
                    Message = "The event could not be saved to the calendar.",
                    Details = ex.Message
                });
            }
        }
    }
}
