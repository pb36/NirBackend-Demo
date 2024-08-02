using System;
using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Helpers;

public class RequiredEnum : RequiredAttribute
{
    public override bool IsValid(object value)
    {
        if (value == null) return false;
        var type = value.GetType();
        return type.IsEnum && Enum.IsDefined(type, value);
    }
}