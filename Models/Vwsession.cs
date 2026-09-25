using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class Vwsession
{
    public short SessionId { get; set; }

    public string? DatabaseName { get; set; }

    public string LoginName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? HostName { get; set; }

    public string? ProgramName { get; set; }

    public DateTime LastRequestStartTime { get; set; }

    public int? Id { get; set; }
}
