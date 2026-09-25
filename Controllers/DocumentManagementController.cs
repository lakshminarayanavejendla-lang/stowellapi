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
    [Route("api/DocumentManagement")]
    [ApiController]
    public class DocumentManagementController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public PhysicalFileProvider operation;
        public string basePath = string.Empty;

        public DocumentManagementController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            this.basePath = _configuration["FileSettings:RootSharePath"];

            this.operation = new PhysicalFileProvider();
   
        }
        
        [HttpPost("sharepointfileoperations")]
        public async Task<IActionResult> sharepointfileoperations([FromBody] object args)
        {
            var clientId = _configuration["SharePoint:ClientId"];
            var tenantId = _configuration["SharePoint:TenantId"];
            var clientSecret = _configuration["SharePoint:ClientSecret"];
            var siteHostName = _configuration["SharePoint:SiteHostName"];
            var sitePath = _configuration["SharePoint:SitePath"];
            var parentFolderPath = _configuration["SharePoint:ParentFolderPath"];

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var graphClient = new GraphServiceClient(credential);

            var site = await graphClient
                .Sites
                .GetByPath(sitePath, siteHostName)
                .Request()
                .GetAsync();

            // IMPORTANT: get SharePoint items
            var children = await graphClient
                .Sites[site.Id]
                .Drive
                .Root
                .ItemWithPath(parentFolderPath)
                .Children
                .Request()
                .GetAsync();

            var files = new List<FileDirectoryContent>();

            foreach (var item in children)
            {
                files.Add(new FileDirectoryContent
                {
                    Name = item.Name,
                    Id = item.Id,
                    IsFile = item.File != null,
                    HasChild = item.Folder != null,
                    Size = item.Size ?? 0,
                    Type = item.File != null ? "File" : "Folder",
                    FilterPath = ""
                });
            }

            // 🔥 REQUIRED WRAPPER
            var response = new FileManagerResponse<FileDirectoryContent>
            {
                Files = files,
                CWD = new FileDirectoryContent
                {
                    Name = "Main Jobs Folder",
                    IsFile = false,
                    HasChild = true,
                    FilterPath = parentFolderPath
                }
            };

            return Ok(response);
        }
        // HELPER METHOD: This replaces the "constructor" logic for dynamic paths
        private void SetDynamicRoot(string folder)
        {
            string fullPath = Path.Combine(this.basePath, folder ?? "");
            //if (!System.IO.Directory.Exists(fullPath)) System.IO.Directory.CreateDirectory(fullPath);

            this.operation.RootFolder(fullPath);
            //  this.operation.RootFolder(this.basePath);
            var field = this.operation.GetType()
    .GetField("hostPath", BindingFlags.Instance | BindingFlags.NonPublic);

            var value = (string)field.GetValue(this.operation);

            if (!string.IsNullOrEmpty(value) &&
                value.StartsWith("\\") &&
                !value.StartsWith("\\\\"))
            {
                field.SetValue(this.operation, "\\" + value);
            }
        }
        [HttpPost("fileoperations")]
        public IActionResult FileOperations([FromBody] FileManagerDirectoryContent args,[FromQuery] string folder)
        {
            try
            {
                SetDynamicRoot(folder);

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
                Console.WriteLine("RootPath: " + _configuration["FileSettings:RootSharePath"]);
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
            }catch(Exception ex)
            {
            }
            return null;
        }
        [HttpPost("FileUpload")]
        [DisableRequestSizeLimit]
        public IActionResult Upload([FromForm] string path, [FromForm] long size, [FromForm] IList<IFormFile> uploadFiles, [FromForm] string action, [FromQuery] string folder)
        {
            try
            {
                SetDynamicRoot(folder);
                FileManagerResponse uploadResponse;
                foreach (var file in uploadFiles)
                {
                    var folders = (file.FileName).Split('/');
                    // checking the folder upload
                    if (folders.Length > 1)
                    {
                        for (var i = 0; i < folders.Length - 1; i++)
                        {
                            string newDirectoryPath = Path.Combine(this.basePath+folder + path, folders[i]);
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
        public IActionResult Download([FromForm] string downloadInput, [FromQuery] string folder)
        {
            SetDynamicRoot(folder);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            FileManagerDirectoryContent args = JsonSerializer.Deserialize<FileManagerDirectoryContent>(downloadInput, options);
            return operation.Download(args.Path, args.Names, args.Data);
        }
        // gets the image(s) from the given path
        [HttpGet("FileGetImage")]
        public IActionResult GetImage(FileManagerDirectoryContent args, [FromQuery] string folder)
        {
            SetDynamicRoot(folder);
            
            return this.operation.GetImage(args.Path, args.Id, false, null, null);
        }
       
        // Optional helper for better browser/viewer compatibility
        // FIX (2026-09-17) - was missing xls/xlsx/csv and image types entirely (fell through to
        // generic application/octet-stream), so a browser opening one of these via GetFile in a
        // new tab couldn't recognize what it actually was as reliably as it should.
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
        public IActionResult GetFile([FromQuery] string filterPath, [FromQuery]  string folder, [FromQuery] string name)
        {
            string networkRoot = _configuration["FileSettings:RootSharePath"];

            string fullPath = Path.Combine(networkRoot, folder, filterPath?.TrimStart('\\') ?? "", name);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            string contentType = GetMimeType(name);
            // ✅ show filename for download + allow inline viewing
            Response.Headers["Content-Disposition"] =
                $"inline; filename=\"{name}\"";
            // IMPORTANT: DO NOT force download or inline headers manually

            // FIX (2026-09-23, Mike Smith - "It takes a while for Excel and Word documents to
            // open") - this used to read the whole file into a byte[] with ReadAllBytes before
            // sending a single byte of the response, so the client waited for the full file (word/
            // excel docs run bigger than a PDF and this endpoint has no size cap) before anything
            // started downloading. PhysicalFile streams straight off disk and, with
            // enableRangeProcessing: true, answers HTTP Range requests - which is what actually
            // matters for Office documents specifically: when Windows opens a .docx/.xlsx URL in
            // desktop Word/Excel (not the browser), Office fetches it in ranges rather than one
            // sequential GET, so a non-range-capable endpoint forces Office to wait on one huge
            // request instead of pipelining several small ones. PDFs were already fast because the
            // browser renders them natively inline without ever going through Office.
            // NOT fixed by this change (a separate, larger problem - see the file's/Angular's
            // header comments): edits made in the resulting Word/Excel session still have nowhere
            // to save back to, since this is a one-way GET-only endpoint over a local/network file
            // share, not a real WOPI/SharePoint-backed editable document.
            return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
        }
        [HttpGet("PreviewFile")]
        public IActionResult PreviewFile([FromQuery] string filterPath,[FromQuery] string folder,[FromQuery] string name)
        {
            string networkRoot = _configuration["FileSettings:RootSharePath"];

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
        // Serves PDF, DOCX, and XLSX files for in-browser viewing
        [HttpGet("GetFileTypes")]
        public IActionResult GetFileTypes([FromQuery] string filterPath, [FromQuery] string folder, [FromQuery] string name)
        {
            string networkRoot = _configuration["FileSettings:RootSharePath"];

            string fullPath = Path.Combine(networkRoot, folder, filterPath?.TrimStart('\\') ?? "", name);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            //if (string.IsNullOrWhiteSpace(path))
            //    return BadRequest("File path is required.");

            //// Sanitize and resolve full path
            //var fullPath = Path.GetFullPath(Path.Combine(_rootPath, path.TrimStart('/', '\\')));

            // Prevent path traversal outside root
            //if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            //    return BadRequest("Invalid file path.");

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found.");

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            string contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            var fileName = Path.GetFileName(fullPath);

            return File(stream, contentType, fileName);
        }
    }
}