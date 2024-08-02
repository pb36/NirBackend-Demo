using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class City
{
    public int CityId { get; set; }

    public string Name { get; set; }

    public int StateId { get; set; }

    public virtual ICollection<BillerInfo> BillerInfos { get; set; } = new List<BillerInfo>();

    public virtual State State { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
