using System.Text.Json.Serialization;

namespace StowellCoAPI.DTO
{
    public class CurrentCostSummaryViewModel
    {
        public string SelectedJobId { get; set; }
        public string SelectedJobName { get; set; }
        public string ShareFolderLink { get; set; }
        public List<string> SelectedCostCodeIds { get; set; }
        public string SelectedPhaseId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<Job> Jobs { get; set; }
        public List<CostCode> CostCodes { get; set; }
        public List<Phase> Phases { get; set; }
        public CostCodeSummaryModel CostCodeSummaryModel { get; set; }
        public CashFlowRecord CashFlowRecord { get; set; }
        public BudgetPayment BudgetPayment { get; set; }
    
    }

    public class CostCodeSummaryModel
    {
        public List<CostCodeSummaryRecord> CostCodeSummary { get; set; }
        public decimal SumOfBudgetAndChanges { get; set; }
        public decimal SumOfToDate { get; set; }
        public decimal SumOfThisPeriod { get; set; }
        public decimal SumOfRemaining { get; set; }
        // Open PO commitments (already subtracted from SumOfRemaining) - see SPCostSummary.
        public decimal SumOfCommitted { get; set; }
        public int Percentage { get{ return SumOfBudgetAndChanges > 0 ? (int)(((SumOfBudgetAndChanges - SumOfRemaining) / SumOfBudgetAndChanges)*100) : 0; }}
        //public decimal Percentage { get{ return 70; }}
    }

    public class Job
    {
        public string FullJob { get; set; }
        public string JobName { get; set; }
        public string Recnum { get; set; }
    }
    public class CostCode
    {
        public string Id { get; set; }
        public string Description { get; set; }
    }
    public class Phase
    {
        public string Recnum { get; set; }
        public string PhaseNum { get; set; }
        public string PhaseName { get; set; }
        public string? Source { get; set; }
        public string? Email { get; set; }
    }

    public class CostCodeSummaryRecord
    {
        public int Id { get; set; }
        public string JobNumber { get; set; }
        public string CostCode { get; set; }
        public string CostCodeDescription { get; set; }
        public decimal BudgetAndChanges { get; set; }
        public decimal ToDate { get; set; }
        public decimal ThisPeriod { get; set; }
        public decimal Remaining { get; set; }
        // Open PO commitments against this cost code (already subtracted from Remaining).
        public decimal Committed { get; set; }

    }

    public class BudgetRecord
    {
        public string JobNumber { get; set; }
        public string? CostCode { get; set; }
        public string CostCodeDescription { get; set; }
        public decimal? TotalBudget { get; set; }  // nullable
        public decimal? UpdateBudget { get; set; }  // nullable
        public decimal? PhaseNum { get; set; }     // nullable
        public string? Source { get; set; }
        public string? Email { get; set; }
        public string? ActionType { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
    //public class CostCodeList
    //{
    //    public string Id { get; set; }
    //    public string CostCode { get; set; }
    //    public string Description { get; set; }
    //}

    public class BudgetPayment
    {
        public string Recnum { get; set; }
        public string Budget { get; set; }
        public string Paid { get; set; }
        public string Balance { get; set; }  // nullable
    }

    //public class CostCodeRecord
    //{
    //    public int JobNumber { get; set; }
    //    public string Address { get; set; }
    //    public string City { get; set; }
    //    public string State { get; set; }
    //    public string Zip { get; set; }
    //}

    public class CurrentJob
    {
        public string Recnum { get; set; }
        public string JobID { get; set; }
        public string JobName { get; set; }
        public string Address { get; set; }
        public string CreatedBy { get; set; }
        public string Status { get; set; }
        /// <summary>Percent complete (0-100) from actrec.pctcmp.</summary>
        public decimal PercentComplete { get; set; }
    }
    public class CostCodeRecord
    {
        public int RecNum { get; set; }
        public string JobNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string RecJobNumber => $"{RecNum} - {JobNumber}";
    }
    public class ContractorClient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
    }
    public class ProjectUser
    {
        public int RecNum { get; set; }
        public string JobName { get; set; }
        public string PM1 { get; set; }
        public string PM2 { get; set; }
        public string Division { get; set; }
    }
    public class CashFlowRecord
    {
        public int RecNum { get; set; }
        public string JobNumber { get; set; }
        public decimal CashCollected { get; set; }
        public decimal CashPaid { get; set; }
        public decimal NetCash { get; set; }
        public decimal InvoiceJ2D { get; set; }
        public decimal A_REndBal_Today { get; set; }
        public decimal jobcoststodate { get; set; }
        public decimal A_PEndBal_Today { get; set; }
        public decimal CashPaidOut { get; set; }
        public decimal NetCashInOut { get; set; }
    }
    public class JobCostJournal
    {
        public string CostCode { get; set; }
        public string Record { get; set; }
        public string Trans { get; set; }
        public string Description { get; set; }
        public string Equipment { get; set; }
        public string CostType { get; set; }
        public decimal Cost { get; set; }
        public int RecType { get; set; }
        public int Id { get; set; }
        public int PhsNum { get; set; }
        public string PhsNme { get; set; }
    }
    public class Employee
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
    }
    public class PrimeChangeList
    {
        public string JobNum { get; set; }
        public string JobName { get; set; }
        public string RecNum { get; set; }
        public string ChgNum { get; set; }
        public DateTime ChgDate { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public decimal ReqAmt { get; set; }
        public decimal AppAmt { get; set; }
    }

    public class InvoiceReceivable
    {
        public int RecNum { get; set; }
        public string InvNum { get; set; }
        public string InvDate { get; set; }
        public string Description { get; set; }
        public string DueDate { get; set; }
        public int Status { get; set; }
        public decimal InvTtl { get; set; }
        public decimal AmtPad { get; set; }
        public decimal DiscCred { get; set; }
        public decimal InvBal { get; set; }
        public decimal Retain { get; set; }
        public decimal InvNet { get; set; }
    }

    public class InvoicePayment
    {
        public int RecNum { get; set; }
        public string JobName { get; set; }
        public string InvNum { get; set; }
        public string InvDate { get; set; }
        public string DueDate { get; set; }
        public decimal InvoiceTotal { get; set; }
        public decimal Balance { get; set; }
    }

    public class ContractSummary
    {
        public decimal OriginalContractAmount { get; set; }
        public decimal ChangesToDate { get; set; }
        public decimal NewContract { get; set; }
        public decimal InvoicedToDate { get; set; }
        public decimal BalanceOnContract { get; set; }
        public string JobName { get; set; }
    }

    public class CashFlowQueryInput
    {
        public string recnum { get; set; }
        //public string period { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    public class ChartData
    {
        [JsonPropertyName("labels")]
        public string[] Labels { get; set; }
        [JsonPropertyName("datasets")]
        public List<ChartDataset> Datasets { get; set; }
    }

    public class ChartDataset
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }          // Dataset name ("Sales", "Expenses")

        [JsonPropertyName("data")]
        public decimal[] Data { get; set; }        // Y-axis values

        [JsonPropertyName("backgroundColor")]
        public string BackgroundColor { get; set; } // For bar/line color

        [JsonPropertyName("borderColor")]
        public string BorderColor { get; set; }     // Line/border color
    }


    public class InvoiceRetention
    {
        public string filenumber { get; set; }
        public decimal total_retain { get; set; }
    }
    public class CashFlowDetail
    {
        public string? JobName { get; set; }
        public decimal CashCollected { get; set; }
        public decimal CashPaid { get; set; }
        public decimal NetCashOut { get; set; }
    }
    public class ChartPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }

}
