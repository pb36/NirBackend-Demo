namespace NirvedBackend.Models.Responses.User;

public class UserLoginResp
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
    public bool IsAdmin { get; set; }
    public string  UserType { get; set; }
    public int UserTypeId { get; set; }
    
    public bool OtpRequired { get; set; }
}