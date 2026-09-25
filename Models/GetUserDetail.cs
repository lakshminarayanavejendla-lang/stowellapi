using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class GetUserDetail
{
    public string? DisplayName { get; set; }

    public string? UserName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public int? Roleid { get; set; }

    public string? RoleName { get; set; }
}
