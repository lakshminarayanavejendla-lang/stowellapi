using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class CostCodeList
{
    public int Id { get; set; }

    public string CostCodeDescription { get; set; } = null!;

    public string? ShortDescription { get; set; }

    public int? PreferredCode { get; set; }

    public DateTime CreatedDate { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }
    public string Description { get; set; }
}
