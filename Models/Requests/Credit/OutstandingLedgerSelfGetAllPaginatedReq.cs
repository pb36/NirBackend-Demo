using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Credit;

public class OutstandingLedgerSelfGetAllPaginatedReq
{
    [Required] [Range(1, int.MaxValue)] public int Page { get; set; }
    [Required] [Range(10, 100)] public int Size { get; set; }
}