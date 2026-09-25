using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class AccountingQueue
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public string Submitter { get; set; } = null!;

    public DateOnly? DateModified { get; set; }
}
