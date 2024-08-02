﻿using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.UserNotice;

public class UserNoticeGetAllPaginatedReq
{
    [Required] [Range(1, int.MaxValue)] public int Page { get; set; }
    [Required] [Range(10, 100)] public int Size { get; set; }
    public string SearchString { get; set; }
}