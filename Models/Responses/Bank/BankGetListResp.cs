using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.Bank;

public class BankGetListResp
{
    public List<BankGetResp> Banks { get; set; }
}