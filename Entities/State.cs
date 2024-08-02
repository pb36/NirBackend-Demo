using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class State
{
    public int StateId { get; set; }

    public string Name { get; set; }

    public virtual ICollection<City> Cities { get; set; } = new List<City>();
}
