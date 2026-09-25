using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ProjectManagementOld
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? UserName { get; set; }

    public int? JobNumber { get; set; }
}
