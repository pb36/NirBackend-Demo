using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.TopUp;

public class TopUpGetAllPaginatedResp : PaginatedResponse
{
    public List<TopUpGetResp> TopUps { get; set; }
}