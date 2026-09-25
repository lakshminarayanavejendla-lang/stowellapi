using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class Subcnt
{
    public string Dscrpt { get; set; } = null!;

    public decimal? Cstcde { get; set; }

    public byte? Csttyp { get; set; }

    public decimal Amount { get; set; }

    public decimal Change { get; set; }

    public decimal Cntrct { get; set; }

    public string Gstsbj { get; set; } = null!;

    public string Pstsbj { get; set; } = null!;

    public string Hstsbj { get; set; } = null!;

    public decimal Billed { get; set; }

    public decimal Remain { get; set; }

    public string Usrdf1 { get; set; } = null!;

    public string Ntetxt { get; set; } = null!;

    public string? Linref { get; set; }

    public Guid Idnum { get; set; }
}
