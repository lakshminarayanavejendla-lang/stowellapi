using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ProcedureLog
{
    public int LogId { get; set; }

    public string? ProcedureName { get; set; }

    public DateTime? ExecutionTime { get; set; }

    public string? Status { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Parameters { get; set; }

    public string? UserName { get; set; }

    public string? ClientIp { get; set; }
}
