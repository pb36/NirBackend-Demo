using System;

namespace NirvedBackend.Models.Responses.Ledger;

public class LedgerGetResp
{
    public int LedgerId { get; set; }
    public string Name { get; set; }
    public int? BillId { get; set; }
    public int? TopUpRequestId { get; set; }
    public int? CreditRequestId { get; set; }
    public decimal Opening { get; set; }
    public decimal Closing { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; }
    public int TypeId { get; set; }
    public string Remark { get; set; }
    public DateTime CreatedOn { get; set; }
}
