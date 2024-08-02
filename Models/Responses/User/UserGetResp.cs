using System;

namespace NirvedBackend.Models.Responses.User;

public class UserGetResp
{
    public int UserId { get; set; }
    public string DisplayId { get; set; }
    public string Username { get; set; }
    public decimal Balance { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Mobile { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public int CityId { get; set; }
    public string State { get; set; }
    public int StateId { get; set; }
    public string UserType { get; set; }
    public int UserTypeId { get; set; }
    public string AdharCardId { get; set; }
    public string PanCardId { get; set; }
    public bool IsActive { get; set; }
    public string ParentUser { get; set; }
    public DateTime CreatedOn { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string LastLoginIp { get; set; }
    public DateTime? LastLoginTime { get; set; }
}