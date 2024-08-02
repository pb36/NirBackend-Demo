using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Biller;

public class BillerCreateReq
{
    [Required] [StringLength(100)] public string Name { get; set; }
    [Required] [Range(1,int.MaxValue)] public int BillerCategoryId { get; set; }
    [Required] [StringLength(10)] public string Code { get; set; }
}