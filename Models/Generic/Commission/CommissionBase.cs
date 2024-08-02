using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Generic.Commission;

public class CommissionBase
{
    [Required][Range(0,1000000)]public int From { get; set; }
    [Required][Range(1,1000000)]public int To { get; set; }
    [Required][Range(0.1,20)]public decimal Percentage { get; set; }
}