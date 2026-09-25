using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StowellCoAPI.DTO;
using StowellCoAPI.Models;
using System;
using System.Data;

namespace StowellCoAPI.Controllers
{
   // [Route("api/[controller]")]
    [ApiController]
    public class BidsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BidsController> _logger;

        public BidsController(ILogger<BidsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        [HttpGet("api/[controller]/GetCurrentBids", Name = "GetCurrentBids")]
        public async Task<IActionResult> GetCurrentBids()
        {
            try
            {
                List<Bids> currentbids = new List<Bids>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SELECT * FROM vw_BidQueue_Current";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Bids record = new Bids
                        {
                            BidId = !reader.IsDBNull(reader.GetOrdinal("BidID")) ? Convert.ToString(reader["BidID"]) : string.Empty,
                            BidName = !reader.IsDBNull(reader.GetOrdinal("BidName")) ? reader["BidName"].ToString() : string.Empty,
                            Bidder = !reader.IsDBNull(reader.GetOrdinal("Bidder")) ? Convert.ToString(reader["Bidder"]) : string.Empty,
                            Status = !reader.IsDBNull(reader.GetOrdinal("Status")) ? Convert.ToString(reader["Status"]) : string.Empty,
                        };

                        currentbids.Add(record);
                    }
                }
                return Ok(currentbids);
            }
            catch (SqlException sqlEx)
            {
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
        [HttpGet("api/[controller]/GetBidQueueData", Name = "GetBidQueueData")]
        public async Task<IActionResult> GetBidQueueData()
        {
            try
            {
                BidQueue bidQueue = new BidQueue();
                List<Bids> currentbids = new List<Bids>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                string query = "SELECT * FROM vw_BidQueue_Current";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Bids record = new Bids
                        {
                            BidId = !reader.IsDBNull(reader.GetOrdinal("BidID")) ? Convert.ToString(reader["BidID"]) : string.Empty,
                            BidName = !reader.IsDBNull(reader.GetOrdinal("BidName")) ? reader["BidName"].ToString() : string.Empty,
                            BidManager = !reader.IsDBNull(reader.GetOrdinal("Bidder")) ? Convert.ToString(reader["Bidder"]) : string.Empty,
                            Status = !reader.IsDBNull(reader.GetOrdinal("Status")) ? Convert.ToString(reader["Status"]) : string.Empty,
                        };

                        currentbids.Add(record);
                    }
                }
                List<Bids> closedbids = new List<Bids>();

                query = "SELECT * FROM vw_BidQueue_Closed";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Bids record = new Bids
                        {
                            BidId = !reader.IsDBNull(reader.GetOrdinal("BidID")) ? Convert.ToString(reader["BidID"]) : string.Empty,
                            BidName = !reader.IsDBNull(reader.GetOrdinal("BidName")) ? reader["BidName"].ToString() : string.Empty,
                            BidManager = !reader.IsDBNull(reader.GetOrdinal("Bid Manager")) ? Convert.ToString(reader["Bid Manager"]) : string.Empty,
                            Status = !reader.IsDBNull(reader.GetOrdinal("Status")) ? Convert.ToString(reader["Status"]) : string.Empty,
                        };

                        closedbids.Add(record);
                    }
                }
                bidQueue.CurrentBids = currentbids;
                bidQueue.ClosedBids = closedbids;
                return Ok(bidQueue);
            }
            catch (SqlException sqlEx)
            {
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
        [HttpPost("api/[controller]/CreateNewBid", Name = "CreateNewBid")]
        public async Task<IActionResult> CreateNewBid([FromBody] CreateBidRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_CreateNewBid", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Parameters
                        command.Parameters.AddWithValue("@DisplayBidID", request.BidId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Department", request.Department ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Division", request.Division ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@BidName", request.BidName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Address1", request.Address1 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@City", request.City ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@State", request.State ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ZipCode", request.ZipCode ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedBy", request.CreatedBy ?? (object)DBNull.Value);

                        await connection.OpenAsync();

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        // FIX (2026-09-15) - found via a live "folder created but never appears
                        // anywhere" report: this used to call CreateFolder with
                        // Path.Combine(BidsDocSettings:RootSharePath, request.BidId) - a FLAT
                        // path with no Department/Division segments. But sp_CreateNewBid computes
                        // and stores a DIFFERENT, NESTED path in BidRecords.DocumentFolderPath
                        // (STO-FS01\Production\Bids\{Department}\{Division}\{BidId} - see that
                        // proc's @FolderPath logic) - the same value GetBidById/
                        // BidsDocumentManagementController's ResolveBidDocumentFolder already read
                        // back and rely on everywhere else. So the folder WAS being created - just
                        // at a flat location nothing else ever looks for, while the nested path
                        // everything else expects was never created at all. Looking up the same
                        // stored DocumentFolderPath the proc just wrote instead of recomputing a
                        // different path here.
                        string resolvedFolderPath = null;
                        using (SqlCommand pathCommand = new SqlCommand(
                            "SELECT DocumentFolderPath FROM BidRecords WHERE DisplayBidID = @DisplayBidID", connection))
                        {
                            pathCommand.Parameters.AddWithValue("@DisplayBidID", request.BidId ?? (object)DBNull.Value);
                            var pathResult = await pathCommand.ExecuteScalarAsync() as string;
                            if (!string.IsNullOrWhiteSpace(pathResult))
                            {
                                resolvedFolderPath = pathResult.StartsWith("\\\\") ? pathResult : "\\\\" + pathResult;
                            }
                        }

                        // The bid row above is the operation that matters to the caller - it's
                        // already committed at this point. Provisioning the bid's document folder
                        // is a best-effort side effect on top of that, not part of "did the bid
                        // get created" - a folder-provisioning problem is caught and logged here
                        // instead of failing/misreporting bid creation itself.
                        string folderWarning = null;
                        try
                        {
                            if (resolvedFolderPath != null)
                            {
                                // destinationRoot = the full resolved path, folderName = "" -
                                // Path.Combine(x, "") returns x unchanged, so CreateFolder creates
                                // exactly resolvedFolderPath rather than appending BidId again.
                                await CreateFolder(_configuration["BidsDocSettings:RootPath"], resolvedFolderPath, "");
                            }
                            else
                            {
                                await CreateFolder(_configuration["BidsDocSettings:RootPath"], _configuration["BidsDocSettings:RootSharePath"], request.BidId);
                            }
                        }
                        catch (Exception folderEx)
                        {
                            _logger.LogError(folderEx, "Bid {BidId} was created but its document folder could not be provisioned.", request.BidId);
                            folderWarning = $"Bid created, but its document folder could not be set up: {folderEx.Message}";
                        }

                        return Ok(new
                        {
                            Message = "Bid created successfully.",
                            RowsAffected = rowsAffected,
                            FolderWarning = folderWarning
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while creating the bid.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while creating the bid.",
                    Details = ex.Message
                });
            }
        }

  [HttpGet("api/[controller]/GetNextDisplayBidID/{createdBy}", Name = "GetNextDisplayBidID")]
        public async Task<IActionResult> GetNextDisplayBidID(string createdBy)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using SqlConnection con = new SqlConnection(connectionString);
                using SqlCommand cmd = new SqlCommand("sp_GetNextDisplayBidID", con);

                cmd.CommandType = CommandType.StoredProcedure;

                // Input parameter
                cmd.Parameters.AddWithValue("@CreatedBy", createdBy);

                // Output parameter
                SqlParameter outputParam = new SqlParameter("@DisplayBidID", SqlDbType.VarChar, 20)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(outputParam);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                string displayBidID = outputParam.Value?.ToString();

                return Ok(new
                {
                    Success = true,
                    DisplayBidID = displayBidID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
        [HttpGet("api/[controller]/GetBidById/{displayBidId}", Name = "GetBidById")]
        public async Task<IActionResult> GetBidById(string displayBidId)
        {
            try
            {
                BidRecords bid = null;

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_GetBidById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DisplayBidID", displayBidId);

                        await connection.OpenAsync();

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            bid = new BidRecords
                            {
                                InternalBidID = !reader.IsDBNull(reader.GetOrdinal("InternalBidID"))
                                    ? Convert.ToInt32(reader["InternalBidID"])
                                    : 0,

                                DisplayBidID = !reader.IsDBNull(reader.GetOrdinal("DisplayBidID"))
                                    ? reader["DisplayBidID"].ToString()
                                    : string.Empty,

                                IsProject = !reader.IsDBNull(reader.GetOrdinal("IsProject"))
                                    ? Convert.ToBoolean(reader["IsProject"])
                                    : false,

                                Department = !reader.IsDBNull(reader.GetOrdinal("Department"))
                                    ? reader["Department"].ToString()
                                    : string.Empty,

                                Division = !reader.IsDBNull(reader.GetOrdinal("Division"))
                                    ? reader["Division"].ToString()
                                    : string.Empty,

                                BidName = !reader.IsDBNull(reader.GetOrdinal("BidName"))
                                    ? reader["BidName"].ToString()
                                    : string.Empty,

                                BidPhase = !reader.IsDBNull(reader.GetOrdinal("BidPhase"))
                                    ? reader["BidPhase"].ToString()
                                    : string.Empty,

                                BidStatus = !reader.IsDBNull(reader.GetOrdinal("BidStatus"))
                                    ? reader["BidStatus"].ToString()
                                    : string.Empty,

                                Jobsite = !reader.IsDBNull(reader.GetOrdinal("Jobsite"))
                                    ? reader["Jobsite"].ToString()
                                    : string.Empty,

                                Address1 = !reader.IsDBNull(reader.GetOrdinal("Address1"))
                                    ? reader["Address1"].ToString()
                                    : string.Empty,

                                Address2 = !reader.IsDBNull(reader.GetOrdinal("Address2"))
                                    ? reader["Address2"].ToString()
                                    : string.Empty,

                                City = !reader.IsDBNull(reader.GetOrdinal("City"))
                                    ? reader["City"].ToString()
                                    : string.Empty,

                                State = !reader.IsDBNull(reader.GetOrdinal("State"))
                                    ? reader["State"].ToString()
                                    : string.Empty,

                                ZipCode = !reader.IsDBNull(reader.GetOrdinal("ZipCode"))
                                    ? reader["ZipCode"].ToString()
                                    : string.Empty,

                                SalesTaxDistrict = !reader.IsDBNull(reader.GetOrdinal("SalesTaxDistrict"))
                                    ? reader["SalesTaxDistrict"].ToString()
                                    : string.Empty,

                                ClientName = !reader.IsDBNull(reader.GetOrdinal("ClientName"))
                                    ? reader["ClientName"].ToString()
                                    : string.Empty,

                                GC_Address1 = !reader.IsDBNull(reader.GetOrdinal("GC_Address1"))
                                    ? reader["GC_Address1"].ToString()
                                    : string.Empty,

                                GC_Address2 = !reader.IsDBNull(reader.GetOrdinal("GC_Address2"))
                                    ? reader["GC_Address2"].ToString()
                                    : string.Empty,

                                GC_City = !reader.IsDBNull(reader.GetOrdinal("GC_City"))
                                    ? reader["GC_City"].ToString()
                                    : string.Empty,

                                GC_State = !reader.IsDBNull(reader.GetOrdinal("GC_State"))
                                    ? reader["GC_State"].ToString()
                                    : string.Empty,

                                GC_ZipCode = !reader.IsDBNull(reader.GetOrdinal("GC_ZipCode"))
                                    ? reader["GC_ZipCode"].ToString()
                                    : string.Empty,

                                GC_SalesTaxDistrict = !reader.IsDBNull(reader.GetOrdinal("GC_SalesTaxDistrict"))
                                    ? reader["GC_SalesTaxDistrict"].ToString()
                                    : string.Empty,

                                DocumentFolderPath = !reader.IsDBNull(reader.GetOrdinal("DocumentFolderPath"))
                                    ? reader["DocumentFolderPath"].ToString()
                                    : string.Empty,

                                HasBidDocumentLibrary = !reader.IsDBNull(reader.GetOrdinal("HasBidDocumentLibrary"))
                                    ? Convert.ToBoolean(reader["HasBidDocumentLibrary"])
                                    : false,

                                CreatedDate = !reader.IsDBNull(reader.GetOrdinal("CreatedDate"))
                                    ? Convert.ToDateTime(reader["CreatedDate"])
                                    : (DateTime?)null,

                                CreatedBy = !reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                                    ? reader["CreatedBy"].ToString()
                                    : string.Empty,

                                ModifiedDate = !reader.IsDBNull(reader.GetOrdinal("ModifiedDate"))
                                    ? Convert.ToDateTime(reader["ModifiedDate"])
                                    : (DateTime?)null,

                                InsuranceType = !reader.IsDBNull(reader.GetOrdinal("InsuranceType"))
                                    ? reader["InsuranceType"].ToString()
                                    : string.Empty,

                                Bonded = !reader.IsDBNull(reader.GetOrdinal("Bonded"))
                                    ? Convert.ToString(reader["Bonded"])
                                    : "No",

                                TaxExempt = !reader.IsDBNull(reader.GetOrdinal("TaxExempt"))
                                    ? Convert.ToString(reader["TaxExempt"])
                                    : "No",

                                InvoiceSubmittal = !reader.IsDBNull(reader.GetOrdinal("InvoiceSubmittal"))
                                    ? reader["InvoiceSubmittal"].ToString()
                                    : string.Empty,

                                CertifiedPayroll = !reader.IsDBNull(reader.GetOrdinal("CertifiedPayroll"))
                                    ? Convert.ToString(reader["CertifiedPayroll"])
                                   : "No",

                                NetTermsDate = !reader.IsDBNull(reader.GetOrdinal("NetTermsDate"))
                                    ? Convert.ToDateTime(reader["NetTermsDate"])
                                    : (DateTime?)null,

                                RetainagePercentage = !reader.IsDBNull(reader.GetOrdinal("RetainagePercentage"))
                                    ? Convert.ToDecimal(reader["RetainagePercentage"])
                                    : 0
                            };
                        }
                    }
                }

                if (bid == null)
                {
                    return NotFound(new
                    {
                        Message = $"No bid found with DisplayBidID = {displayBidId}"
                    });
                }

                return Ok(bid);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving bid.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving bid.",
                    Details = ex.Message
                });
            }
        }
        [HttpGet("api/[controller]/GetBidAmounts/{displayBidId}", Name = "GetBidAmounts")]
        public async Task<IActionResult> GetBidAmounts(string displayBidId)
        {
            try
            {
                List<BidAmounts> bidAmounts = new List<BidAmounts>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_GetBidAmounts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DisplayBidID", displayBidId);

                        await connection.OpenAsync();

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            BidAmounts record = new BidAmounts
                            {
                                BidAmountID = !reader.IsDBNull(reader.GetOrdinal("BidAmountID"))
                                    ? Convert.ToInt32(reader["BidAmountID"])
                                    : 0,

                                InternalBidID = !reader.IsDBNull(reader.GetOrdinal("InternalBidID"))
                                    ? Convert.ToInt32(reader["InternalBidID"])
                                    : 0,

                                BidDate = !reader.IsDBNull(reader.GetOrdinal("BidDate"))
                                    ? Convert.ToDateTime(reader["BidDate"])
                                    : (DateTime?)null,

                                BidAmount = !reader.IsDBNull(reader.GetOrdinal("BidAmount"))
                                    ? Convert.ToDecimal(reader["BidAmount"])
                                    : 0,

                                Status = !reader.IsDBNull(reader.GetOrdinal("Status"))
                                    ? reader["Status"].ToString()
                                    : string.Empty,

                                CreatedDate = !reader.IsDBNull(reader.GetOrdinal("CreatedDate"))
                                    ? Convert.ToDateTime(reader["CreatedDate"])
                                    : (DateTime?)null
                            };

                            bidAmounts.Add(record);
                        }
                    }
                }

                return Ok(bidAmounts);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving bid amounts.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving bid amounts.",
                    Details = ex.Message
                });
            }
        }
        // NEW - Notes table on Bid Overview's Overview tab (see BidNote model's header comment -
        // was built on the Angular side with no backend, 404'd on every call). Mirrors
        // GetBidAmounts/AddBidAmount immediately above/below exactly.
        [HttpGet("api/[controller]/GetBidNotes/{displayBidId}", Name = "GetBidNotes")]
        public async Task<IActionResult> GetBidNotes(string displayBidId)
        {
            try
            {
                List<BidNote> bidNotes = new List<BidNote>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_GetBidNotes", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DisplayBidID", displayBidId);

                        await connection.OpenAsync();

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            BidNote record = new BidNote
                            {
                                NoteId = !reader.IsDBNull(reader.GetOrdinal("NoteId"))
                                    ? Convert.ToInt32(reader["NoteId"])
                                    : 0,

                                InternalBidID = !reader.IsDBNull(reader.GetOrdinal("InternalBidID"))
                                    ? Convert.ToInt32(reader["InternalBidID"])
                                    : 0,

                                NoteDate = !reader.IsDBNull(reader.GetOrdinal("NoteDate"))
                                    ? Convert.ToDateTime(reader["NoteDate"])
                                    : (DateTime?)null,

                                Note = !reader.IsDBNull(reader.GetOrdinal("Note"))
                                    ? reader["Note"].ToString()
                                    : string.Empty,

                                EnteredBy = !reader.IsDBNull(reader.GetOrdinal("EnteredBy"))
                                    ? reader["EnteredBy"].ToString()
                                    : string.Empty
                            };

                            bidNotes.Add(record);
                        }
                    }
                }

                return Ok(bidNotes);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving bid notes.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving bid notes.",
                    Details = ex.Message
                });
            }
        }

        [HttpPost("api/[controller]/AddBidNote", Name = "AddBidNote")]
        public async Task<IActionResult> AddBidNote([FromBody] AddBidNoteRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_AddBidNote", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@InternalBidID", request.InternalBidID);
                        command.Parameters.AddWithValue("@NoteDate", request.NoteDate);
                        command.Parameters.AddWithValue("@Note", request.Note);
                        command.Parameters.AddWithValue("@EnteredBy", request.EnteredBy);

                        await connection.OpenAsync();

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        string result = string.Empty;
                        int newNoteId = 0;

                        if (await reader.ReadAsync())
                        {
                            result = !reader.IsDBNull(reader.GetOrdinal("Result"))
                                ? reader["Result"].ToString()
                                : string.Empty;

                            newNoteId = !reader.IsDBNull(reader.GetOrdinal("NewNoteID"))
                                ? Convert.ToInt32(reader["NewNoteID"])
                                : 0;
                        }

                        return Ok(new
                        {
                            Message = result,
                            NewNoteID = newNoteId
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while saving the bid note.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while saving the bid note.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("api/[controller]/GetBidAccessSummary/{displayBidId}", Name = "GetBidAccessSummary")]
        public async Task<IActionResult> GetBidAccessSummary(string displayBidId)
        {
            try
            {
                List<BidAccessSummary> accessSummary = new List<BidAccessSummary>();

                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_GetBidAccessSummary", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DisplayBidID", displayBidId);

                        await connection.OpenAsync();

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            BidAccessSummary record = new BidAccessSummary
                            {
                                BidAccessID = !reader.IsDBNull(reader.GetOrdinal("BidAccessID"))
                                    ? Convert.ToInt32(reader["BidAccessID"])
                                    : 0,

                                EmployeeName = !reader.IsDBNull(reader.GetOrdinal("EmployeeName"))
                                    ? reader["EmployeeName"].ToString()
                                    : string.Empty,

                                RoleName = !reader.IsDBNull(reader.GetOrdinal("RoleName"))
                                    ? reader["RoleName"].ToString()
                                    : string.Empty,

                                CreatedDate = !reader.IsDBNull(reader.GetOrdinal("CreatedDate"))
                                    ? Convert.ToDateTime(reader["CreatedDate"])
                                    : (DateTime?)null
                            };

                            accessSummary.Add(record);
                        }
                    }
                }

                return Ok(accessSummary.Where(x=>x.BidAccessID>0).ToList());
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving bid access summary.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving bid access summary.",
                    Details = ex.Message
                });
            }
        }
        [HttpPost("api/[controller]/UpdateBidDetails", Name = "UpdateBidDetails")]
        public async Task<IActionResult> UpdateBidDetails([FromBody] UpdateBidDetailsRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_UpdateBidDetails", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@InternalBidID", request.InternalBidID);

                        // Bid Overview
                        command.Parameters.AddWithValue("@Department",
                            (object?)request.Department ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Division",
                            (object?)request.Division ?? DBNull.Value);

                        command.Parameters.AddWithValue("@BidName",
                            (object?)request.BidName ?? DBNull.Value);

                        command.Parameters.AddWithValue("@BidPhase",
                            (object?)request.BidPhase ?? DBNull.Value);

                        command.Parameters.AddWithValue("@BidStatus",
                            (object?)request.BidStatus ?? DBNull.Value);

                        // Property Address
                        command.Parameters.AddWithValue("@Jobsite",
                            (object?)request.Jobsite ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Address1",
                            (object?)request.Address1 ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Address2",
                            (object?)request.Address2 ?? DBNull.Value);

                        command.Parameters.AddWithValue("@City",
                            (object?)request.City ?? DBNull.Value);

                        command.Parameters.AddWithValue("@State",
                            (object?)request.State ?? DBNull.Value);

                        command.Parameters.AddWithValue("@ZipCode",
                            (object?)request.ZipCode ?? DBNull.Value);

                        command.Parameters.AddWithValue("@SalesTaxDistrict",
                            (object?)request.SalesTaxDistrict ?? DBNull.Value);

                        // General Contractor
                        command.Parameters.AddWithValue("@ClientName",
                            (object?)request.ClientName ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GC_Address1",
                            (object?)request.GC_Address1 ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GC_Address2",
                            (object?)request.GC_Address2 ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GC_City",
                            (object?)request.GC_City ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GC_State",
                            (object?)request.GC_State ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GC_ZipCode",
                            (object?)request.GC_ZipCode ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GC_SalesTaxDistrict",
                            (object?)request.GC_SalesTaxDistrict ?? DBNull.Value);

                        // Project Information
                        command.Parameters.AddWithValue("@InsuranceType",
                            (object?)request.InsuranceType ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Bonded",
                            (object?)request.Bonded ?? DBNull.Value);

                        command.Parameters.AddWithValue("@TaxExempt",
                            (object?)request.TaxExempt ?? DBNull.Value);

                        command.Parameters.AddWithValue("@InvoiceSubmittal",
                            (object?)request.InvoiceSubmittal ?? DBNull.Value);

                        command.Parameters.AddWithValue("@CertifiedPayroll",
                            (object?)request.CertifiedPayroll ?? DBNull.Value);

                        command.Parameters.AddWithValue("@NetTermsDate",
                            (object?)request.NetTermsDate ?? DBNull.Value);

                        command.Parameters.AddWithValue("@RetainagePercentage",
                            (object?)request.RetainagePercentage ?? DBNull.Value);

                        // Audit
                        command.Parameters.AddWithValue("@ModifiedBy",
                            request.ModifiedBy);

                        await connection.OpenAsync();

                        object result = await command.ExecuteScalarAsync();

                        return Ok(new
                        {
                            Message = result?.ToString()
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while updating bid details.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while updating bid details.",
                    Details = ex.Message
                });
            }
        }
        [HttpPost("api/[controller]/AddBidAmount", Name = "AddBidAmount")]
        public async Task<IActionResult> AddBidAmount([FromBody] AddBidAmountRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_AddBidAmount", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@InternalBidID", request.InternalBidID);

                        command.Parameters.AddWithValue("@BidDate", request.BidDate);

                        command.Parameters.AddWithValue("@BidAmount", request.BidAmount);

                        command.Parameters.AddWithValue("@Status",
                            (object?)request.Status ?? DBNull.Value);

                        command.Parameters.AddWithValue("@ModifiedBy",
                            request.ModifiedBy);

                        await connection.OpenAsync();

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        string result = string.Empty;
                        int newBidAmountID = 0;

                        if (await reader.ReadAsync())
                        {
                            result = !reader.IsDBNull(reader.GetOrdinal("Result"))
                                ? reader["Result"].ToString()
                                : string.Empty;

                            newBidAmountID = !reader.IsDBNull(reader.GetOrdinal("NewBidAmountID"))
                                ? Convert.ToInt32(reader["NewBidAmountID"])
                                : 0;
                        }

                        return Ok(new
                        {
                            Message = result,
                            NewBidAmountID = newBidAmountID
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while adding bid amount.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while adding bid amount.",
                    Details = ex.Message
                });
            }
        }
        [HttpPut("api/[controller]/UpdateBidAmount", Name = "UpdateBidAmount")]
        public async Task<IActionResult> UpdateBidAmount([FromBody] UpdateBidAmountRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_UpdateBidAmount", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@BidAmountID", request.BidAmountID);

                        command.Parameters.AddWithValue("@BidDate",
                            (object?)request.BidDate ?? DBNull.Value);

                        command.Parameters.AddWithValue("@BidAmount",
                            (object?)request.BidAmount ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Status",
                            (object?)request.Status ?? DBNull.Value);

                        command.Parameters.AddWithValue("@ModifiedBy",
                            request.ModifiedBy);

                        await connection.OpenAsync();

                        object result = await command.ExecuteScalarAsync();

                        return Ok(new
                        {
                            Message = result?.ToString()
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while updating bid amount.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while updating bid amount.",
                    Details = ex.Message
                });
            }
        }
        [HttpDelete("api/[controller]/DeleteBidAmount", Name = "Delete_BidAmount")]
        public async Task<IActionResult> DeleteBidAmount([FromBody] DeleteBidAmountRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_DeleteBidAmount", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@BidAmountID", request.BidAmountID);

                        command.Parameters.AddWithValue("@ModifiedBy",
                            request.ModifiedBy);

                        await connection.OpenAsync();

                        object result = await command.ExecuteScalarAsync();

                        return Ok(new
                        {
                            Message = result?.ToString()
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while deleting bid amount.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while deleting bid amount.",
                    Details = ex.Message
                });
            }
        }
        [HttpPost("api/[controller]/AddBidAccess", Name = "AddBidAccess")]
        public async Task<IActionResult> AddBidAccess([FromBody] AddBidAccessRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_AddBidAccess", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@InternalBidID", request.InternalBidID);

                        command.Parameters.AddWithValue("@RoleName",
                            (object?)request.RoleName ?? DBNull.Value);

                        command.Parameters.AddWithValue("@EmployeeName",
                            (object?)request.EmployeeName ?? DBNull.Value);

                        command.Parameters.AddWithValue("@ModifiedBy",
                            request.ModifiedBy);

                        await connection.OpenAsync();

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        string result = string.Empty;
                        int newBidAccessID = 0;

                        if (await reader.ReadAsync())
                        {
                            result = !reader.IsDBNull(reader.GetOrdinal("Result"))
                                ? reader["Result"].ToString()
                                : string.Empty;

                            newBidAccessID = !reader.IsDBNull(reader.GetOrdinal("NewBidAccessID"))
                                ? Convert.ToInt32(reader["NewBidAccessID"])
                                : 0;
                        }

                        return Ok(new
                        {
                            Message = result,
                            NewBidAccessID = newBidAccessID
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while adding bid access.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while adding bid access.",
                    Details = ex.Message
                });
            }
        }
        [HttpDelete("api/[controller]/RemoveBidAccess", Name = "RemoveBidAccess")]
        public async Task<IActionResult> RemoveBidAccess([FromBody] RemoveBidAccessRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_RemoveBidAccess", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@BidAccessID", request.BidAccessID);

                        command.Parameters.AddWithValue("@ModifiedBy",
                            request.ModifiedBy);

                        await connection.OpenAsync();

                        object result = await command.ExecuteScalarAsync();

                        return Ok(new
                        {
                            Message = result?.ToString()
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while removing bid access.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while removing bid access.",
                    Details = ex.Message
                });
            }
        }
        [HttpPost("api/[controller]/ConvertBidToProject", Name = "ConvertBidToProject")]
        public async Task<IActionResult> ConvertBidToProject([FromBody] ConvertBidToProjectRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_ConvertBidToProject", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@InternalBidID", request.InternalBidID);

                        command.Parameters.AddWithValue("@ModifiedBy", request.ModifiedBy);

                        await connection.OpenAsync();

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        string result = string.Empty;
                        string newProjectId = string.Empty;
                        string newProjectRecNum = string.Empty;
                        string newProjectName = string.Empty;

                        if (await reader.ReadAsync())
                        {
                            result = !reader.IsDBNull(reader.GetOrdinal("Result"))
                                ? reader["Result"].ToString()
                                : string.Empty;

                            if (reader.HasColumn("NewProjectID") && !reader.IsDBNull(reader.GetOrdinal("NewProjectID")))
                            {
                                newProjectId = reader["NewProjectID"].ToString();
                            }

                            if (reader.HasColumn("NewProjectRecNum") && !reader.IsDBNull(reader.GetOrdinal("NewProjectRecNum")))
                            {
                                newProjectRecNum = reader["NewProjectRecNum"].ToString();
                            }

                            if (reader.HasColumn("NewProjectName") && !reader.IsDBNull(reader.GetOrdinal("NewProjectName")))
                            {
                                newProjectName = reader["NewProjectName"].ToString();
                            }
                        }
                        reader.Close();

                        // NEW (2026-09-15, per Mike Smith) - "project folders, etc." Same
                        // non-blocking pattern as CreateNewBid's CreateFolder call (see that
                        // action's comment): a folder-provisioning failure shouldn't undo or
                        // misreport a project that was already created above. ProjectDocSettings
                        // now points at the real confirmed share (see appsettings.json comment) -
                        // confirmed via a live "Could not find a part of the path" error on job
                        // 12162402's Documents tab, which also confirmed the expected folder NAME
                        // format the Documents tab actually looks for: "{recnum} - {jobName}"
                        // (Angular's project-file-manager.component.ts uses `fullJob`, which is
                        // built that way - see GetCurrentCostSummaryViewModel's `jobs` array) -
                        // previously this passed just the bare recnum, which would never have
                        // matched what the Documents tab looks up regardless of the share being
                        // reachable.
                        string folderWarning = null;
                        if (!string.IsNullOrEmpty(newProjectRecNum))
                        {
                            string projectFolderName = string.IsNullOrEmpty(newProjectName)
                                ? newProjectRecNum
                                : $"{newProjectRecNum} - {newProjectName}";
                            try
                            {
                                await CreateFolder(_configuration["ProjectDocSettings:RootPath"], _configuration["ProjectDocSettings:RootSharePath"], projectFolderName);
                            }
                            catch (Exception folderEx)
                            {
                                _logger.LogError(folderEx, "Project {NewProjectRecNum} was created but its document folder could not be provisioned.", newProjectRecNum);
                                // TEMP (2026-09-15) - includes the real exception message instead
                                // of a fixed generic string, to find out whether this is the same
                                // STO-FS01 network issue Bids document browsing hit or something
                                // more specific (e.g. ProjectDocSettings:RootPath's guessed
                                // template folder name being wrong) - revert to the generic
                                // message once diagnosed, this exposes internal path details to
                                // the client which isn't something to ship long-term.
                                folderWarning = $"Project created, but its document folder could not be set up: {folderEx.Message}";
                            }
                        }

                        return Ok(new
                        {
                            Message = result,
                            NewProjectID = newProjectId,
                            NewProjectRecNum = newProjectRecNum,
                            FolderWarning = folderWarning
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while converting bid to project.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while converting bid to project.",
                    Details = ex.Message
                });
            }
        }
        [HttpPost("api/[controller]/CloseBidAsLost", Name = "CloseBidAsLost")]
        public async Task<IActionResult> CloseBidAsLost([FromBody] CloseBidAsLostRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_CloseBidAsLost", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@InternalBidID", request.InternalBidID);
                        command.Parameters.AddWithValue("@ModifiedBy", string.IsNullOrWhiteSpace(request.ModifiedBy) ? "system" : request.ModifiedBy);

                        await connection.OpenAsync();

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            string result = string.Empty;

                            if (await reader.ReadAsync())
                            {
                                result = reader["Result"] != DBNull.Value
                                    ? reader["Result"].ToString()
                                    : string.Empty;
                            }

                            return Ok(new
                            {
                                Message = result
                            });
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while closing bid as lost.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while closing bid as lost.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("api/[controller]/GetContractorClientByName/{clientName}", Name = "GetContractorClientByName")]
        public async Task<IActionResult> GetContractorClientByName(string clientName)
        {
            try
            {
                // Fetch all clients for this ID
                var clients = await GetContractorClientRecords(clientName);

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
        private async Task<List<ContractorClient>> GetContractorClientRecords(string name = null)
        {
            List<ContractorClient> contractorClientRecords = new List<ContractorClient>();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");

            string query = string.IsNullOrEmpty(name)
                ? "SELECT * FROM VwClients ORDER BY ID"
                : "SELECT * FROM VwClients WHERE clientname = @Name ORDER BY ID";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(name))
                {
                    command.Parameters.AddWithValue("@Name", name);
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
		 [HttpGet("api/[controller]/GetBidAccessEmployees", Name = "GetBidAccessEmployees")]
        public async Task<IActionResult> GetBidAccessEmployees()
        {
            try
            {
                List<Employee> employees = new List<Employee>();

                string connectionString =
                    _configuration.GetConnectionString("SageSBQConnection");

                string query = @"
            SELECT ID, DisplayName
            FROM StowellUsers";

                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;

                        await connection.OpenAsync();

                        using (SqlDataReader reader =
                               await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Employee record = new Employee
                                {
                                    Id = !reader.IsDBNull(reader.GetOrdinal("ID"))
         ? Convert.ToInt32(reader["ID"])
         : 0,

                                    DisplayName =
         !reader.IsDBNull(reader.GetOrdinal("DisplayName"))
         ? reader["DisplayName"].ToString()
         : string.Empty
                                };

                                employees.Add(record);
                            }
                        }
                    }
                }

                return Ok(employees);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, sqlEx.Message);

                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving employees.",
                    Details = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving employees.",
                    Details = ex.Message
                });
            }
        }
        [HttpGet("api/[controller]/GetAllBidPhases", Name = "GetAllBidPhases")]
        public async Task<IActionResult> GetAllBidPhases()
        {
            try
            {
                List<Phase> costCodeRecords = GetAllPhases();

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
        private List<Phase> GetAllPhases()
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
        [HttpGet("api/[controller]/GetAllBidStatus", Name = "GetAllBidStatus")]
        public async Task<IActionResult> GetAllBidStatus()
        {
            try
            {
                List<BidStatus> bidStatuses = GetAllStatus();

                

                return Ok(new { data = bidStatuses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllStatuses Error");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.Message }
                );
            }
        }
        private List<BidStatus> GetAllStatus()
        {
            List<BidStatus> costCodeRecords = new List<BidStatus>();

            string connectionString = _configuration.GetConnectionString("SageSBQConnection");
            string query = "select * from BidStatus order by 'order' asc;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    BidStatus record = new BidStatus
                    {
                        Uid = reader.IsDBNull(reader.GetOrdinal("Uid")) ? string.Empty : reader["Uid"].ToString(),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader["Name"].ToString()
                    };

                    costCodeRecords.Add(record);
                }
            }

            return costCodeRecords;
        }

        private async Task CreateFolder(string sourceTemplatePath, string destinationRoot, string folderName)
        {
            try
            {
                // FIX 1: Correct path combine
                var destPath = Path.Combine(destinationRoot, folderName);

                // Validate source
                if (!Directory.Exists(sourceTemplatePath))
                {
                    throw new DirectoryNotFoundException($"Source not found: {sourceTemplatePath}");
                }

                // Create destination folder
                Directory.CreateDirectory(destPath);

                // Copy structure + files
                CopyAll(new DirectoryInfo(sourceTemplatePath), new DirectoryInfo(destPath));
            }
            catch (Exception ex)
            {
                // IMPORTANT: log or return error
                Console.WriteLine($"Error in CreateFolder: {ex.Message}");
                throw;
            }
        }
        private void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            try
            {
                // Create all directories
                foreach (DirectoryInfo dir in source.GetDirectories())
                {
                    DirectoryInfo newDir = target.CreateSubdirectory(dir.Name);
                    CopyAll(dir, newDir);
                }
            }
            catch (Exception ex)
            {
                // IMPORTANT: log or return error
                Console.WriteLine($"Error in CopyAll: {ex.Message}");
                throw;
            }
        }
    }
    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
