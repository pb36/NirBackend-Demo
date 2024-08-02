using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class BillerInfo
{
    public int BillerInfo1 { get; set; }

    public string FieldsData { get; set; }

    public int? City { get; set; }

    public bool Fetching { get; set; }

    public int BillerId { get; set; }

    public virtual Biller Biller { get; set; }

    public virtual City CityNavigation { get; set; }
}
