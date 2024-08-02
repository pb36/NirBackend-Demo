using System.Collections.Generic;
using NirvedBackend.Models.Generic.Commission;

namespace NirvedBackend.Models.Responses.User;

public class UserCommissionGetResp
{
    public List<UserCommissionBase> Commissions { get; set; }
}

public class UserCommissionBase
{
    public int CommissionPercentageId { get; set; }
    public string Category { get; set; }
    public List<CommissionBase> PercentageJson { get; set; }
    public decimal DefaultPercentage { get; set; }
}