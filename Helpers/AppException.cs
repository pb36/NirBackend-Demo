using System;
using System.Globalization;

namespace NirvedBackend.Helpers;

public class AppException : Exception
{
    public AppException()
    {
    }

    public AppException(string error) : base(error)
    {
    }

    public AppException(string message, params object[] args)
        : base(string.Format(CultureInfo.CurrentCulture, message, args))
    {
    }
}