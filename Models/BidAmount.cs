using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class BidAmount
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public DateOnly? BidDate { get; set; }

    public decimal? BidAmount1 { get; set; }

    public bool? IsDeleted { get; set; }

    public DateOnly? DeletedDate { get; set; }
}
