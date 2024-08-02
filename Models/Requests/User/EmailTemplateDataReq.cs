using System.Text.Json.Serialization;

namespace NirvedBackend.Models.Requests.User;

public class EmailTemplateDataReq
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("otp")]
    public string Otp { get; set; }
    [JsonPropertyName("ip")]
    public string Ip { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; }
}