using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class McostCode
{
    public int Id { get; set; }

    public decimal? CostCode { get; set; }

    public string? Description { get; set; }

    public string? Column5 { get; set; }

    public string? Column6 { get; set; }

    public string? Material { get; set; }

    public string? Labor { get; set; }

    public string? Equipment { get; set; }

    public string? Subcontract { get; set; }

    public string? GcOther { get; set; }

    public string? Column7 { get; set; }

    public string? Column8 { get; set; }

    public string? Column9 { get; set; }

    public string? Column10 { get; set; }

    public string? Column11 { get; set; }

    public string? PhaseA { get; set; }

    public string? PhaseB { get; set; }

    public string? PhaseC { get; set; }
}
