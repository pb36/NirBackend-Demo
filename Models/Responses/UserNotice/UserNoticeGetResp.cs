using System;

namespace NirvedBackend.Models.Responses.UserNotice;

public class UserNoticeGetResp
{
    public int UserNoticeId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool IsActive { get; set; }
}