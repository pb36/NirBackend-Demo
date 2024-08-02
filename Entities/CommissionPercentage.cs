using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class CommissionPercentage
{
    public int CommissionPercentageId { get; set; }

    public int UserId { get; set; }

    public int BillerCategoryId { get; set; }

    public string PercentageJson { get; set; }

    public decimal Percentage { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public virtual BillerCategory BillerCategory { get; set; }

    public virtual User User { get; set; }
}
