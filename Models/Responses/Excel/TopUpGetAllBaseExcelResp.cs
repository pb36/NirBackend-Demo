
using System;
using Ganss.Excel;

namespace NirvedBackend.Models.Responses.Excel;

public class TopUpGetAllBaseExcelResp
{
   [Column(1,"TopUp Request Id")] public int Id { get; set; }
   [Column(2,"Payment Mode")] public string PaymentMode { get; set; }
   [Column(3,"Remitter Name")] public string RemitterName { get; set; }
   [Column(4,"Amount")] public decimal Amount { get; set; }
   [Column(5,"Bank")] public string Bank { get; set; }
   [Column(6,"Reference Number")] public string ReferenceNumber { get; set; }
   [Column(7,"Deposit Date")] public DateOnly DepositDate { get; set; }
   [Column(8,"Status")] public string Status { get; set; }
   [Column(9,"Transaction Date")] public DateTime TransactionDate { get; set; }
   [Column(10,"Updated Date")] public DateTime? UpdatedDate { get; set; }
   [Column(11,"Remark")] public string Remark { get; set; }
}