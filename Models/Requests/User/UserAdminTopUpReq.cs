using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.User;

public class UserAdminTopUpReq
{
    [Required,Range(0,100000000)] public decimal Amount { get; set; }
}