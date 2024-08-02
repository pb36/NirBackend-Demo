using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.DropDown;

public class SuperDistributorDropDownListResp
{
    public List<SuperDistributorDropDownBaseResp> SuperDistributors { get; set; }
}

public class SuperDistributorDropDownBaseResp
{
    public int UserId { get; set; }
    public string SuperDistributorName { get; set; }
}
