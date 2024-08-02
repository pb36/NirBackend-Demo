namespace NirvedBackend.Models.Responses.User;

public class UserDashboardResp
{
    public decimal TodayAmount { get; set; }
    public decimal MonthAmount { get; set; }
    public int TodayBill { get; set; }
    public int MonthBill { get; set; }
    public decimal SelfOutstanding { get; set; }
    public decimal Outstanding { get; set; }
}