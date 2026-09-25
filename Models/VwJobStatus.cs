using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwJobStatus
{
    public int Id { get; set; }

    public string Status { get; set; } = null!;
}
