using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.User;

public class UserLoginOtpReq
{
    [Required] public string Username { get; set; }
    [Required][StringLength(6)] public string Otp { get; set; }
}