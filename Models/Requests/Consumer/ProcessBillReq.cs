namespace NirvedBackend.Models.Requests.Consumer;

public class ProcessBillReq
{
    public int BillId { get; set; }
    public decimal Amount { get; set; }
}