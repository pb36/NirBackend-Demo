using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class Biller
{
    public int BillerId { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public int BillerCategoryId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual BillerCategory BillerCategory { get; set; }

    public virtual BillerInfo BillerInfo { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
}
