using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class BidEmployee
{
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public string? Role { get; set; }

    public string? Employee { get; set; }

    public bool? IsDeleted { get; set; }

    public DateOnly? DeletedDate { get; set; }
    public int? EmployeeId { get; set; }

}
