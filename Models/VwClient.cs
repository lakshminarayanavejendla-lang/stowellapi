using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwClient
{
    public string Clientname { get; set; } = null!;

    public long Id { get; set; }

    public string Shtnme { get; set; } = null!;

    public string Address1 { get; set; } = null!;

    public string Address2 { get; set; } = null!;

    public string City { get; set; } = null!;

    public string State { get; set; } = null!;

    public string Zip { get; set; } = null!;
}
