using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Models.Requests.Ledger;
using NirvedBackend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class LedgerController(ILedgerService ledgerService):ControllerBase
{
    [Authorize]
    [CustomJwtValidate]
    [HttpPost]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetAllPaginatedAsync([FromBody] LedgerGetAllPaginatedReq ledgerGetAllPaginatedReq)
    {
        var result = await ledgerService.GetAllPaginatedAsync(ledgerGetAllPaginatedReq, User.GetUserId(), User.GetRole());
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("excel")]
    [SwaggerOperation(Summary = "auth")]
    [EnableRateLimiting("ExcelRateLimitPolicy")]
    public async Task<IActionResult> GetAllExcelAsync([FromBody] LedgerGetAllExcelReq ledgerGetAllExcelReq)
    {
        var result = await ledgerService.GetAllExcelAsync(ledgerGetAllExcelReq, User.GetUserId(), User.GetRole());
        return Ok(result);
    }
}