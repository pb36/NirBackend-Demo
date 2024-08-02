using System;

namespace NirvedBackend.Models.Responses.Config;

public class ConfigGetAutoCommissionResp
{
    public bool IsAutoCommission { get; set; }
    public DateTime LastUpdatedOn { get; set; }
}