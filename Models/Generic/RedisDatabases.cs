namespace NirvedBackend.Models.Generic;

public enum RedisDatabases
{
    UserSession=0,
    UserLoginOtp=1,
    UserForgotPassword=2,
    ResponseCache=3,
    HmacNonce=4,
}