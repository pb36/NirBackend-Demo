using Microsoft.AspNetCore.Authorization;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Helpers;

public class Authorize : AuthorizeAttribute
{
    public Authorize(Policies policy=Policies.All)
    {
        if (policy!=Policies.All)
        {
            Policy = policy.ToString();
        }
    }
}