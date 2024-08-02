using System.Collections.Generic;

namespace NirvedBackend.Models.Responses.Biller;

public class BillerRetailerGetAllResp
{
    public List<BillerRetailerBaseGetResp> Billers { get; set; }
    public int DefaultBillerId { get; set; }
}

public class BillerRetailerBaseGetResp
{
    public int BillerId { get; set; }
    public string Name { get; set; }
    public BillerConfigGetResp BillerConfig { get; set; }
}