using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class Bid
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public string Bidder { get; set; } = null!;

    public string? Phase { get; set; }

    public string? BidStatus { get; set; }

    public string? Division { get; set; }

    public string? Department { get; set; }

    public string? JobType { get; set; }

    public string? JobName { get; set; }

    public string? ShortName { get; set; }

    public string? ContractNumber { get; set; }

    public DateOnly? ContractDate { get; set; }

    public decimal? ContractAmount { get; set; }

    public DateOnly? EstStartDate { get; set; }

    public DateOnly? EstCompletionDate { get; set; }

    public bool? IsNew { get; set; }

    public bool? IsAccountingQueue { get; set; }

    public string? Submitter { get; set; }

    public DateOnly? BidDate { get; set; }

    public DateOnly? DateModified { get; set; }

    public bool? IsPushtoSage { get; set; }

    public DateOnly? AccountingQueueDateModified { get; set; }

    public DateOnly? PushtoSageDateModified { get; set; }

    public decimal? TotalBudget { get; set; }

    public decimal? TotalPaid { get; set; }

    public DateOnly? LastModified { get; set; }

    public string? LastModifiedBy { get; set; }
}
