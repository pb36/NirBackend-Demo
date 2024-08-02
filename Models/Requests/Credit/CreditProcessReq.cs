using System.ComponentModel.DataAnnotations;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.Credit;

public class CreditProcessReq
{
    [Required]public int CreditRequestId { get; set; }
    [RequiredEnum]public CreditRequestStatus Status { get; set; }
    [StringLength(90)]public string Remark { get; set; }
}