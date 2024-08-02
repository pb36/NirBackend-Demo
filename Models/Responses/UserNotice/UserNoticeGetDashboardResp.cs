using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.UserNotice;

public class UserNoticeGetDashboardResp
{
    public List<UserNoticeGetResp> UserNotices { get; set; }
}