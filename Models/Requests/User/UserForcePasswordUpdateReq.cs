using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.User;

public class UserForcePasswordUpdateReq
{
    [Required] public int UserId { get; set; }
    [Required] [StringLength(50)] public string NewPassword { get; set; }
}