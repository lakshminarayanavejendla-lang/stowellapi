using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ProjectManagementLog
{
    public int Id { get; set; }

    public int JobNumber { get; set; }

    public string? Pmemail { get; set; }

    public string? Apmemail { get; set; }

    public int? Status { get; set; }

    public string? Command { get; set; }

    public string? ModifiedUser { get; set; }

    public DateOnly? ModifiedDate { get; set; }
}
