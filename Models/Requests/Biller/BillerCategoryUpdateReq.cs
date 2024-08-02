using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Biller;

public class BillerCategoryUpdateReq : BillerCategoryCreateReq
{
    [Required][Range(1, int.MaxValue)] public int BillerCategoryId { get; set; }
}