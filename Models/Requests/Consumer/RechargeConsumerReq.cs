namespace NirvedBackend.Models.Requests.Consumer;

public class RechargeConsumerReq
{
    public string MobileNumber { get; set; }
    public string OperatorCode { get; set; }
    public string RefId { get; set; }
    public int Amount { get; set; }
}