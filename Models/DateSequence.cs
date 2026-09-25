using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class DateSequence
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? Month { get; set; }

    public int? Year { get; set; }
}
