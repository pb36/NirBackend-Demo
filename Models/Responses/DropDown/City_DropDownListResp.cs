using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.DropDown;

public class CityDropDownListResp
{
   public List<CityDropDownBaseResp> Cities { get; set; }
}

public class CityDropDownBaseResp
{
    public int CityId { get; set; }
    public string CityName { get; set; }
}