using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NirvedBackend.Models.Generic.Commission;

namespace NirvedBackend.Models.Requests.User;

public class UserCommissionReq
{
    [Required]public List<UserCommissionUpdateBase> Commissions { get; set; }
}

public class UserCommissionUpdateBase
{
    [Required][Range(1,int.MaxValue)]public int CommissionPercentageId { get; set; }
    [Required][Length(1,5)] public List<CommissionBase> PercentageJson { get; set; }
    [Required][Range(0.1,20)]public decimal DefaultPercentage { get; set; }
    
}