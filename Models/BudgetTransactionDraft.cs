using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class BudgetTransactionDraft
{
    public long DraftId { get; set; }

    public long? JobId { get; set; }

    public string? CostCodeDescription { get; set; }

    public decimal? Budget { get; set; }

    public short? PhaseNumber { get; set; }

    public string? PhaseName { get; set; }

    public string? BudgetSubmittedBy { get; set; }

    public bool? IsSave { get; set; }

    public DateOnly? SavedDate { get; set; }

    public bool? Issubmitted { get; set; }

    public DateOnly? SubmittedDate { get; set; }

    public string? SubmittedBy { get; set; }
}
