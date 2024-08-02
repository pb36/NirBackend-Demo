using System;
using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.UserNotice;

public class UserNoticeCreateReq
{
    [Required] [StringLength(50)] public string Title { get; set; }
    [Required] [StringLength(300)] public string Message { get; set; }
    [Required] public DateOnly StartDate { get; set; }
    [Required] public DateOnly EndDate { get; set; }
}