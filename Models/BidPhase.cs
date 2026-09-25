using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class BidPhase
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public string? Phase { get; set; }

    public string? Description { get; set; }

    public bool? IsDeleted { get; set; }

    public DateOnly? DeletedDate { get; set; }
}
