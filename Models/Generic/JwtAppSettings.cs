﻿namespace NirvedBackend.Models.Generic;

public class JwtAppSettings
{
    public string Secret { get; set; }
    public int ExpirationHours { get; set; }
    public string Audience { get; set; }
    public string Issuer { get; set; }
    public string Subject { get; set; }
}