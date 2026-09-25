using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace StowellCoAPI.Controllers
{
    // NEW controller - Stowell PO Module. Backs the "File Upload" drop zone on the FORM-PO
    // Request / FORM-CO Request pages, which was previously a static placeholder with no upload
    // wired up. Deliberately does NOT reuse BidsDocumentManagementController/FileManagerController
    // - both are Syncfusion FileManager-backed full file browsers (read/copy/move/rename/search)
    // with a lot of half-finished/commented-out SharePoint-via-Graph experimentation. This
    // controller is a minimal, focused "attach files to a job's PO/CO folder" endpoint using
    // plain System.IO instead.
    //
    // Files are stored on local disk under {ContentRoot}/Uploads/PoAttachments/{jobId}/, keyed
    // only by job ID (not by a specific PO/CO draft) since the real PO/CO number doesn't exist
    // until Submit succeeds - a known simplification for this first pass.
    [ApiController]
    public class PoAttachmentsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PoAttachmentsController> _logger;
        private static readonly Regex SafeJobId = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
        private readonly IConfiguration _configuration;
        public PoAttachmentsController(IWebHostEnvironment env, ILogger<PoAttachmentsController> logger,IConfiguration configuration)
        {
            _env = env;
            _logger = logger;
            _configuration = configuration;
        }

        // FIX (2026-09-24, Mike Smith - "Document from PO did not save in Doc Library (needs to save in 11-Purchase Orders
        // folder)"): files used to go to FileStorage:PoAttachmentsPath (a stale dev-machine share), keyed only by job. They are
        // now saved in the job's own project folder on the project document share - the same "{job} - {name}" folder the
        // Documents tab shows (ProjectDocSettings:RootSharePath) - inside its "11-Purchase Orders" sub-folder, with the file's
        // real name.
        private const string PoFolderName = "11-Purchase Orders";
        private string? ProjectRoot => _configuration["ProjectDocSettings:RootSharePath"];

        private IActionResult ValidateJobId(string jobId, out string folder, bool createPoFolder = false)
        {
            folder = null;
            if (string.IsNullOrWhiteSpace(jobId) || !SafeJobId.IsMatch(jobId))
                return BadRequest(new { Message = "Invalid jobId." });

            var root = ProjectRoot;
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return StatusCode(500, new { Message = "The project document folder is not reachable from the API.", Details = "ProjectDocSettings:RootSharePath = " + root });

            // the project's folder is named "{recnum} - {job name}" (see BidsController.ConvertBidToProject)
            var projectFolder = Directory.EnumerateDirectories(root, jobId + " - *").FirstOrDefault()
                                ?? Directory.EnumerateDirectories(root, jobId + "*").FirstOrDefault();
            if (projectFolder == null)
                return NotFound(new { Message = $"The project folder for job {jobId} was not found in the document library.", Details = root });

            // the template's sub-folder for purchase orders: "11-Purchase Orders" (tolerate small naming differences)
            var poFolder = Directory.EnumerateDirectories(projectFolder)
                .FirstOrDefault(d => Path.GetFileName(d).StartsWith("11", StringComparison.Ordinal)
                                     && Path.GetFileName(d).Contains("Purchase", StringComparison.OrdinalIgnoreCase));
            if (poFolder == null)
            {
                poFolder = Path.Combine(projectFolder, PoFolderName);
                if (createPoFolder) Directory.CreateDirectory(poFolder);
            }
            folder = poFolder;
            return null;
        }

        /// <summary>A file name that does not overwrite an existing document: "name.ext", then "name (1).ext", ...</summary>
        private static string UniqueName(string folder, string fileName)
        {
            string candidate = fileName;
            string stem = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            for (int i = 1; System.IO.File.Exists(Path.Combine(folder, candidate)); i++)
                candidate = $"{stem} ({i}){ext}";
            return candidate;
        }

        [HttpPost("api/PoAttachments/Upload")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Upload([FromForm] string jobId, [FromForm] List<IFormFile> files)
        {
            var invalid = ValidateJobId(jobId, out var folder, createPoFolder: true);
            if (invalid != null) return invalid;

            try
            {
                Directory.CreateDirectory(folder);
                var saved = new List<object>();

                foreach (var file in files ?? new List<IFormFile>())
                {
                    if (file.Length == 0) continue;

                    string safeName = Path.GetFileName(file.FileName);
                    string storedName = UniqueName(folder, safeName);
                    string fullPath = Path.Combine(folder, storedName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    saved.Add(new { name = storedName, storedName, size = file.Length });
                }

                return Ok(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while uploading files.", Details = ex.Message });
            }
        }

        [HttpGet("api/PoAttachments/List/{jobId}")]
        public IActionResult List(string jobId)
        {
            var invalid = ValidateJobId(jobId, out var folder);
            if (invalid != null) return invalid;

            try
            {
                if (!Directory.Exists(folder)) return Ok(new List<object>());

                var files = new DirectoryInfo(folder).GetFiles()
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .Select(f => new
                    {
                        name = DisplayName(f.Name),
                        storedName = f.Name,
                        size = f.Length,
                        uploaded = f.CreationTimeUtc
                    })
                    .ToList();

                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while listing files.", Details = ex.Message });
            }
        }

        [HttpGet("api/PoAttachments/Download/{jobId}/{storedName}")]
        public IActionResult Download(string jobId, string storedName)
        {
            var invalid = ValidateJobId(jobId, out var folder);
            if (invalid != null) return invalid;

            string safeStoredName = Path.GetFileName(storedName);
            string fullPath = Path.Combine(folder, safeStoredName);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
            return File(bytes, "application/octet-stream", DisplayName(safeStoredName));
        }

        [HttpDelete("api/PoAttachments/Delete/{jobId}/{storedName}")]
        public IActionResult Delete(string jobId, string storedName)
        {
            var invalid = ValidateJobId(jobId, out var folder);
            if (invalid != null) return invalid;

            try
            {
                string safeStoredName = Path.GetFileName(storedName);
                string fullPath = Path.Combine(folder, safeStoredName);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                return Ok(new { deleted = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the file.", Details = ex.Message });
            }
        }

        // Files are stored under their real name in the library now (no timestamp prefix to strip).
        private static string DisplayName(string storedName) => storedName;
    }
}
