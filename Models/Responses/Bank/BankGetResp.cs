namespace NirvedBackend.Models.Responses.Bank;

public class BankGetResp
{
    public int BankId { get; set; }
    public string BankName { get; set; }
    public string AccountName { get; set; }
    public string Type { get; set; }
    public int TypeId { get; set; }
    public string AccountNumber { get; set; }
    public string BranchName { get; set; }
    public string Address { get; set; }
    public string IfscCode { get; set; }
    public bool IsActive { get; set; }
}