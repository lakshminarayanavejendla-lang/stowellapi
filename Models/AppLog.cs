using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class AppLog
{
    public long LogId { get; set; }

    public DateTime? LogTime { get; set; }

    public string? LogLevel { get; set; }

    public string? Username { get; set; }

    public string? Module { get; set; }

    public string? LogMessage { get; set; }
}
