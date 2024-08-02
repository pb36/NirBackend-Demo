using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Helpers;

public class MaxListLength(int maxLength) : ValidationAttribute
{
    private int MaxLength { get; set; } = maxLength;

    public override bool IsValid(object value)
    {
        if (value is IList list)
        {
            return list.Count <= MaxLength;
        }
        return false;
    }
}