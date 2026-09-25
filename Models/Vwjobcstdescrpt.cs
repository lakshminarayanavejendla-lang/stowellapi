using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class Vwjobcstdescrpt
{
    public long? RowId { get; set; }

    public string Dscrpt { get; set; } = null!;

    public decimal Cstcde { get; set; }

    public long Jobnum { get; set; }
}
