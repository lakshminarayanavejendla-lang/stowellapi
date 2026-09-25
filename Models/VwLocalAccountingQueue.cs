using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwLocalAccountingQueue
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public string? JobName { get; set; }

    public string? JobType { get; set; }

    public DateOnly? BidDate { get; set; }

    public string? Address { get; set; }

    public string? Submitter { get; set; }

    public string? BidStatus { get; set; }

    public int? StatusId { get; set; }
}
