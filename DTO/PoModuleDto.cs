namespace StowellCoAPI.DTO
{
    // Stowell PO Module DTOs - mirror the Angular models in
    // core/models/project-management/{po-queue,co-queue,job-po}.model.ts field-for-field so the
    // client can deserialize without changes (ASP.NET Core's default camelCase JSON naming policy
    // turns these PascalCase properties into the camelCase fields the Angular side expects).

    public class PoQueueRecord
    {
        public string MasterJobPoId { get; set; }
        public string PoStatus { get; set; }
        public string JobName { get; set; }
        public string Pm { get; set; }
    }

    public class PoQueueData
    {
        public List<PoQueueRecord> ActivePOs { get; set; } = new();
        public List<PoQueueRecord> CompletedPOs { get; set; } = new();
    }

    public class PurchaseOrderRecord
    {
        public string PoId { get; set; }
        public string PoDescription { get; set; }
        public string Status { get; set; }
        public string DateSubmitted { get; set; }
        public string RequiredOnsite { get; set; }
        public string Vendor { get; set; }
        // Sage phase number (pchord.phsnum) - lets the PO tab show one phase at a time.
        public int? PoPhase { get; set; }
    }

    public class ChangeOrderLogRecord
    {
        public string CoId { get; set; }
        public string CoDescription { get; set; }
        public decimal CoAmount { get; set; }
        public string CoStatus { get; set; }
        public string CoReason { get; set; }
        // Sage phase number (prmchg.phsnum) - lets the CO tab show one phase at a time.
        public int? CoPhase { get; set; }
    }

    public class BackChargeLogRecord
    {
        public string BcId { get; set; }
        public string BcDescription { get; set; }
        public decimal BcAmount { get; set; }
        public string BcStatus { get; set; }
        public string BcReason { get; set; }
    }

    public class JobPoOverviewData
    {
        public List<PurchaseOrderRecord> PurchaseOrders { get; set; } = new();
        public List<ChangeOrderLogRecord> ChangeOrderLog { get; set; } = new();
        public List<BackChargeLogRecord> BackChargeLog { get; set; } = new();
    }

    public class CoQueueRecord
    {
        public string MasterJobCoId { get; set; }
        // FLAG: mockup literally labels this column "PO Status" on the CO Queue page too -
        // preserved as-is to match the Angular-side field name (co-queue.model.ts).
        public string PoStatus { get; set; }
        public string JobName { get; set; }
        public string Pm { get; set; }
        // NEW - set only when this row's MasterJobCoId also has a dbo.CoBudgetAllocations row
        // (CO_GetQueueData LEFT JOINs on CoNumber = chgnum). Lets the Angular grid send a row to
        // the CO Budget Allocation edit page instead of the plain edit-co-form page - see
        // CO_SubmitBudgetReallocation's header comment for why a Budget Allocation submission
        // also creates a StowellSandbox.dbo.prmchg row (so it shows up in this same queue).
        public int? AllocationId { get; set; }
    }

    public class CoQueueData
    {
        public List<CoQueueRecord> ActiveCOs { get; set; } = new();
        public List<CoQueueRecord> CompletedCOs { get; set; } = new();
    }

    // Request-body DTOs for the write side (Save/Submit). Mirror po-request.model.ts /
    // co-request.model.ts field-for-field; ASP.NET Core's default JSON model binder is
    // case-insensitive so the Angular side's camelCase payload binds to these PascalCase
    // properties without any [JsonPropertyName] attributes.

    public class PoRequestItemDto
    {
        public string Item { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal ItemCost { get; set; }
        public string ItemNote { get; set; }
        // NEW - see PO_SubmitRequest/PO_UpdatePo (2026-09-10 DB changes): the line's Sage cost
        // code (pcorln.cstcde), now persisted end-to-end (submit/update write it, GetPoById reads
        // it back) so re-editing a PO no longer drops the cost-code linkage. Not stored for
        // drafts/CO items - PoRequestDraftItems and sbcgln have no cost-code column.
        public decimal? CostCode { get; set; }
    }

    public class PoBackChargeDto
    {
        public string Vendor { get; set; }
        public decimal? PercentCharge { get; set; }
        public decimal DollarCharge { get; set; }
        public string Reason { get; set; }
    }

    public class PoRequestSubmissionDto
    {
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string Requester { get; set; }
        public string Vendor { get; set; }
        public string PoDescription { get; set; }
        public string DateRequested { get; set; }
        public string RequiredOnSite { get; set; }
        public decimal TotalPoAmount { get; set; }
        // NEW (2026-09-14) - the real authenticated app user (AuthService.getLoggedInUserEmail()),
        // distinct from Requester (a free-text business field with no connection to who's actually
        // logged in). This is what actually feeds pchord.usrnme now, for a trustworthy audit trail.
        public string CreatedBy { get; set; }
        // NEW (2026-09-23, Moqups FORM-PO Request: "User must select Project Phase") - the Sage phase
        // number this PO belongs to; written to pchord.phsnum on submit/update, kept on drafts.
        public int? PhaseNumber { get; set; }
        public List<PoRequestItemDto> Items { get; set; } = new();
        public List<PoBackChargeDto> BackCharges { get; set; } = new();
    }

    public class CoRequestSubmissionDto
    {
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string PurchaseOrderId { get; set; }
        public string Requester { get; set; }
        public string Vendor { get; set; }
        public string CoReason { get; set; }
        public string DateRequested { get; set; }
        public string RequiredOnSite { get; set; }
        public decimal TotalCoAmount { get; set; }
        // NEW (2026-09-14) - see PoRequestSubmissionDto.CreatedBy's comment; same fix, same reason.
        public string CreatedBy { get; set; }
        public List<PoRequestItemDto> Items { get; set; } = new();
        public List<PoBackChargeDto> BackCharges { get; set; } = new();
    }

    // Draft list/resume DTOs - backs the "Draft Purchase Orders"/"Draft Change Orders" sections
    // on PO/CO Queue Home. Mirror PO_GetDrafts/PO_GetDraftById (and the CO equivalents) exactly.

    public class PoDraftSummary
    {
        public int DraftId { get; set; }
        public string JobId { get; set; }
        public string Vendor { get; set; }
        public string PoDescription { get; set; }
        public string Requester { get; set; }
        public decimal TotalAmount { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }
        // Sage phase number the draft was saved under (null = none chosen) - lets the PO tab filter drafts by phase.
        public int? PhaseNumber { get; set; }
    }

    public class PoDraftDetail
    {
        public int DraftId { get; set; }
        public string JobId { get; set; }
        public string Vendor { get; set; }
        public string PoDescription { get; set; }
        public string Requester { get; set; }
        public string DateRequested { get; set; }
        public string RequiredOnSite { get; set; }
        public decimal TotalAmount { get; set; }
        public int? PhaseNumber { get; set; }
        public List<PoRequestItemDto> Items { get; set; } = new();
    }

    public class CoDraftSummary
    {
        public int DraftId { get; set; }
        public string JobId { get; set; }
        public string PurchaseOrderId { get; set; }
        public string Vendor { get; set; }
        public string CoReason { get; set; }
        public string Requester { get; set; }
        public decimal TotalAmount { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }
    }

    // Backs the vendor picker on FORM-PO Request / FORM-CO Request, replacing what was free text
    // with a real dropdown sourced from StowellSandbox.dbo.actpay (the Sage vendor master) - the
    // exact table PO_SubmitRequest/CO_SubmitRequest already validate the typed vendor name
    // against. Shared by both forms; lives on PoQueueController since it was built first.
    public class VendorOption
    {
        public long Recnum { get; set; }
        public string VendorName { get; set; }
    }

    // Backs the cost code picker on FORM-PO Request / FORM-CO Request, replacing free-text
    // "Item" with a real dropdown scoped to the selected job's actual budget lines
    // (StowellSandbox.dbo.bdglin) - see PO_GetJobCostCodes in po_module_costcodes.sql. Remaining
    // is computed the same way dbo.SPJobSummary already does (Budget + Changes - ToDate spent).
    public class JobCostCodeOption
    {
        public decimal CostCode { get; set; }
        public string CostCodeDescription { get; set; }
        public decimal Budget { get; set; }
        public decimal ToDate { get; set; }
        public decimal Remaining { get; set; }
    }

    // NEW - backs the Phase dropdown on FORM-CO Reallocation (Change Order Budget Allocation),
    // replacing free-text Phase entry with a real dropdown scoped to the selected job's actual
    // phases (StowellSandbox.dbo.jobphs) - see PO_GetJobPhases.
    public class JobPhaseOption
    {
        public int PhaseNumber { get; set; }
        public string PhaseName { get; set; }
    }

    public class CoDraftDetail
    {
        public int DraftId { get; set; }
        public string JobId { get; set; }
        public string PurchaseOrderId { get; set; }
        public string Vendor { get; set; }
        public string CoReason { get; set; }
        public string Requester { get; set; }
        public string DateRequested { get; set; }
        public string RequiredOnSite { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PoRequestItemDto> Items { get; set; } = new();
    }

    // Backs PoQueueController.GetPoById - edit-on-click for an already-submitted, real Sage PO.
    // Mirrors Angular's PoDetail (po-request.model.ts). See PO_GetPoById (2026-09-10 DB changes).
    public class PoDetail
    {
        public string PoId { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string Requester { get; set; }
        public string Vendor { get; set; }
        public string PoDescription { get; set; }
        public string DateRequested { get; set; }
        public string RequiredOnSite { get; set; }
        public decimal TotalPoAmount { get; set; }
        public int? PhaseNumber { get; set; }
        public List<PoRequestItemDto> Items { get; set; } = new();
        public List<PoBackChargeDto> BackCharges { get; set; } = new();
    }

    // Backs CoQueueController.GetCoById - edit-on-click for an already-submitted, real Sage CO.
    // Mirrors Angular's CoDetail (co-request.model.ts). See CO_GetCoById (2026-09-10 DB changes).
    // Vendor is always "" - CO_SubmitRequest/CO_UpdateCo have never had a header vendor column to
    // write to (only dbo.BackCharges.Vendor is ever populated); not fabricated here.
    public class CoDetail
    {
        public string CoId { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string PurchaseOrderId { get; set; }
        public string Requester { get; set; }
        public string Vendor { get; set; }
        public string CoReason { get; set; }
        public string DateRequested { get; set; }
        public string RequiredOnSite { get; set; }
        public decimal TotalCoAmount { get; set; }
        public List<PoRequestItemDto> Items { get; set; } = new();
        public List<PoBackChargeDto> BackCharges { get; set; } = new();
    }
}
