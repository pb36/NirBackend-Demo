using System;

namespace NirvedBackend.Models.Responses.Credit;

public class CreditGetResp
{
    public int CreditRequestId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public int StatusId { get; set; }
    public string RemitterName { get; set; }
    public string Remark { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool CurrentUser { get; set; }
}