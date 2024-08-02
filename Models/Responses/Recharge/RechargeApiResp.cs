namespace NirvedBackend.Models.Responses.Recharge;

public class RechargeApiResp
{
    public string STATUSCODE { get; set; }
    public string STATUSMSG { get; set; }
    public string REFNO { get; set; }
    public int TRNID { get; set; }
    public int TRNSTATUS { get; set; }
    public string TRNSTATUSDESC { get; set; }
    public string OPRID { get; set; }
    public decimal BAL { get; set; }
    public decimal BALANCE { get; set; }
}