using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class Job
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public string? JobName { get; set; }

    public string Division { get; set; } = null!;

    public DateOnly? RequestedDeliveryDate { get; set; }

    public string? EstimatedId { get; set; }

    public string? JobType { get; set; }

    public DateOnly? ContractualDeliveryDate { get; set; }

    public DateOnly? EstimatedDate { get; set; }

    public DateOnly? ContractDate { get; set; }

    public DateOnly? ContractReviewCompleteDate { get; set; }

    public string? FolderLocation { get; set; }

    public string? CreatedBy { get; set; }

    public string? CreatedByEmail { get; set; }

    public string? ModifiedBy { get; set; }

    public string? ModifiedByEmail { get; set; }

    public string? ClientId { get; set; }

    public string? CompanyName { get; set; }

    public string? ClientContract { get; set; }

    public string? Address { get; set; }

    public string? Address2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public int? Zip { get; set; }

    public string? Phone { get; set; }

    public string? ContactEmail { get; set; }

    public string? InvoiceType { get; set; }

    public DateOnly? NetTermsDate { get; set; }

    public decimal? Retainagepercent { get; set; }

    public string? NtofilingStatus { get; set; }

    public DateOnly? DateNeeded { get; set; }

    public string? JobSite { get; set; }

    public string? JobAddress { get; set; }

    public string? JobAddress2 { get; set; }

    public string? JobCity { get; set; }

    public string? JobState { get; set; }

    public int? JobZip { get; set; }

    public string? JobPhone { get; set; }

    public string? JobEmail { get; set; }

    public string? GeneralContractor { get; set; }

    public string? ContractorPm { get; set; }
}
