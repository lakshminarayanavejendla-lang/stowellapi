namespace StowellCoAPI.Models
{
    public class Bids
    {
        public string BidId { get; set; }
        public string BidName { get; set; }
        public string BidManager { get; set; }
        public string Bidder { get; set; }
        public string Status { get; set; }
    }
    public class BidQueue
    {
        public List<Bids> CurrentBids { get; set; }
        public List<Bids> ClosedBids { get; set; }
    }
    // FIX (2026-09-15) - same bug class as UpdateBidDetailsRequest (see that class's header
    // comment for the full explanation): these were all non-nullable string, so
    // [ApiController]'s implicit-required validation silently 400'd CreateNewBid the moment a
    // caller left any of them blank (all of them except BidName/CreatedBy are genuinely optional
    // on the New Bid form). This is almost certainly why a previewed-but-never-actually-created
    // bid ID (e.g. the New Bid form's live "next ID" preview) can end up referenced in the UI
    // with nothing behind it in BidRecords - the create silently failed.
    public class CreateBidRequest
    {
        public string? BidId { get; set; }
        public string? Department { get; set; }
        public string? Division { get; set; }
        public string BidName { get; set; }
        public string? Address1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string CreatedBy { get; set; }
    }
    public class BidRecords
    {
        public int InternalBidID { get; set; }
        public string DisplayBidID { get; set; }
        public bool IsProject { get; set; }
        public string Department { get; set; }
        public string Division { get; set; }
        public string BidName { get; set; }
        public string BidPhase { get; set; }
        public string BidStatus { get; set; }
        public string Jobsite { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string SalesTaxDistrict { get; set; }
        public string ClientName { get; set; }

        public string GC_Address1 { get; set; }
        public string GC_Address2 { get; set; }
        public string GC_City { get; set; }
        public string GC_State { get; set; }
        public string GC_ZipCode { get; set; }
        public string GC_SalesTaxDistrict { get; set; }

        public string DocumentFolderPath { get; set; }
        public bool HasBidDocumentLibrary { get; set; }

        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public string InsuranceType { get; set; }
        public string Bonded { get; set; }
        public string TaxExempt { get; set; }

        public string InvoiceSubmittal { get; set; }
        public string CertifiedPayroll { get; set; }

        public DateTime? NetTermsDate { get; set; }

        public decimal RetainagePercentage { get; set; }
    }
    public class BidAmounts
    {
        public int BidAmountID { get; set; }

        public int InternalBidID { get; set; }

        public DateTime? BidDate { get; set; }

        public decimal BidAmount { get; set; }

        public string Status { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
    // NEW - Notes table on Bid Overview's Overview tab (see this repo's bid-note.model.ts header
    // comment - not a Blazor port, no Notes feature exists there; shape mirrors BidAmounts, the
    // sibling sub-grid on the same tab). Backed by dbo.BidNotes / sp_GetBidNotes / sp_AddBidNote.
    public class BidNote
    {
        public int NoteId { get; set; }

        public int InternalBidID { get; set; }

        public DateTime? NoteDate { get; set; }

        public string Note { get; set; }

        public string EnteredBy { get; set; }
    }
    public class BidAccessSummary
    {
        public int BidAccessID { get; set; }

        public string EmployeeName { get; set; }

        public string RoleName { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
    // FIX (2026-09-15) - every field below except InternalBidID/ModifiedBy was plain non-nullable
    // string. With <Nullable>enable</Nullable> project-wide, [ApiController]'s automatic model
    // validation treats a non-nullable reference-type property as implicitly required - so ANY
    // bid with even one blank optional field (GC_Address2, SalesTaxDistrict, etc. - common) got
    // its ENTIRE save rejected with 400 Bad Request before the request body ever reached this
    // action or sp_UpdateBidDetails's own (correct) COALESCE(@Field, Field) partial-update logic.
    // This is why "Bid Status did not Save" - the save silently 400'd regardless of which field
    // the user actually changed. Marked nullable to match what the stored proc has always
    // expected: any of these may legitimately be omitted.
    public class UpdateBidDetailsRequest
    {
        public int InternalBidID { get; set; }

        // Bid Overview
        public string? Department { get; set; }
        public string? Division { get; set; }
        public string? BidName { get; set; }
        public string? BidPhase { get; set; }
        public string? BidStatus { get; set; }

        // Property Address
        public string? Jobsite { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? SalesTaxDistrict { get; set; }

        // General Contractor
        public string? ClientName { get; set; }
        public string? GC_Address1 { get; set; }
        public string? GC_Address2 { get; set; }
        public string? GC_City { get; set; }
        public string? GC_State { get; set; }
        public string? GC_ZipCode { get; set; }
        public string? GC_SalesTaxDistrict { get; set; }

        // Project Information
        public string? InsuranceType { get; set; }
        public string? Bonded { get; set; }
        public string? TaxExempt { get; set; }
        public string? InvoiceSubmittal { get; set; }
        public string? CertifiedPayroll { get; set; }

        public DateTime? NetTermsDate { get; set; }

        public decimal? RetainagePercentage { get; set; }

        // Audit
        public string ModifiedBy { get; set; }
    }
    public class AddBidAmountRequest
    {
        public int InternalBidID { get; set; }

        public DateTime BidDate { get; set; }

        public decimal BidAmount { get; set; }

        public string Status { get; set; }

        public string ModifiedBy { get; set; }
    }
    public class AddBidNoteRequest
    {
        public int InternalBidID { get; set; }

        public DateTime NoteDate { get; set; }

        public string Note { get; set; }

        public string EnteredBy { get; set; }
    }
    public class UpdateBidAmountRequest
    {
        public int BidAmountID { get; set; }

        public DateTime? BidDate { get; set; }

        public decimal? BidAmount { get; set; }

        public string Status { get; set; }

        public string ModifiedBy { get; set; }
    }
    public class DeleteBidAmountRequest
    {
        public int BidAmountID { get; set; }

        public string ModifiedBy { get; set; }
    }
    public class AddBidAccessRequest
    {
        public int InternalBidID { get; set; }

        public string RoleName { get; set; }

        public string EmployeeName { get; set; }

        public string ModifiedBy { get; set; }
    }
    public class RemoveBidAccessRequest
    {
        public int BidAccessID { get; set; }

        public string ModifiedBy { get; set; }
    }
    public class ConvertBidToProjectRequest
    {
        public int InternalBidID { get; set; }

        public string ModifiedBy { get; set; }
    }
    public class CloseBidAsLostRequest
    {
        public int InternalBidID { get; set; }

        // Optional (2026-09-24): a missing value made model validation answer 400, so Bid Lost never ran.
        public string? ModifiedBy { get; set; }
    }
    public class BidStatus
    {
        public string Uid { get; set; }
        public string Name { get; set; }
    }
}
