using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.DropDown;

public class DistributorDropDownListResp
{
    public List<DistributorDropDownBaseResp> Distributors { get; set; }
}

public class DistributorDropDownBaseResp
{
    public int UserId { get; set; }
    public string DistributorName { get; set; }
}