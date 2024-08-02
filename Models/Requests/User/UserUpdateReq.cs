using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.User;

public class UserUpdateReq
{
    [Required] public int UserId { get; set; }
    [Required] [StringLength(255)] public string Email { get; set; }

    [Required]
    [MinLength(3)]
    [StringLength(100)]
    public string Name { get; set; }

    [Required] [StringLength(10)] public string Mobile { get; set; }
}