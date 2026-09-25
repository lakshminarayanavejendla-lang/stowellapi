using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class GetProjectDetail
{
    public long Recnum { get; set; }

    public int? JobNumber { get; set; }

    public string Jobnme { get; set; } = null!;

    public string? Pmemail { get; set; }

    public string? Apmemail { get; set; }

    public string? Division { get; set; }

    public string? UserName { get; set; }

    public byte StatusId { get; set; }

    public string? Status { get; set; }
}
