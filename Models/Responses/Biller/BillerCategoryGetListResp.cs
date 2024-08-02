using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.Biller;

public class BillerCategoryGetListResp
{
    public List<BillerCategoryGetResp> BillerCategories { get; set; }
}