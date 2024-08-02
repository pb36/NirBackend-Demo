namespace NirvedBackend.Models.EmailTemplates;

public class SendLoginOtpReq
{
    public string Name { get; set; }
    public string Otp { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string Ip { get; set; }
}