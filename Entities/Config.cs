using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class Config
{
    public int ConfigId { get; set; }

    public string Key { get; set; }

    public string Value { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateOnly CreatedOnDate { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public DateOnly? UpdatedOnDate { get; set; }
}
