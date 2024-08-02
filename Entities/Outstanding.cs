using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class Outstanding
{
    public int OutstandingId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public virtual ICollection<OutstandingLedger> OutstandingLedgers { get; set; } = new List<OutstandingLedger>();

    public virtual User User { get; set; }
}
