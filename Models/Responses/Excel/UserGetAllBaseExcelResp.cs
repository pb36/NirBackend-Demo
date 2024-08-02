using Ganss.Excel;

namespace NirvedBackend.Models.Responses.Excel;

public class UserGetAllBaseExcelResp
{
    [Column(1,"User Id")] public string DisplayId { get; set; }
    [Column(2,"Username")] public string Username { get; set; }
    [Column(3,"Balance")] public decimal Balance { get; set; }
    [Column(4,"Name")] public string Name { get; set; }
    [Column(5,"Parent User")] public string ParentUser { get; set; }
    [Column(6,"Mobile")] public string Mobile { get; set; }
    [Column(7,"UserType")] public string UserType { get; set; }
    [Column(8,"IsActive")] public bool IsActive { get; set; }
}