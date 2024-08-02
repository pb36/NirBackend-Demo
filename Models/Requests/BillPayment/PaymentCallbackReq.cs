namespace NirvedBackend.Models.Requests.BillPayment;

public class PaymentCallbackReq
{ 
    public string ClientRefNo { get; set; }
    public string Status { get; set; }
    public string StatusMsg { get; set; }
    public string TrnID { get; set; }
    public string OprID { get; set; }
    public decimal DP { get; set; }
    public decimal DR { get; set; }
    public decimal BAL { get; set; }
}