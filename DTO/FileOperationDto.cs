using System;
using System.Collections.Generic;

namespace StowellCoAPI.DTO
{
    /// <summary>
    /// Neutral DTO for file/folder information independent of UI framework.
    /// Used by file manager API endpoints.
    /// </summary>
    public class FileDirectoryContent
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public bool IsFile { get; set; }
        public string Type { get; set; }
        public bool HasChild { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public string FilterPath { get; set; }
    }

    public class FileDetails
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public bool IsFile { get; set; }
        public string Size { get; set; }
        public string Modified { get; set; }
        public string Created { get; set; }
    }

    public class ErrorDetails
    {
        public string Message { get; set; }
        public string Code { get; set; }
    }

    /// <summary>
    /// Generic response wrapper for file manager operations.
    /// </summary>
    public class FileManagerResponse<T>
    {
        public T CWD { get; set; }
        public IEnumerable<T> Files { get; set; }
        public ErrorDetails Error { get; set; }
        public FileDetails Details { get; set; }
    }
}
