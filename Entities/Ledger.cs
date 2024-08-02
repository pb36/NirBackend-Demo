using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class Ledger
{
    public int LedgerId { get; set; }

    public int UserId { get; set; }

    public int? BillId { get; set; }

    public int? TopUpRequestId { get; set; }

    public int? CreditRequestId { get; set; }

    public decimal Opening { get; set; }

    public decimal Closing { get; set; }

    public int Type { get; set; }

    public decimal Amount { get; set; }

    public string Remark { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateOnly CreatedOnDate { get; set; }

    public virtual Bill Bill { get; set; }

    public virtual CreditRequest CreditRequest { get; set; }

    public virtual TopUpRequest TopUpRequest { get; set; }

    public virtual User User { get; set; }
}
