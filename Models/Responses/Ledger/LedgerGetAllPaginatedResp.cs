using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.Ledger;

public class LedgerGetAllPaginatedResp : PaginatedResponse
{
    public List<LedgerGetResp> Ledgers { get; set; }
}