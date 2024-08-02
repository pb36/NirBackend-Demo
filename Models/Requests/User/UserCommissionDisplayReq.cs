using System;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.User;

public class UserCommissionDisplayReq
{
    [RequiredEnum] public PaginatedDateRange DateRange { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}