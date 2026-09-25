using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class SageJob
{
    public int Id { get; set; }

    public string JobNumber { get; set; } = null!;

    public DateOnly? DateCreated { get; set; }

    public string? UserId { get; set; }
}
