using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class TopUpRequest
{
    public int TopUpRequestId { get; set; }

    public decimal Amount { get; set; }

    public int PaymentMode { get; set; }

    public int UserId { get; set; }

    public int? BankId { get; set; }

    public string ImageId { get; set; }

    public DateOnly DepositDate { get; set; }

    public string ReferenceNumber { get; set; }

    public string Remark { get; set; }

    public int Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateOnly CreatedOnDate { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public DateOnly? UpdatedOnDate { get; set; }

    public virtual Bank Bank { get; set; }

    public virtual ICollection<Ledger> Ledgers { get; set; } = new List<Ledger>();

    public virtual User User { get; set; }
}
