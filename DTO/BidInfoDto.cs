namespace StowellCoAPI.DTO
{
    public class BidInfoDto
    {
        public string JobID { get; set; }
        public string? Bidder { get; set; }
        public string? Phase { get; set; }
        public string? BidStatus { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? JobType { get; set; }
        public string? JobName { get; set; }
        public string? ShortName { get; set; }
        public string? ContractNumber { get; set; }
        public string? ContractDate { get; set; }
        public decimal? ContractAmount { get; set; } = 0.0m;
        public string? EstStartDate { get; set; }
        public string? EstCompletionDate { get; set; }

        public ProjectLocationDto? ProjectLocation { get; set; }
        public ContractorInfoDto? ContractorInfo { get; set; }
        public ProjectInfoDto? ProjectInfo { get; set; }
    }

    public class ProjectLocationDto
    {
        public string? JobID { get; set; }
        public string? Jobsite { get; set; }
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? SalesTaxDistrict { get; set; }
    }

    public class ContractorInfoDto
    {
        public string? JobID { get; set; }
        public string? Client { get; set; }
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
    }

    public class ProjectInfoDto
    {
        public string? JobID { get; set; }
        public int? InsuranceType { get; set; }
        public int? Bonded { get; set; }
        public int? TaxExcempt { get; set; }
        public int? InvoiceSubmittal { get; set; }
        public int? CertifiedPayroll { get; set; }
        public string? NetTermsDate { get; set; }
        public decimal? RetainagePerc { get; set; } = 0.0m;
    }
    public class BidItem
    {
        public int ID { get; set; }
        public string JobID { get; set; }
        public DateTime BidDate { get; set; }
        public string Address { get; set; }
        public string Submitter { get; set; }
        public string BidStatus { get; set; }
    }

    public class BidQueueViewModel
    {
        public List<BidItem> OpenBids { get; set; }
        public List<BidItem> ClosedBids { get; set; }
        public List<BidItem> PendingBids { get; set; }
    }

    public class AccountingItem
    {
        public int ID { get; set; }
        public string JobID { get; set; }
        public string JobName { get; set; }
        public string JobType { get; set; }
        public DateTime BidDate { get; set; }
        public string Address { get; set; }
        public string Submitter { get; set; }
        public string BidStatus { get; set; }
        public string StatusID { get; set; }
    }

    public class ProjectBudgetQueueItem
    {
        public int ID { get; set; }
        public string JobID { get; set; }
        public string JobName { get; set; }
        public string JobType { get; set; }
        public string Address { get; set; }
        public string Phase { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
    }
    public class EmployeeDetails
    {
        public string Id { get; set; }
        public string Role { get; set; }
        public string Employee { get; set; }
    }
    public class StatusModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
    }
}
