using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class VwBudgetPayment
{
    public long Recnum { get; set; }

    public decimal? Totalbudget { get; set; }

    public decimal? AmountPaid { get; set; }

    public decimal? Balance { get; set; }
}
