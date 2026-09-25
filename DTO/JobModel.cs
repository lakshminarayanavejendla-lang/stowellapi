using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StowellCoAPI.DTO
{
    public class JobModel
    {
        public string JobID { get; set; }
        public string JobName { get; set; }
        public string Division { get; set; }
        public DateTime RequestedDeliveryDate { get; set; }
        public string EstimatedID { get; set; }
        public string JobType { get; set; }
        public DateTime ContractualDeliveryDate { get; set; }
        public DateTime EstimatedDate { get; set; }
        public DateTime ContractDate { get; set; }
        public DateTime ContractReviewCompleteDate { get; set; }
        public string ClientId { get; set; }
        public string CompanyName { get; set; }
        public string ClientContract { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
        public string ContactEmail { get; set; }
        public string InvoiceType { get; set; }
        public string NetTermsDate { get; set; }
        public string RetainagePercent { get; set; }
        public string NTOFilingStatus { get; set; }
        public string DateNeeded { get; set; }
        public string JobSite { get; set; }
        public string ProjectAddress { get; set; }
        public string ProjectAddress2 { get; set; }
        public string ProjectCity { get; set; }
        public string ProjectState { get; set; }
        public string ProjectZip { get; set; }
        public string ProjectPhone { get; set; }
        public string ProjectEmail { get; set; }
        public string GeneralContractor { get; set; }
        public string ContractorPM { get; set; }
        public string FolderLocation { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByEmail { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedByEmail { get; set; }
    }

}
