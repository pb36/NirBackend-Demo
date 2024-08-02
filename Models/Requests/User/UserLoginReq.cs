using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.User;

public class UserLoginReq
{
    [Required] public string Username { get; set; }
    [Required] public string Password { get; set; }
}