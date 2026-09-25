using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ContractorInfo
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public string? Client { get; set; }

    public string? Address { get; set; }

    public string? Address2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Zip { get; set; }

    public DateOnly? DateModified { get; set; }
}
