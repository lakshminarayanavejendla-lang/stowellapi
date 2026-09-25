namespace StowellCoAPI.DTO
{
    // Stowell PO/CO Module - Change Order Budget Allocation (Moqups page FORM-CO Reallocation).
    // Mirrors core/models/project-management/co-budget-allocation.model.ts field-for-field.
    // Backed by CO_SaveBudgetReallocation/CO_SubmitBudgetReallocation/CO_GetBudgetReallocationById,
    // which already existed in the local SAGESBQ database (2026-09-13) writing to the app-owned
    // dbo.CoBudgetAllocations/-Phases/-Lines tables - this DTO/controller layer was the missing
    // piece connecting those procs to the Angular app.

    public class CoBudgetLineDto
    {
        public string CostCode { get; set; }
        public string CostCodeDescription { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class CoBudgetAllocationRequestDto
    {
        public string CoNumber { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string CoType { get; set; }
        public string CoDescription { get; set; }
        public string CoReason { get; set; }
        public string CoStatus { get; set; }
        public decimal OverallBudget { get; set; }
        public decimal ProfitOnSell { get; set; }
        public decimal? RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string CreatedBy { get; set; }
        public List<string> Phases { get; set; } = new();
        public List<CoBudgetLineDto> Lines { get; set; } = new();
    }

    public class CoBudgetAllocationDetail
    {
        public int AllocationId { get; set; }
        public string CoNumber { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string CoType { get; set; }
        public string CoDescription { get; set; }
        public string CoReason { get; set; }
        public string CoStatus { get; set; }
        public decimal OverallBudget { get; set; }
        public decimal BudgetChangeTotal { get; set; }
        public decimal ProfitOnSell { get; set; }
        public decimal? RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        // Allocation record's own lifecycle status (Draft/Submitted) - distinct from CoStatus,
        // which is the user-picked CO status dropdown (Draft/Pending/Approved/Rejected).
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> Phases { get; set; } = new();
        public List<CoBudgetLineDto> Lines { get; set; } = new();
    }
}
