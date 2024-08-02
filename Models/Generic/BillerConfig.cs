using System.Collections.Generic;

namespace NirvedBackend.Models.Generic;

public class BillerConfig
{
    public List<BillerConfigBase> BillerConfigList { get; set; }
}

public class BillerConfigBase
{
    public string Name { get; set; }
    public bool Required { get; set; }
    public string Type { get; set; }
    public string Validation { get; set; }
    public int MinLength { get; set; }
    public int MaxLength { get; set; }
}