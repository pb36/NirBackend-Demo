using System.ComponentModel.DataAnnotations;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.User;

public class UserJournalVoucherReq
{
    public int FromUserId { get; set; } 
    [Required]public int UserId { get; set; }
    
    [RequiredEnum]public TransactionType TransactionType { get; set; }
    [Required,Range(0.1,100000000)]public decimal Amount { get; set; }
    [Required][StringLength(90)]public string Remark { get; set; }
}