using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ErrorLog
{
    public int ErrorLogId { get; set; }

    public string ErrorMessage { get; set; } = null!;

    public string? ErrorProcedure { get; set; }

    public int? ErrorLine { get; set; }

    public int? ErrorSeverity { get; set; }

    public int? ErrorState { get; set; }

    public DateTime DateOccurred { get; set; }
}
