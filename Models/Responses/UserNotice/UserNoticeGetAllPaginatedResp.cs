using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.UserNotice;

public class UserNoticeGetAllPaginatedResp : PaginatedResponse
{
    public List<UserNoticeGetResp> UserNotices { get; set; }
}