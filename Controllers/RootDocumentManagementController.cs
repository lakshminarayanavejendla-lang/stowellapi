using Azure.Identity;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Graph;
using Newtonsoft.Json;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using StowellCoAPI.DTO;
using Syncfusion.EJ2.FileManager.Base;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using System.Data;
using System.Reflection;
using System.Runtime;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace StowellCoAPI.Controllers
{
    [Route("api/RootDocumentManagement")]
    [ApiController]
    public class RootDocumentManagementController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public PhysicalFileProvider operation;
        public string basePath = string.Empty;

        public RootDocumentManagementController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            this.basePath = _configuration["FileSettings:RootPath"];

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
                Console.WriteLine("RootPath: " + _configuration["FileSettings:RootPath"]);
                Console.WriteLine("Args.Path: " + args.Path);
                switch (args.Action)
                {
                    case "read":
                        {
                            // Check if user is trying to enter a restricted folder
                            if (!string.IsNullOrEmpty(args.Path) && args.Path != "/")
                            {
                                string folderName = args.Path.Trim('/');

                                var restrictedFolders = GetRestrictedFolders();

                                if (restrictedFolders.Any(x =>
                                    x.Equals(folderName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    FileManagerResponse deniedResponse = new FileManagerResponse();

                                    deniedResponse.Error =
                                        new Syncfusion.EJ2.FileManager.Base.ErrorDetails
                                        {
                                            Code = "401",
                                            Message = "Access denied. You do not have permission to access this folder."
                                        };

                                    return Ok(this.operation.ToCamelCase(deniedResponse));
                                }
                            }

                            FileManagerResponse response =
                                this.operation.GetFiles(args.Path, args.ShowHiddenItems);

                            if (response.CWD != null)
                            {
                                response.CWD.Name =
                                    string.IsNullOrEmpty(response.CWD.Name)
                                        ? folder
                                        : response.CWD.Name;
                            }

                            // Add lock icon to restricted folders
                            var restricted = GetRestrictedFolders();

                            if (response.Files != null)
                            {
                              response.Files = response.Files
        .Select(item =>
        {
            if (!item.IsFile &&
                restricted.Contains(item.Name))
            {
                item.Permission = new AccessPermission
                {
                    Read = true,
                    Write = false,
                    Download = false,
                    Copy = false,
                    Upload = false,
                    WriteContents = false
                };
            }

            return item;
        })
        .ToList(); 
                            }

                            return Ok(this.operation.ToCamelCase(response));
                        }
                    //FileManagerResponse response = this.operation.GetFiles(args.Path, args.ShowHiddenItems);
                    //// 1. Fix the Root (CWD)
                    //if (response.CWD != null)
                    //{
                    //    response.CWD.Name = string.IsNullOrEmpty(response.CWD.Name) ? folder : response.CWD.Name; // Use the folder name from query string
                    //}
                    //                                //    response.CWD.Path = args.Path ?? "";
                    //                                //   // response.CWD.FilterPath = "";
                    //                                //    response.CWD.FilterPath.Replace("\\", "/");
                    //                                //}

                    //    var readfiles = this.operation.ToCamelCase(response);
                    //// reads the file(s) or folder(s) from the given path.
                    ////  var obj = this.operation.ToCamelCase(this.operation.GetFiles(args.Path, args.ShowHiddenItems));
                    //return Ok(readfiles);
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
        //[HttpGet("GetFile")]
        //public IActionResult GetFile(string name, string folder)
        //{
        //    // 1. Your network share root
        //    string networkRoot = @"\\DESKTOP-C86LJD9\AnuShareFolder";

        //    // 2. Clean the filterPath 
        //    // FilterPath usually looks like "\Folder1\SubFolder\"
        //    string relativePath = filterPath.TrimStart('\\');

        //    // 3. Combine them all
        //    string physicalPath = Path.Combine(networkRoot, relativePath, name);

        //    if (!System.IO.File.Exists(physicalPath))
        //    {
        //        return NotFound($"File not found at: {physicalPath}");
        //    }

        //    var fileBytes = System.IO.File.ReadAllBytes(physicalPath);
        //    return File(fileBytes, "application/octet-stream", name);
        //}
        //[HttpGet("GetFile")]
        //public IActionResult GetFile([FromQuery] string filterPath, [FromQuery] string folder, [FromQuery] string name)
        //{
        //    string networkRoot = _configuration["FileSettings:RootPath"];

        //    // 1. Clean the paths to prevent double slashes or accidental root-overrides
        //    string cleanFolderName = folder.Trim('\\');
        //    string cleanFilterPath = (filterPath ?? "").Trim('\\');

        //    // 2. Combine all parts
        //    // Path.Combine will handle the slashes between segments automatically
        //    string fullPath = Path.Combine(networkRoot, cleanFolderName, cleanFilterPath, name);

        //    if (System.IO.File.Exists(fullPath))
        //    {
        //        byte[] b = System.IO.File.ReadAllBytes(fullPath);

        //        // It is better to provide the MIME type for better browser handling
        //        string contentType = GetMimeType(name);
        //        return File(b, contentType, name);
        //    }

        //    return NotFound($"File not found at: {fullPath}");
        //}

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
        //[HttpGet("GetFile")]
        //public IActionResult GetFile([FromQuery] string filterPath, [FromQuery] string folder, [FromQuery] string name)
        //{
        //    string networkRoot = _configuration["FileSettings:RootPath"];
        //    string fullPath = Path.Combine(networkRoot, folder, filterPath.TrimStart('\\'), name);

        //    if (System.IO.File.Exists(fullPath))
        //    {
        //        byte[] b = System.IO.File.ReadAllBytes(fullPath);

        //        // IMPORTANT: Use "inline" instead of "attachment"
        //        // This tells the browser: "Try to show this in the window"
        //        var cd = new System.Net.Mime.ContentDisposition
        //        {
        //            FileName = name,
        //            Inline = true,
        //        };
        //        Response.Headers.Add("Content-Disposition", cd.ToString());

        //        // Map the correct content type so the browser knows it's a PDF/Doc
        //        string contentType = GetMimeType(name);
        //        return File(b, contentType);
        //    }

        //    return NotFound();
        //}
        [HttpGet("GetFile")]
        public IActionResult GetFile([FromQuery] string filterPath, [FromQuery]  string folder, [FromQuery] string name)
        {
            string networkRoot = _configuration["FileSettings:RootPath"];

            string fullPath = Path.Combine(networkRoot, folder, filterPath?.TrimStart('\\') ?? "", name);

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
            string networkRoot = _configuration["FileSettings:RootPath"];

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
        private List<string> GetRestrictedFolders()
        {
            return new List<string>();
                //{
                //    "10064 - Fairmont Hotel",
                //    "10109 - Symphony Square Apartments"
                //};
        }
        //private List<string> GetRestrictedFolders()
        //{
        //    using SqlConnection con =
        //        new SqlConnection(_configuration.GetConnectionString("Default"));

        //    using SqlCommand cmd =
        //        new SqlCommand("sp_GetRestrictedFolders", con);

        //    cmd.CommandType = CommandType.StoredProcedure;

        //    con.Open();

        //    List<string> folders = new();

        //    using SqlDataReader reader = cmd.ExecuteReader();

        //    while (reader.Read())
        //    {
        //        folders.Add(reader["FolderName"].ToString());
        //    }

        //    return folders;
        //}
    }
}