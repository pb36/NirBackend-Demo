namespace NirvedBackend.Models.Responses.Credit;

public class OutstandingGetResp
{
    public int OutstandingId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }
    public string Mobile { get; set; }
    public decimal OutstandingAmount { get; set; }
}