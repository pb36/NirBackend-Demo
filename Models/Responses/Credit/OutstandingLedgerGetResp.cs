using System;

namespace NirvedBackend.Models.Responses.Credit;

public class OutstandingLedgerGetResp
{
    public int OutstandingLedgerId { get; set; }
    public decimal Amount { get; set; }
    public decimal Opening { get; set; }
    public decimal Closing { get; set; }
    public string TransactionType { get; set; }
    public int TransactionTypeId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Remark { get; set; }
}