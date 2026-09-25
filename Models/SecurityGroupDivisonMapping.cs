using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class SecurityGroupDivisonMapping
{
    public int Id { get; set; }

    public string SecurityGroup { get; set; } = null!;

    public string Division { get; set; } = null!;
}
