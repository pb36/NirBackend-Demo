using System;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.Excel;

public class LedgerGetAllExcelReq
{
    [RequiredEnum] public PaginatedDateRange DateRange { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string SearchString { get; set; }
}