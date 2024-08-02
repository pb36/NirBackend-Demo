using System.ComponentModel.DataAnnotations;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.Bank;

public class BankCreateReq
{
    [Required] [StringLength(100)] public string BankName { get; set; }
    [Required] [StringLength(100)] public string AccountName { get; set; }
    [RequiredEnum] public BankType Type { get; set; }
    [Required] [StringLength(20)] public string AccountNumber { get; set; }
    [Required] [StringLength(30)] public string BranchName { get; set; }
    [Required] [StringLength(100)] public string Address { get; set; }
    [Required] [StringLength(11)] public string IfscCode { get; set; }
}