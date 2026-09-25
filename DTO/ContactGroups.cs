namespace StowellCoAPI.DTO
{
    public class ContactGroups
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
    }
    public class ContactGroupMembers
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string JobTitle { get; set; }
        public string City { get; set; }
        public string Mail { get; set; }
        public string StreetAddress { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Location { get; set; }
        public string Company { get; set; }
        public string Phone { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string? PhotoBase64 { get; set; }

    }
}
