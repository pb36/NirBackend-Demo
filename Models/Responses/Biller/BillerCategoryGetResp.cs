using System;

namespace NirvedBackend.Models.Responses.Biller;

public class BillerCategoryGetResp
{
    public int BillerCategoryId { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
}