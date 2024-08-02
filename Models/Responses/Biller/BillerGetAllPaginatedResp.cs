using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.Biller;

public class BillerGetAllPaginatedResp : PaginatedResponse
{
    public List<BillerGetResp> Billers { get; set; }
}