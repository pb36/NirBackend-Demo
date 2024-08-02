using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Biller;

public class BillerUpdateReq : BillerCreateReq
{
    [Required] [Range(1,int.MaxValue)] public int BillerId { get; set; }
}