﻿namespace NirvedBackend.Models.Generic;

public class PaginatedResponse
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public int PageCount { get; set; }
}