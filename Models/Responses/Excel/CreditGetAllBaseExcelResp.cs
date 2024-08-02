using System;
using Ganss.Excel;

namespace NirvedBackend.Models.Responses.Excel;

public class CreditGetAllBaseExcelResp
{
    [Column(1,"Credit Request Id")] public int Id { get; set; }
    [Column(2,"Remitter Name")] public string RemitterName { get; set; }
    [Column(3,"Amount")] public decimal Amount { get; set; }
    [Column(4,"Status")] public string Status { get; set; }
    [Column(5,"Transaction Date")] public DateTime TransactionDate { get; set; }
    [Column(6,"Updated Date")] public DateTime? UpdatedDate { get; set; }
    [Column(7,"Remark")] public string Remark { get; set; }
}