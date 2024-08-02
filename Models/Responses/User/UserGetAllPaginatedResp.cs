using System;
using System.Collections.Generic;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Responses.User;

public class UserGetAllPaginatedResp : PaginatedResponse
{
    public List<UserGetAllBaseResp> Users { get; set; }
}

public class UserGetAllBaseResp
{
    public int UserId { get; set; }
    public string DisplayId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public decimal Balance { get; set; }
    public string Name { get; set; }
    public string Mobile { get; set; }
    public string City { get; set; }
    public string UserType { get; set; }
    public bool IsActive { get; set; }
    public string ParentUser { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public string AdharCardId { get; set; }
    public string PanCardId { get; set; }
}