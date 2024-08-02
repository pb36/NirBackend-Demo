namespace NirvedBackend.Models.Requests.Excel;

public class UserGetAllExcelReq
{
    public int? ParentId { get; set; }
    public string SearchString { get; set; }
}