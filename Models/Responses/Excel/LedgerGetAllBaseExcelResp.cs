using System;
using Ganss.Excel;

namespace NirvedBackend.Models.Responses.Excel;

public class LedgerGetAllBaseExcelResp
{
    [Column(1,"Ledger Id")] public int LedgerId { get; set; }
    [Column(2,"Transaction Type")] public string Type { get; set; }
    [Column(3,"Opening Balance")] public decimal Opening { get; set; }
    [Column(4,"Amount")] public decimal Amount { get; set; }
    [Column(5,"Closing Balance")] public decimal Closing { get; set; }
    [Column(6,"Remark")] public string Remark { get; set; }
    [Column(7,"Party Name")] public string Name { get; set; }
    [Column(8,"User Type")] public string UserType { get; set; }
    [Column(9,"Created On")] public DateTime CreatedOn { get; set; }
    [Column(10,"Bill Type")] public string BillType { get; set; }
    [Column(11,"Bill Id")] public string BillId { get; set; }
    [Column(12,"Top Up Request Id")] public int? TopUpRequestId { get; set; }
    [Column(13,"Credit Request Id")] public int? CreditRequestId { get; set; }
}