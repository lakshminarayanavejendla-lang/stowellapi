using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class SageBudgetTransaction
{
    public int Id { get; set; }

    public long JobId { get; set; }

    public string CostCodeDescription { get; set; } = null!;

    public decimal Budget { get; set; }

    public short? PhaseNumber { get; set; }

    public string? PhaseName { get; set; }

    public DateOnly? Saveddate { get; set; }

    public string? Submittedby { get; set; }
}
