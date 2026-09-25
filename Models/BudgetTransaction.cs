using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class BudgetTransaction
{
    public int Id { get; set; }

    public long JobId { get; set; }

    public string CostCodeDescription { get; set; } = null!;

    public decimal Budget { get; set; }

    public decimal? ApprovedBudget { get; set; }

    public short? PhaseNumber { get; set; }

    public string? PhaseName { get; set; }

    public bool? IsNewBudget { get; set; }

    public string? BudgetSubmittedBy { get; set; }

    public DateOnly? SubmittedDate { get; set; }

    public bool? Isapproved { get; set; }

    public string? BudgetApprovedBy { get; set; }

    public DateOnly? ApprovedDate { get; set; }

    public bool? IsRejected { get; set; }

    public string? RejectedBy { get; set; }

    public DateOnly? RejectedDate { get; set; }

    public virtual ICollection<BudgetTransactionLog> BudgetTransactionLogs { get; set; } = new List<BudgetTransactionLog>();
}
