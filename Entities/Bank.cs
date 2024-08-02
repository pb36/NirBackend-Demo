using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class Bank
{
    public int BankId { get; set; }

    public string Name { get; set; }

    public string AccountName { get; set; }

    public int Type { get; set; }

    public string AccountNumber { get; set; }

    public string BranchName { get; set; }

    public string Address { get; set; }

    public string IfscCode { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<TopUpRequest> TopUpRequests { get; set; } = new List<TopUpRequest>();
}
