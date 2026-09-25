using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ProjectManagement
{
    public int Id { get; set; }

    public int JobNumber { get; set; }

    public string? Pmemail { get; set; }

    public string? Apmemail { get; set; }

    public int? Status { get; set; }

    public string? Branch { get; set; }

    public string? Division { get; set; }
}
