using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.DropDown;

public class BillerCategoryDropDownListResp
{
    public List<BillerCategoryDropDownBaseResp> BillerCategories { get; set; }
}

public class BillerCategoryDropDownBaseResp
{
    public int BillerCategoryId { get; set; }
    public string Name { get; set; }
}