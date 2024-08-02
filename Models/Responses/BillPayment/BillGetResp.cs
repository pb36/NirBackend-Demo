using System;

namespace NirvedBackend.Models.Responses.BillPayment;

public class BillGetResp
{
    public int BillId { get; set; }
    public string DisplayId { get; set; }
    public string Biller { get; set; }
    public string ServiceNumber { get; set; }
    public string ExtraInfo { get; set; }
    public string BillAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; }
    public int StatusId { get; set; }
    public string Remark { get; set; }
    public string PaymentRef { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string CustomerName { get; set; }
    public string RetailerName { get; set; }
    public string ReferenceId { get; set; }
}