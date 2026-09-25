using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwInvoiceretentionByEmail
{
    public string EmailId { get; set; } = null!;

    public long FileNumber { get; set; }

    public decimal? TotalRetain { get; set; }
}
