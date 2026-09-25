using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class ProjectResource
{
    public int Id { get; set; }

    public string? JobId { get; set; }

    public string? Branch { get; set; }

    public int? UserId { get; set; }
}
