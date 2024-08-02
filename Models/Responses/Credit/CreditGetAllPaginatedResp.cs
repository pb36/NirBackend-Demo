using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.Credit;

public class CreditGetAllPaginatedResp : PaginatedResponse
{
    public List<CreditGetResp> CreditRequests { get; set; }
}