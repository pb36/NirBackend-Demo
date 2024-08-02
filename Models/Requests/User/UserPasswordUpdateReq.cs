using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.User;

public class UserPasswordUpdateReq
{
    [Required] public string CurrentPassword { get; set; }
    [Required] [StringLength(50)] public string NewPassword { get; set; }
}