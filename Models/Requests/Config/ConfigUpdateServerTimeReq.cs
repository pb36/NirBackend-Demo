using System;
using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.Config;

public class ConfigUpdateServerTimeReq
{
    [Required] public TimeOnly StartTime { get; set; }
    [Required] public TimeOnly EndTime { get; set; }
}