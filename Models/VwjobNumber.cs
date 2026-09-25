using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwjobNumber
{
    public string? Fulljob { get; set; }

    public string Jobnme { get; set; } = null!;

    public long Recnum { get; set; }
}
