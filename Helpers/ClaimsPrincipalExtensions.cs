using System;
using System.Linq;
using System.Security.Claims;
using NirvedBackend.Models.Generic;

namespace NirvedBackend.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        return Convert.ToInt32(user.Claims.First(c => c.Type == "id").Value);
    }
    
    public static UserType GetRole(this ClaimsPrincipal user)
    {
        return (UserType) Enum.Parse(typeof(UserType), user.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }
    
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.GetRole() == UserType.Admin;
    }
}