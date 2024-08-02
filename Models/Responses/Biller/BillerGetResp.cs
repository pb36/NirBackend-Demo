using System;

namespace NirvedBackend.Models.Responses.Biller;

public class BillerGetResp
{
    public int BillerId { get; set; }
    public string Name { get; set; }
    public string BillerCategory { get; set; }
    public int BillerCategoryId { get; set; }
    public string Code { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
}