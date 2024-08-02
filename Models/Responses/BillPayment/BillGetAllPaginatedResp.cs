using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.BillPayment;

public class BillGetAllPaginatedResp : PaginatedResponse
{
    public List<BillGetResp> Bills { get; set; }
}