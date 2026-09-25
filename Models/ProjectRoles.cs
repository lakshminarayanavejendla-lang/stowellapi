namespace StowellCoAPI.Models
{
    public partial class ProjectRoles
    {
        public int Id { get; set; }
        public int RoleId { get; set; }

        public string? RoleName { get; set; }

        public string? Description { get; set; }
        public bool? IsActive { get; set; }

        public DateTime? CreatedDate { get; set; }

    }
}
