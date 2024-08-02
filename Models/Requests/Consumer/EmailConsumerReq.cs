using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.Consumer;

public class EmailConsumerReq
{
    public EmailSendType EmailSendType { get; set; }
    public string Data { get; set; }
}