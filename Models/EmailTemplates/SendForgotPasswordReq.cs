namespace NirvedBackend.Models.EmailTemplates;

public class SendForgotPasswordReq
{
    public string Origin { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Guid { get; set; }
}