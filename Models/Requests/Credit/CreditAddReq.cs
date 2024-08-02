using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Credit;

public class CreditAddReq
{
    [Required,Range(1,1000000)] public decimal Amount { get; set; }
}