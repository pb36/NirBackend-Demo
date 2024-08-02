using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Excel;

namespace NirvedBackend.Models.Requests.Consumer;

public class UserGetAllExcelConsumerReq : UserGetAllExcelReq
{
    public UserType UserType { get; set; }
    public int CurrentUserId { get; set; }
}