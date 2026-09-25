using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class BudgetTransactionLog
{
    public int LogId { get; set; }

    public int TransactionId { get; set; }

    public long JobId { get; set; }

    public string Action { get; set; } = null!;

    public string? CostCodeDescription { get; set; }

    public decimal? Budget { get; set; }

    public decimal? ApprovedBudget { get; set; }

    public short? PhaseNumber { get; set; }

    public string? PhaseName { get; set; }

    public string? BudgetSubmittedBy { get; set; }

    public DateOnly? SubmittedDate { get; set; }

    public string? BudgetApprovedBy { get; set; }

    public DateOnly? ApprovedDate { get; set; }

    public bool? IsNewBudget { get; set; }

    public bool? Isapproved { get; set; }

    public bool? IsRejected { get; set; }

    public string? RejectedBy { get; set; }

    public DateOnly? RejectedDate { get; set; }

    public DateTime? LogDate { get; set; }

    public string? LogUser { get; set; }

    public virtual BudgetTransaction Transaction { get; set; } = null!;
}
