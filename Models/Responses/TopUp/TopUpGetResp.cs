using System;

namespace NirvedBackend.Models.Responses.TopUp;

public class TopUpGetResp
{
    public int TopUpRequestId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; }
    public int PaymentModeId { get; set; }
    public bool CurrentUser{ get; set; }
    public string BankName { get; set; }
    public int? BankId { get; set; }
    public DateOnly DepositDate { get; set; }
    public string ReferenceNumber { get; set; }
    public string Remark { get; set; }
    public string ImageId { get; set; }
    public string Status { get; set; }
    public string RemitterName { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    
}