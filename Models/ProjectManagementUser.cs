using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ProjectManagementUser
{
    public int Id { get; set; }

    public int JobNumber { get; set; }

    public string EmailId { get; set; } = null!;

    public int UserRole { get; set; }
    public string UserRoleName { get; set; }

    public DateTime DateAdded { get; set; }

    public bool? IsDisabled { get; set; }
    public bool IsNew { get; set; }

    public string? SelectedUserId { get; set; }
    public int? SelectedRoleId { get; set; }

    // 🔹 Child rows for the nested grid
    public List<ProjectManagementUser> Assignments { get; set; } = new();

}
