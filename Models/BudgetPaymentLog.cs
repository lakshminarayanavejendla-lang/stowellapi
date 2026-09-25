using System;
using System.Collections.Generic;

namespace StowellCoAPI.Models;

public partial class BudgetPaymentLog
{
    public int LogId { get; set; }

    public string Operation { get; set; } = null!;

    public int BudgetPaymentId { get; set; }

    public string? JobId { get; set; }

    public decimal? AmountPaid { get; set; }

    public string? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public DateTime LogDateTime { get; set; }

    public string LogUser { get; set; } = null!;
}
