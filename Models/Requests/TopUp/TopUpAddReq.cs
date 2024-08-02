using System;
using System.ComponentModel.DataAnnotations;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.TopUp;

public class TopUpAddReq
{
    [Required,Range(1,1000000)] public decimal Amount { get; set; }
    [RequiredEnum] public PaymentMode PaymentMode { get; set; }
    public int? BankId { get; set; }
    [Required] public DateOnly DepositDate { get; set; }
    [Required][StringLength(20)] public string ReferenceNumber { get; set; }
    [Required] public string ImageBase64 { get; set; }
    [Required] public string Extension { get; set; }
}