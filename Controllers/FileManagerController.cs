using Azure.Identity;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Graph;
using Newtonsoft.Json;
using StowellCoAPI.DTO;
using Syncfusion.EJ2.FileManager.Base;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using System.Runtime;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace StowellCoAPI.Controllers
{
    [Route("api/filemanager")]
    [ApiController]
    public class FileManagerController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public PhysicalFileProvider operation;
        public string basePath;
        string root = Path.Combine("wwwroot", "Files");
        //public TestFileManagerController(IWebHostEnvironment hostingEnvironment)
        //{
        //    this.basePath = hostingEnvironment.ContentRootPath;
        //    this.operation = new PhysicalFileProvider();
        //    string physicalPath = Path.Combine(this.basePath, this.root);
        //    this.operation.RootFolder(physicalPath);
        //}
        public FileManagerController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            this.basePath = hostingEnvironment.ContentRootPath;
            this.operation = new PhysicalFileProvider();
            string physicalPath = Path.Combine(this.basePath, root);
            this.operation.RootFolder(physicalPath);
        }

        //[HttpPost("fileoperations")]
        //public async Task<IActionResult> FileOperations([FromBody] object args)
        //{
        //    var clientId = _configuration["SharePoint:ClientId"];
        //    var tenantId = _configuration["SharePoint:TenantId"];
        //    var clientSecret = _configuration["SharePoint:ClientSecret"];
        //    var siteHostName = _configuration["SharePoint:SiteHostName"];
        //    var sitePath = _configuration["SharePoint:SitePath"];
        //    var rootPath = _configuration["SharePoint:ParentFolderPath"];

        //    var credential = new ClientSecretCredential(
        //        tenantId,
        //        clientId,
        //        clientSecret
        //    );

        //    var graphClient = new GraphServiceClient(credential);

        //    var site = await graphClient
        //        .Sites
        //        .GetByPath(sitePath, siteHostName)
        //        .Request()
        //        .GetAsync();

        //    // IMPORTANT: DO NOT RECURSE
        //    var items = await graphClient
        //        .Sites[site.Id]
        //        .Drive
        //        .Root
        //        .ItemWithPath(rootPath)
        //        .Children
        //        .Request()
        //        .GetAsync();

        //    var result = items.Select(x => new
        //    {
        //        Name = x.Name,
        //        Id = x.Id,
        //        Size = x.Size ?? 0,
        //        IsFile = x.File != null,
        //        HasChild = x.Folder != null,
        //        Type = x.File != null ? "File" : "Folder"
        //    });

        //    return Ok(result);
        //}
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
        //private readonly string root = @"\\DESKTOP-C86LJD9\AnuShareFolder";

        [HttpPost("fileoperations")]
        //public IActionResult FileOperations([FromBody] object args)
        ////[FromBody] FileManagerDirectoryContent args)
        //{
        //    try
        //    {
        //        //switch (args.Action)
        //        //{
        //        //    case "read":
        //        //        return Read(args.Path);

        //        //    case "details":
        //        //        return Details(args);

        //        //    default:
        //        //        return Ok();
        //        //}
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}
        public IActionResult FileOperations([FromBody] FileManagerDirectoryContent args)
        {
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
            switch (args.Action)
            {
                case "read":
                    // reads the file(s) or folder(s) from the given path.
                    var readfiles = this.operation.ToCamelCase(this.operation.GetFiles(args.Path, args.ShowHiddenItems));
                    return Ok(readfiles);
                case "delete":
                    // deletes the selected file(s) or folder(s) from the given path.
                    return Ok(this.operation.ToCamelCase(this.operation.Delete(args.Path, args.Names)));
                case "copy":
                    // copies the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                    return Ok(this.operation.ToCamelCase(this.operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData)));
                case "move":
                    // cuts the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                    return Ok(this.operation.ToCamelCase(this.operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData)));
                case "details":
                    // gets the details of the selected file(s) or folder(s).
                    return Ok(this.operation.ToCamelCase(this.operation.Details(args.Path, args.Names, args.Data)));
                case "create":
                    // creates a new folder in a given path.
                    return Ok(this.operation.ToCamelCase(this.operation.Create(args.Path, args.Name)));
                case "search":
                    // gets the list of file(s) or folder(s) from a given path based on the searched key string.
                    return Ok(this.operation.ToCamelCase(this.operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive)));
                case "rename":
                    // renames a file or folder.
                    return Ok(this.operation.ToCamelCase(this.operation.Rename(args.Path, args.Name, args.NewName, false, args.ShowFileExtension, args.Data)));
            }
            return null;
        }
        //var action = args.GetProperty("action").GetString();

        //switch (action)
        //{
        //    case "read":
        //        var path = args.GetProperty("path").GetString();
        //        var relativePath = path?.TrimStart('/') ?? "";
        //        var fullPath = Path.Combine(root, relativePath.Replace("/", "\\"));
        //        return Read(path);

        //    case "details":
        //       // return Details(args);

        //    default:
        //        return Ok();
        //}
        //  var action = args.GetProperty("action").GetString();

        //switch (action)
        //{
        //    case "read":
        //        {
        //var path = args.GetProperty("path").GetString();

        //var relativePath = string.IsNullOrWhiteSpace(path) || path == "/"
        //    ? ""
        //    : path.TrimStart('/');

        //var fullPath = Path.Combine(root, relativePath.Replace("/", "\\"));

        //return Read(fullPath, relativePath);
        //    var path = args.GetProperty("path").GetString();

        //    // Convert Syncfusion virtual path → physical path
        //    var relativePath = string.IsNullOrWhiteSpace(path) || path == "/"
        //        ? ""
        //        : path.Trim('/').Replace("/", "\\");

        //    var fullPath = Path.Combine(root, relativePath);

        //    return Read(fullPath, path ?? "/");
        //}

        //  default:
        //  return Ok();
        // }
        // }
        private IActionResult Read(string fullPath, string virtualPath)
        {
            //var files = new List<FileManagerDirectoryContent>();

            //if (!System.IO.Directory.Exists(fullPath))
            //    fullPath = root;

            //foreach (var dir in System.IO.Directory.GetDirectories(fullPath))
            //{
            //    var dirInfo = new DirectoryInfo(dir);

            //    files.Add(new FileManagerDirectoryContent
            //    {
            //        Name = dirInfo.Name ?? "",
            //        IsFile = false,
            //        Type = "Directory",
            //        Size = 0,
            //        Path = dirInfo.FullName,//Path.GetRelativePath(root, dir).Replace("\\", "/"),
            //        HasChild = System.IO.Directory.GetFileSystemEntries(dir).Any(),
            //        DateCreated = dirInfo.CreationTime,
            //        DateModified = dirInfo.LastWriteTime
            //    });
            //}

            //foreach (var file in System.IO.Directory.GetFiles(fullPath))
            //{
            //    var fileInfo = new FileInfo(file);

            //    files.Add(new FileManagerDirectoryContent
            //    {
            //        Name = fileInfo.Name ?? "",
            //        IsFile = true,
            //        Size = fileInfo.Length,
            //        Type = fileInfo.Extension ?? "",
            //        Path = Path.GetRelativePath(root, file).Replace("\\", "/"),
            //        DateCreated = fileInfo.CreationTime,
            //        DateModified = fileInfo.LastWriteTime,
            //        HasChild = false
            //    });
            //}

            //var cwd = new DirectoryInfo(fullPath);

            //var response = new FileManagerResponse<FileManagerDirectoryContent>
            //{
            //    CWD = new FileManagerDirectoryContent
            //    {
            //        Name = cwd.Name,
            //        Path = string.IsNullOrEmpty(relativePath) ? "/" : "/" + relativePath.Replace("\\", "/"),
            //        IsFile = false
            //    },

            //    Files = files
            //};
            //var obj = JsonConvert.SerializeObject(response);
            //return Ok(response);
            var files = new List<FileDirectoryContent>();

            if (!System.IO.Directory.Exists(fullPath))
                fullPath = root;

            // ---------------- FOLDERS ----------------
            foreach (var dir in System.IO.Directory.GetDirectories(fullPath))
            {
                var dirInfo = new DirectoryInfo(dir);

                var relative = Path.GetRelativePath(root, dir)
                    .Replace("\\", "/");

                files.Add(new FileDirectoryContent
                {
                    Name = dirInfo.Name ?? "",
                    IsFile = false,
                    Type = "Directory",
                    Size = 0,
                    Path = "/" + relative,   // ✅ IMPORTANT: virtual path only
                    HasChild = System.IO.Directory.EnumerateFileSystemEntries(dir).Any(),
                    DateCreated = dirInfo.CreationTime,
                    DateModified = dirInfo.LastWriteTime,

                });
            }

            // ---------------- FILES ----------------
            foreach (var file in System.IO.Directory.GetFiles(fullPath))
            {
                var fileInfo = new FileInfo(file);

                var relative = Path.GetRelativePath(root, file)
                    .Replace("\\", "/");

                files.Add(new FileDirectoryContent
                {
                    Name = fileInfo.Name ?? "",
                    IsFile = true,
                    Size = fileInfo.Length,
                    Type = fileInfo.Extension ?? "",
                    Path = "/" + relative,   //  IMPORTANT
                    HasChild = false,
                    DateCreated = fileInfo.CreationTime,
                    DateModified = fileInfo.LastWriteTime
                });
            }

            // ---------------- CWD ----------------
            var cwd = new DirectoryInfo(fullPath);

            var response = new FileManagerResponse<FileDirectoryContent>
            {
                CWD = new FileDirectoryContent
                {
                    //Name = cwd.Name,
                    Path = string.IsNullOrEmpty(virtualPath) ? "/" : virtualPath,
                    //IsFile = false
                    Name = cwd.Name,
                    IsFile = false,
                    HasChild = true,
                    FilterPath = root
                },
                Files = files
            };
            var obj = JsonConvert.SerializeObject(response);
            return Ok(response);
        }
        private IActionResult Read(string path)
        {
            var fullPath = string.IsNullOrEmpty(path) || path == "/"
                ? root
                : Path.Combine(root, path.TrimStart('\\'));


            var files = new List<FileDirectoryContent>();

            foreach (var dir in System.IO.Directory.GetDirectories(fullPath))
            {
                var dirInfo = new DirectoryInfo(dir);

                files.Add(new FileDirectoryContent
                {
                    Name = dirInfo.Name ?? "",
                    IsFile = false,
                    Type = "Directory",
                    Size = 0,
                    Path = dir,//Path.GetRelativePath(root, dirInfo.FullName).Replace("\\", "/"),
                    HasChild = System.IO.Directory.EnumerateFileSystemEntries(dirInfo.FullName).Any(),
                    DateCreated = dirInfo.CreationTime,
                    DateModified = dirInfo.LastWriteTime
                });
            }

            foreach (var file in System.IO.Directory.GetFiles(fullPath))
            {
                var fileInfo = new FileInfo(file);

                files.Add(new FileDirectoryContent
                {
                    Name = fileInfo.Name ?? "",
                    IsFile = true,
                    Size = fileInfo.Length,
                    Type = fileInfo.Extension ?? "",
                    Path = file,// Path.GetRelativePath(root, fileInfo.FullName).Replace("\\", "/"),
                    DateCreated = fileInfo.CreationTime,
                    DateModified = fileInfo.LastWriteTime,
                });
            }

            var response = new FileManagerResponse<FileDirectoryContent>
            {
                CWD = new FileDirectoryContent
                {
                    Name = new DirectoryInfo(fullPath).Name,
                    Path = path.Replace(root, "").Replace("\\", "/")
                },
                Files = files
            };

            return Ok(response);
        }
        //private IActionResult Details(FileDirectoryContent args)
        //{
        //    var fullPath = Path.Combine(
        //        @"\\DESKTOP-C86LJD9\AnuShareFolder",
        //        args.Path?.TrimStart('\\') ?? ""
        //    );

        //    if (System.IO.Directory.Exists(fullPath))
        //    {
        //        var dir = new DirectoryInfo(fullPath);

        //        var details = new FileDetails
        //        {
        //            Name = dir.Name,
        //            Location = dir.FullName,
        //            IsFile = false,
        //            Size = "0",
        //            Modified = dir.LastWriteTime.ToShortDateString(),
        //            Created = dir.CreationTime.ToShortDateString()
        //        };

        //        return Ok(new FileManagerResponse<FileDirectoryContent>
        //        {
        //            Details = details
        //        });
        //    }
        //    else if (System.IO.File.Exists(fullPath))
        //    {
        //        var file = new FileInfo(fullPath);

        //        var details = new FileDetails
        //        {
        //            Name = file.Name,
        //            Location = file.FullName,
        //            IsFile = true,
        //            Size = file.Length.ToString(),
        //            Modified = file.LastWriteTime.ToShortDateString(),
        //            Created = file.CreationTime.ToShortDateString()
        //        };

        //        return Ok(new FileManagerResponse<FileDirectoryContent>
        //        {
        //            Details = details
        //        });
        //    }

        //    return NotFound();
        //}
        // uploads the file(s) into a specified path
        // [Route("Upload")]
        [HttpPost("FileUpload")]
        [DisableRequestSizeLimit]
        public IActionResult Upload([FromForm] string path, [FromForm] long size, [FromForm] IList<IFormFile> uploadFiles, [FromForm] string action)
        {
            try
            {
                FileManagerResponse uploadResponse;
                foreach (var file in uploadFiles)
                {
                    var folders = (file.FileName).Split('/');
                    // checking the folder upload
                    if (folders.Length > 1)
                    {
                        for (var i = 0; i < folders.Length - 1; i++)
                        {
                            string newDirectoryPath = Path.Combine(this.basePath + path, folders[i]);
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
        public IActionResult Download([FromForm] string downloadInput)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            FileManagerDirectoryContent args = JsonSerializer.Deserialize<FileManagerDirectoryContent>(downloadInput, options);
            return operation.Download(args.Path, args.Names, args.Data);
        }
        //[HttpPost("FileDownload")]
        //public IActionResult Download([FromForm] string path, [FromForm] string[] names)
        //{
        //    return operation.Download(path, names);
        //}
        // gets the image(s) from the given path
        [HttpGet("FileGetImage")]
        public IActionResult GetImage(FileManagerDirectoryContent args)
        {
            return this.operation.GetImage(args.Path, args.Id, false, null, null);
        }
    }
}