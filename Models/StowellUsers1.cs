using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class StowellUsers1
{
    public int Id { get; set; }

    public string? DisplayName { get; set; }

    public string? UserName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Licenses { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }
}
