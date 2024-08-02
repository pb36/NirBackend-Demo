using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class Bill
{
    public int BillId { get; set; }

    public int BillerId { get; set; }

    public string DisplayId { get; set; }

    public string ServiceNumber { get; set; }

    public decimal Amount { get; set; }

    public string CustomerName { get; set; }

    public DateOnly DueDate { get; set; }

    public string PaymentRef { get; set; }

    public bool CommissionGiven { get; set; }

    public string ReferenceNumber { get; set; }

    public int Status { get; set; }

    public string ExtraInfo { get; set; }

    public string Remark { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateOnly CreatedOnDate { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public DateOnly? UpdatedOnDate { get; set; }

    public virtual Biller Biller { get; set; }

    public virtual User CreatedByNavigation { get; set; }

    public virtual ICollection<Ledger> Ledgers { get; set; } = new List<Ledger>();
}
