using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwGetBidderName
{
    public string Bidder { get; set; } = null!;

    public string? Email { get; set; }
}
