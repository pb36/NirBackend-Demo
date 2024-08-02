using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.Consumer;

public class WhatsappConsumerReq
{
    public WhatsappMessageType WhatsappMessageType { get; set; }
    public string Url { get; set; }
    public string PhoneNumber { get; set; }
    public string Message { get; set; }
}