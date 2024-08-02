using System;
using Ganss.Excel;

namespace NirvedBackend.Models.Responses.Excel;

public class BillGetAllBaseExcelResp
{
    [Column(1,"Bill Id")] public string TransactionId { get; set; }
    [Column(2,"Transaction Type")] public string Type { get; set; }
    [Column(3,"Service Number")] public string ServiceNumber { get; set; }
    [Column(4,"Amount")] public decimal Amount { get; set; }
    [Column(5,"Customer Name")] public string CustomerName { get; set; }
    [Column(6,"Extra Info")] public string ExtraInfo { get; set; }
    [Column(7,"Party Name")] public string Name { get; set; }
    [Column(8,"Due Date")] public DateOnly DueDate { get; set; }
    [Column(9,"Transaction Date")] public DateTime TransactionDate { get; set; }
    [Column(10,"Updated Date")] public DateTime? UpdatedDate { get; set; }
    [Column(11,"Status")] public string Status { get; set; }
    [Column(12,"Remark")] public string Remark { get; set; }
    [Column(13,"Payment Ref")] public string PaymentRef { get; set; }
}