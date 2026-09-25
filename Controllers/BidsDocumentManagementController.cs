using Azure.Identity;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Graph;
using Newtonsoft.Json;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using StowellCoAPI.DTO;
using Syncfusion.EJ2.FileManager.Base;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using System.Reflection;
using System.Runtime;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace StowellCoAPI.Controllers
{
    [Route("api/BidsDocumentManagement")]
    [ApiController]
    public class BidsDocumentManagementController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public PhysicalFileProvider operation;
        public string basePath = string.Empty;

        public BidsDocumentManagementController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            this.basePath = _configuration["BidsDocSettings:RootSharePath"];

            this.operation = new PhysicalFileProvider();
   
        }
        
        // FIX (2026-09-15) - this used to do `this.operation.RootFolder(rootPath)` directly, i.e.
        // set Syncfusion's PhysicalFileProvider root to the bare bid ID string (e.g. "B2600003")
        // the Angular side sends as `rootPath` - not a real filesystem path, so every file
        // operation threw. That exception was then swallowed by FileOperations' empty
        // `catch(Exception ex) { }` and masked by a `return null`, which itself crashed with an
        // unrelated "Cannot return null from an action method" 500 - the actual cause of the
        // Documents tab's "Network Error" reports. Each bid already has its real, fully-resolved
        // folder path stored on BidRecords.DocumentFolderPath (e.g.
        // "STO-FS01\Production\Bids\Drywall\Masonry\B2600002" - confirmed live), so this now
        // looks that up by DisplayBidID (what `rootPath` actually is) instead of guessing at a
        // Department/Division layout client-side.
        private void SetDynamicRoot(string rootPath)
        {
            string fullPath = ResolveBidDocumentFolder(rootPath) ?? rootPath;

            this.operation.RootFolder(fullPath);
            var field = this.operation.GetType().GetField("hostPath", BindingFlags.Instance | BindingFlags.NonPublic);

            var value = (string)field.GetValue(this.operation);

            if (!string.IsNullOrEmpty(value) &&
                value.StartsWith("\\") &&
                !value.StartsWith("\\\\"))
            {
                field.SetValue(this.operation, "\\" + value);
            }
        }

        // Looks up the real UNC folder path for a bid from BidRecords.DocumentFolderPath (stored
        // without the leading "\\" - see GetBidById's live output) given its DisplayBidID. Returns
        // null (falls back to the caller's raw rootPath) if the bid isn't found or has no folder
        // path recorded yet, rather than throwing - a missing/blank path is a legitimate state for
        // a bid that predates document library provisioning.
        private string ResolveBidDocumentFolder(string displayBidId)
        {
            if (string.IsNullOrWhiteSpace(displayBidId)) return null;

            try
            {
                string connectionString = _configuration.GetConnectionString("SageSBQConnection");
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                using var command = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT DocumentFolderPath FROM BidRecords WHERE DisplayBidID = @DisplayBidID", connection);
                command.Parameters.AddWithValue("@DisplayBidID", displayBidId);

                connection.Open();
                var result = command.ExecuteScalar() as string;

                if (string.IsNullOrWhiteSpace(result)) return null;

                return result.StartsWith("\\\\") ? result : "\\\\" + result;
            }
            catch
            {
                return null;
            }
        }
        [HttpPost("fileoperations")]
        public IActionResult FileOperations([FromBody] FileManagerDirectoryContent args, [FromQuery] string rootPath)
        {
            try
            {
                SetDynamicRoot(rootPath);

                // return Ok();
                if (args.Action == "delete" || args.Action == "rename")
                {
                    if ((args.TargetPath == null) && (args.Path == ""))
                    {
                        FileManagerResponse response = new FileManagerResponse();
                        response.Error = new Syncfusion.EJ2.FileManager.Base.ErrorDetails { Code = "401", Message = "Restricted to modify the root folder." };
                        return Ok(this.operation.ToCamelCase(response));
                    }
                }
                Console.WriteLine("RootPath: " + _configuration["BidsDocSettings:RootSharePath"]);
                Console.WriteLine("Args.Path: " + args.Path);
                switch (args.Action)
                {
                    case "read":
                        FileManagerResponse response = this.operation.GetFiles(args.Path, args.ShowHiddenItems);
                        // 1. Fix the Root (CWD)
                        //if (response.CWD != null)
                        //{
                        //    response.CWD.Name = folder; // Use the folder name from query string
                        //    response.CWD.Path = args.Path ?? "";
                        //   // response.CWD.FilterPath = "";
                        //    response.CWD.FilterPath.Replace("\\", "/");
                        //}

                        var readfiles = this.operation.ToCamelCase(response);
                        // reads the file(s) or folder(s) from the given path.
                        //  var obj = this.operation.ToCamelCase(this.operation.GetFiles(args.Path, args.ShowHiddenItems));
                        return Ok(readfiles);
                    case "delete":
                        FileManagerResponse deleteresponse = this.operation.Delete(args.Path, args.Names);
                        // 2. Fix the separators in the CWD (Current Working Directory)
                        //if (deleteresponse.CWD != null)
                        //{
                        //    deleteresponse.CWD.Name = folder; // Use the folder name from query string
                        //    deleteresponse.CWD.Path = args.Path ?? "";
                        //    deleteresponse.CWD.FilterPath = "";
                        //    deleteresponse.CWD.FilterPath.Replace("\\", "/");
                        //}
                        // deletes the selected file(s) or folder(s) from the given path.
                        return Ok(this.operation.ToCamelCase(deleteresponse));
                    case "copy":
                        var copyresponse = this.operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData);
                        //if (copyresponse.CWD != null)
                        //{
                        //    copyresponse.CWD.Name = folder;
                        //    copyresponse.CWD.Path = args.Path ?? "";
                        //    copyresponse.CWD.FilterPath = "";
                        //    copyresponse.CWD.FilterPath.Replace("\\", "/");
                        //}

                        // copies the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                        return Ok(this.operation.ToCamelCase(copyresponse));
                    case "move":
                        var moveresponse = this.operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData);
                        //if (moveresponse.CWD != null)
                        //{
                        //    moveresponse.CWD.Name = folder;
                        //    moveresponse.CWD.Path = args.Path ?? "";
                        //    moveresponse.CWD.FilterPath = "";
                        //    moveresponse.CWD.FilterPath.Replace("\\", "/");
                        //}
                        // cuts the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                        return Ok(this.operation.ToCamelCase(moveresponse));
                    case "details":
                        var detailsresponse = this.operation.Details(args.Path, args.Names, args.Data);
                        //if (detailsresponse.CWD != null)
                        //{
                        //    detailsresponse.CWD.Name = folder;
                        //    detailsresponse.CWD.Path = args.Path ?? "";
                        //    detailsresponse.CWD.FilterPath = "";
                        //    detailsresponse.CWD.FilterPath.Replace("\\", "/");
                        //}
                        // gets the details of the selected file(s) or folder(s).
                        return Ok(this.operation.ToCamelCase(detailsresponse));
                    case "create":
                        var createresponse = this.operation.Create(args.Path, args.Name);
                        //if (createresponse.CWD != null)
                        //{
                        //    createresponse.CWD.Name = folder;
                        //    createresponse.CWD.Path = args.Path ?? "";
                        //    //createresponse.CWD.FilterPath = "";
                        //    createresponse.CWD.FilterPath.Replace("\\", "/");
                        //}
                        // creates a new folder in a given path.
                        return Ok(this.operation.ToCamelCase(createresponse));
                    case "search":
                        var searchresponse = this.operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive);
                        //if (searchresponse.CWD != null)
                        //{
                        //    searchresponse.CWD.Name = folder;
                        //    searchresponse.CWD.Path = args.Path ?? "";
                        //    searchresponse.CWD.FilterPath = "";
                        //    searchresponse.CWD.FilterPath.Replace("\\", "/");
                        //}
                        // gets the list of file(s) or folder(s) from a given path based on the searched key string.
                        return Ok(this.operation.ToCamelCase(searchresponse));
                    case "rename":
                        var renameresponse = this.operation.Rename(args.Path, args.Name, args.NewName, false, args.ShowFileExtension, args.Data);
                        //if (renameresponse.CWD != null)
                        //{
                        //    renameresponse.CWD.Name = folder;
                        //    renameresponse.CWD.Path = args.Path ?? "";
                        //    renameresponse.CWD.FilterPath = "";
                        //    renameresponse.CWD.FilterPath.Replace("\\", "/");
                        //}
                        // renames a file or folder.
                        return Ok(this.operation.ToCamelCase(renameresponse));
                }
            }
            catch (Exception ex)
            {
                // FIX (2026-09-15) - this was an empty catch (silently swallowed the real error,
                // e.g. "root folder not found" from the SetDynamicRoot bug above) falling through
                // to `return null`, which crashes ASP.NET Core with an unrelated
                // "Cannot return null from an action method" 500 - the actual root of the
                // Documents tab's "Network Error" reports. Returns a proper Syncfusion
                // FileManagerResponse error envelope instead, same shape the "read"/"delete"/etc.
                // cases above already return on success.
                var errorResponse = new FileManagerResponse
                {
                    Error = new Syncfusion.EJ2.FileManager.Base.ErrorDetails { Code = "500", Message = ex.Message }
                };
                return Ok(this.operation.ToCamelCase(errorResponse));
            }
            return Ok(this.operation.ToCamelCase(new FileManagerResponse
            {
                Error = new Syncfusion.EJ2.FileManager.Base.ErrorDetails { Code = "400", Message = $"Unsupported action '{args.Action}'." }
            }));
        }
        [HttpPost("FileUpload")]
        [DisableRequestSizeLimit]
        public IActionResult Upload([FromForm] string path, [FromForm] long size, [FromForm] IList<IFormFile> uploadFiles, [FromForm] string action, [FromQuery] string rootPath)
        {
            try
            {
                SetDynamicRoot(rootPath);
                FileManagerResponse uploadResponse;
                foreach (var file in uploadFiles)
                {
                    var folders = (file.FileName).Split('/');
                    // checking the folder upload
                    if (folders.Length > 1)
                    {
                        for (var i = 0; i < folders.Length - 1; i++)
                        {
                            string newDirectoryPath = Path.Combine(rootPath + path, folders[i]);
                            if (Path.GetFullPath(newDirectoryPath) != (Path.GetDirectoryName(newDirectoryPath) + Path.DirectorySeparatorChar + folders[i]))
                            {
                                throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                            }
                            if (!System.IO.Directory.Exists(newDirectoryPath))
                            {
                                this.operation.ToCamelCase(this.operation.Create(path, folders[i]));
                            }
                            path += folders[i] + "/";
                        }
                    }
                }
                uploadResponse = operation.Upload(path, uploadFiles, action, size, null);
                if (uploadResponse.Error != null)
                {
                    Response.Clear();
                    Response.ContentType = "application/json; charset=utf-8";
                    Response.StatusCode = Convert.ToInt32(uploadResponse.Error.Code);
                    Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = uploadResponse.Error.Message;
                }
            }
            catch (Exception e)
            {
                Syncfusion.EJ2.FileManager.Base.ErrorDetails er = new Syncfusion.EJ2.FileManager.Base.ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = "417";
                er.Message = "Access denied for Directory-traversal";
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = Convert.ToInt32(er.Code);
                Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = er.Message;
                return Content("");
            }
            return Content("");
        }

        // downloads the selected file(s) and folder(s)
        [HttpPost("FileDownload")]
        public IActionResult Download([FromForm] string downloadInput, [FromQuery] string rootPath)
        {
            SetDynamicRoot(rootPath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            FileManagerDirectoryContent args = JsonSerializer.Deserialize<FileManagerDirectoryContent>(downloadInput, options);
            return operation.Download(args.Path, args.Names, args.Data);
        }
        // gets the image(s) from the given path
        [HttpGet("FileGetImage")]
        public IActionResult GetImage(FileManagerDirectoryContent args, [FromQuery] string rootPath)
        {
            SetDynamicRoot(rootPath);
            
            return this.operation.GetImage(args.Path, args.Id, false, null, null);
        }
       
        // Optional helper for better browser/viewer compatibility
        // FIX (2026-09-17) - was missing xls/xlsx/csv and image types entirely, see
        // DocumentManagementController.GetMimeType's matching fix comment.
        private string GetMimeType(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".csv" => "text/csv",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
        
        [HttpGet("GetFile")]
        public IActionResult GetFile([FromQuery] string filterPath, [FromQuery]  string rootPath, [FromQuery] string name)
        {
            // FIX (2026-09-16, Mike Smith - "was not able to [open files] from Bid Doc Library",
            // Project Doc Library worked fine) - unlike FileOperations/Upload/Download/GetImage
            // (all routed through SetDynamicRoot -> ResolveBidDocumentFolder, which looks up the
            // bid's real UNC folder from BidRecords.DocumentFolderPath), this action combined the
            // raw `rootPath` query value - just the DisplayBidID string, e.g. "B2600059" - directly
            // as if it were a filesystem path. Path.Combine("B2600059", ...) resolves relative to
            // the API process's working directory, never where the file actually lives, so
            // File.Exists() was always false and every open 404'd. Now resolves the same real
            // folder path the rest of this controller already uses.
            string fullPath = Path.Combine(ResolveBidDocumentFolder(rootPath) ?? rootPath, filterPath?.TrimStart('\\') ?? "", name);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);

            string contentType = GetMimeType(name);
            // ✅ show filename for download + allow inline viewing
            Response.Headers["Content-Disposition"] =
                $"inline; filename=\"{name}\"";
            // IMPORTANT: DO NOT force download or inline headers manually
            return File(fileBytes, contentType);
        }
        [HttpGet("PreviewFile")]
        public IActionResult PreviewFile([FromQuery] string filterPath,[FromQuery] string folder,[FromQuery] string name)
        {
            string networkRoot = _configuration["BidsDocSettings:RootSharePath"];

            string fullPath = Path.Combine(
                networkRoot,
                folder,
                filterPath?.TrimStart('\\') ?? "",
                name
            );

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            string ext = Path.GetExtension(name).ToLowerInvariant();

            var memoryStream = new MemoryStream();

            try
            {
                if (ext == ".pdf")
                {
                    byte[] pdfBytes = System.IO.File.ReadAllBytes(fullPath);
                    return File(pdfBytes, "application/pdf", name);
                }

                // =========================
                // DOCX → PDF
                // =========================
                if (ext == ".docx")
                {
                    using var wordDoc = new Syncfusion.DocIO.DLS.WordDocument(fullPath);
                    using var renderer = new Syncfusion.DocIORenderer.DocIORenderer();

                    var pdfDoc = renderer.ConvertToPDF(wordDoc);
                    pdfDoc.Save(memoryStream);
                    pdfDoc.Close(true);
                }

                // =========================
                // XLSX → PDF
                // =========================
                else if (ext == ".xlsx")
                {
                    using var excelEngine = new Syncfusion.XlsIO.ExcelEngine();
                    var application = excelEngine.Excel;
                    var workbook = application.Workbooks.Open(fullPath);

                    var renderer = new Syncfusion.XlsIORenderer.XlsIORenderer();
                    var pdfDoc = renderer.ConvertToPDF(workbook);

                    pdfDoc.Save(memoryStream);
                    pdfDoc.Close(true);
                }
                else
                {
                    return BadRequest("Unsupported file type");
                }

                memoryStream.Position = 0;

                return File(memoryStream, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}