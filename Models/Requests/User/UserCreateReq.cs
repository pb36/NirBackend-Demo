using System.ComponentModel.DataAnnotations;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.User;

public class UserCreateReq
{
    // [Required] [StringLength(50)] public string Username { get; set; }
    [Required] [StringLength(255)] public string Email { get; set; }

    [Required]
    [MinLength(3)]
    [StringLength(100)]
    public string Name { get; set; }

    [Required] [StringLength(10)] public string Mobile { get; set; }
    [Required] public string Aadhar { get; set; }
    [Required] public string AadharExt { get; set; }
    [Required] public string Pan { get; set; }
    [Required] public string PanExt { get; set; }
    [Required] [StringLength(255)] public string Address { get; set; }
    [Required] public int CityId { get; set; }
    [Required] [StringLength(50)] public string Password { get; set; }
    [RequiredEnum] public UserType UserType { get; set; }
    [Required] public int ParentId { get; set; }
}