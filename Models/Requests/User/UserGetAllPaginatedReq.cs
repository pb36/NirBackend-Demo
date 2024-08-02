using System.ComponentModel.DataAnnotations;

namespace NirvedBackend.Models.Requests.User;

public class UserGetAllPaginatedReq
{
    [Required] [Range(1, int.MaxValue)] public int Page { get; set; }
    [Required] [Range(10, 100)] public int Size { get; set; }
    public int? ParentId { get; set; }
    public string SearchString { get; set; }
}