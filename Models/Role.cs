using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string? RoleName { get; set; }

    public string? PermissionType { get; set; }
}
