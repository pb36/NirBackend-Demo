using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Bank;

public class BankUpdateReq : BankCreateReq
{
    [Required]public int BankId { get; set; }
}