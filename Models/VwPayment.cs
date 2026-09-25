using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwPayment
{
    public long? Jobnum { get; set; }

    public string Dscrpt { get; set; } = null!;

    public string? Invnum { get; set; }

    public DateOnly? Chkdte { get; set; }

    public string Chknum { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal Dsctkn { get; set; }

    public decimal Aplcrd { get; set; }

    public Guid Idnum { get; set; }
}
