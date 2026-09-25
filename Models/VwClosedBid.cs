using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwClosedBid
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public DateOnly? BidDate { get; set; }

    public string? Address { get; set; }

    public string Submitter { get; set; } = null!;

    public string? BidStatus { get; set; }
}
