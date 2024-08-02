using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Credit;

public class OutstandingClearReq
{
    [Required,Range(1,int.MaxValue)] public int OutstandingId { get; set; }
    [Required,Range(1,5000000)] public decimal Amount { get; set; }
    [Required][StringLength(90)] public string Remark { get; set; }
}