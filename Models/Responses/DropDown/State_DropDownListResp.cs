using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.DropDown;

public class StateDropDownListResp
{
    public List<StateDropDownBaseResp> States { get; set; }
}

public class StateDropDownBaseResp
{
    public int StateId { get; set; }
    public string StateName { get; set; }
}
