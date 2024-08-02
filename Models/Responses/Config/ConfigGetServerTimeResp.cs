using System;

namespace NirvedBackend.Models.Responses.Config;

public class ConfigGetServerTimeResp
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}