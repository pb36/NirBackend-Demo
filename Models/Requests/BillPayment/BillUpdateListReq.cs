using System.Collections.Generic;
using Ganss.Excel;

namespace NirvedBackend.Models.Requests.BillPayment;

public class BillUpdateListReq
{
    public List<BillUpdateListBase> BillUpdateList { get; set; }
}

public class BillUpdateListBase
{
    [Column(1, "Bill Id")]
    public string BillId { get; set; }
    [Column(2, "Payment Ref")]
    public string PaymentRef { get; set; }
    [Column(3, "Remark")]
    public string Remark { get; set; }
}