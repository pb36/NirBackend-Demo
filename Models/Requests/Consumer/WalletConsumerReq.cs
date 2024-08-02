using NirvedBackend.Models.Generic;

namespace NirvedBackend.Models.Requests.Consumer;

public class WalletConsumerReq
{
    public WalletTransactionType TransactionType { get; set; }
    public string Data { get; set; }
}