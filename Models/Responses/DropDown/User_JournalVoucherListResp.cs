using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.DropDown;

public class UserJournalVoucherListResp
{
    public List<UserJournalVoucherBaseResp> Users { get; set; }
}

public class UserJournalVoucherBaseResp
{
    public int UserId { get; set; }
    public string Name { get; set; }
}