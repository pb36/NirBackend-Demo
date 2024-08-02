using System;
using Newtonsoft.Json;

namespace NirvedBackend.Models.Responses.BillPayment;

public class BillFetchResp
{
    public bool Success { get; set; }
    public decimal BillAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public string CustomerName { get; set; }
    public DateOnly BillDate { get; set; }
    //Json feild name is code
    [JsonProperty("code")]
    public string ErrorCode { get; set; }
    //Json feild name is message
    [JsonProperty("text")]
    public string ErrorMessage { get; set; }
}