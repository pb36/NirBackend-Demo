using System.ComponentModel.DataAnnotations;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.TopUp;

public class TopUpProcessReq
{
    [Required]public int TopUpRequestId { get; set; }
    [RequiredEnum]public TopUpRequestStatus Status { get; set; }
    [StringLength(90)]public string Remark { get; set; }
}