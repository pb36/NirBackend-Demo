using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class OutstandingLedger
{
    public int OutstandingLedgerId { get; set; }

    public int OutstandingId { get; set; }

    public decimal Opening { get; set; }

    public decimal Closing { get; set; }

    public decimal Amount { get; set; }

    public int Type { get; set; }

    public DateTime CreatedOn { get; set; }

    public string Remark { get; set; }

    public virtual Outstanding Outstanding { get; set; }
}
