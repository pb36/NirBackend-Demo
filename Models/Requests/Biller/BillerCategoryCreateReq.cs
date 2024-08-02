using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Biller;

public class BillerCategoryCreateReq
{
    [StringLength(50)] public string BillerName { get; set; }
}