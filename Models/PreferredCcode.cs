using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class PreferredCcode
{
    public int? PreferredCode { get; set; }

    public string Description { get; set; } = null!;

    public decimal? CostCode { get; set; }
}
