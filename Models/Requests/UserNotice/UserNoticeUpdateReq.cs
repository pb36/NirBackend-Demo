using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.UserNotice;

public class UserNoticeUpdateReq : UserNoticeCreateReq
{
   [Required][Range(1,int.MaxValue)] public int UserNoticeId { get; set; }
}