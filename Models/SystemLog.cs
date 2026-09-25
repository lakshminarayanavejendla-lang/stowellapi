using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class SystemLog
{
    public int Id { get; set; }

    public DateTime? LogDate { get; set; }

    public string? ProcedureName { get; set; }

    public string? Action { get; set; }

    public string? Details { get; set; }

    public string? UserName { get; set; }

    public string? JobId { get; set; }

    public string? CostCode { get; set; }

    public string? ErrorMessage { get; set; }
}
