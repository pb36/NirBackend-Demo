using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Config;

public class ConfigUpdateTxnPasswordReq
{
    [Required] public string OldTxnPassword { get; set; }
    [Required] [StringLength(50)] [MinLength(6)] public string NewTxnPassword { get; set; }
}