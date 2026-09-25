using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ProjectInfo
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public string? InsuranceType { get; set; }

    public string? Bonded { get; set; }

    public string? TaxExcempt { get; set; }

    public string? InvoiceSubmittal { get; set; }

    public string? CertifiedPayroll { get; set; }

    public DateOnly? NetTermsDate { get; set; }

    public decimal? RetainagePerc { get; set; }

    public DateOnly? DateModified { get; set; }
}
