using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.BillPayment;

public class BillFetchReq
{
    [Required,Range(1,int.MaxValue)]public int BillerId { get; set; }
    [Required]public string ServiceNumber { get; set; }
    public string ExtraInfo { get; set; }
}