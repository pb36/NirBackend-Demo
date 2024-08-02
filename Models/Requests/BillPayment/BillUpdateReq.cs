using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.BillPayment;

public class BillUpdateReq
{
    [Required][Range(1,int.MaxValue)] public int BillId { get; set; }
    [Required][StringLength(90)] public string Reason { get; set; }
    [StringLength(40)]public string PaymentRef { get; set; }
}