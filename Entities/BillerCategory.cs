using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class BillerCategory
{
    public int BillerCategoryId { get; set; }

    public string Name { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual ICollection<Biller> Billers { get; set; } = new List<Biller>();

    public virtual ICollection<CommissionPercentage> CommissionPercentages { get; set; } = new List<CommissionPercentage>();
}
