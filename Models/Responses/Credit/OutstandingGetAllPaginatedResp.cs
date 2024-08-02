using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.Credit;

public class OutstandingGetAllPaginatedResp : PaginatedResponse
{
    public List<OutstandingGetResp> Outstanding { get; set; }
}