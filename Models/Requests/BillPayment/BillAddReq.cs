using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NirvedBackend.Helpers;

namespace NirvedBackend.Models.Requests.BillPayment;

public class BillAddReq
{
    [MaxListLength(20, ErrorMessage = "Maximum 20 bills can be added at once")]
    [RequiredList(ErrorMessage = "At least one bill is required")] 
    public List<BillAddBaseReq> Bills { get; set; }
    
    [Required] public string Timestamp { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public string Nonce { get; set; }
}

public class BillAddBaseReq
{
    [Required] public int BillerId { get; set; }
    [Required] public string ServiceNumber { get; set; }
    [Required] public decimal BillAmount { get; set; }
    public string ExtraInfo { get; set; }
    [Required] public string CustomerName { get; set; }
    [Required] public DateOnly DueDate { get; set; }
    
}