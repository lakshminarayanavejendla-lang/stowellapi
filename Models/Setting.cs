using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class Setting
{
    public int Id { get; set; }

    public string FileNumber { get; set; } = null!;
}
