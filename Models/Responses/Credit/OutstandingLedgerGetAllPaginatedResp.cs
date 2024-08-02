using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.Credit;

public class OutstandingLedgerGetAllPaginatedResp : PaginatedResponse
{
    public List<OutstandingLedgerGetResp> OutstandingLedgers { get; set; }
}