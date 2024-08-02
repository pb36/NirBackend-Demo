using Ganss.Excel;

namespace NirvedBackend.Models.Responses.Excel;

public class OutstandingGetAllBaseExcelResp
{
    [Column(1,"Outstanding Id")] public int Id { get; set; }
    [Column(2,"Name")] public string Name { get; set; }
    [Column(4,"Number")] public string Number { get; set; }
    [Column(3,"Amount")] public decimal Amount { get; set; }
    
}