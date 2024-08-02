namespace NirvedBackend.Models.Responses.Biller;

public class BillerConfigGetResp
{
    public bool Fetching { get; set; }
    public int? CityId { get; set; }
    public string CityAndState { get; set; }
    public string FieldsData { get; set; }
}