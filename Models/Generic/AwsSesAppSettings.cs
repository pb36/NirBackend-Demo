namespace NirvedBackend.Models.Generic;

public class AwsSesAppSettings
{
    public string Name { get; set; }
    public string From { get; set; }
    public string ReplyTo { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
}