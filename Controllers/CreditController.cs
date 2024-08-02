using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Credit;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class CreditController(ICreditService creditService) : ControllerBase
{
    [Authorize(Policies.SuperDistributorOrDistributorOrRetailer)]
    [CustomJwtValidate]
    [HttpPost("add-credit-request")]
    [SwaggerOperation(Summary = "super distributor or distributor or retailer auth")]
    public async Task<IActionResult> AddCreditRequestAsync([FromBody] CreditAddReq creditAddReq)
    {
        var result = await creditService.AddCreditRequestAsync(creditAddReq, User.GetUserId());
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("get-all-credit-requests")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetAllCreditRequestsAsync([FromBody] CreditGetAllPaginatedReq creditGetAllPaginatedReq)
    {
        var result = await creditService.GetAllCreditRequestsAsync(creditGetAllPaginatedReq,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("get-all-credit-requests-excel")]
    [SwaggerOperation(Summary = "auth")]
    [EnableRateLimiting("ExcelRateLimitPolicy")]
    public async Task<IActionResult> GetAllCreditRequestsExcelAsync([FromBody] CreditGetAllExcelReq creditGetAllExcelReq)
    {
        var result = await creditService.GetAllExcelAsync(creditGetAllExcelReq,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("get-credit-request")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetCreditRequestAsync([FromQuery] int creditRequestId)
    {
        var result = await creditService.GetCreditRequestAsync(creditRequestId,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("process-credit-request")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> ProcessCreditRequestAsync([FromBody] CreditProcessReq creditProcessReq)
    {
        var result = await creditService.ProcessCreditRequestAsync(creditProcessReq,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("get-all-outstanding")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> GetAllOutstandingRequestsAsync([FromBody] OutstandingGetAllPaginatedReq outstandingGetAllPaginatedReq)
    {
        var result = await creditService.GetAllOutstandingRequestsAsync(outstandingGetAllPaginatedReq,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("get-all-outstanding-excel")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    [EnableRateLimiting("ExcelRateLimitPolicy")]
    public async Task<IActionResult> GetAllOutstandingRequestsExcelAsync([FromBody] OutstandingGetAllExcelReq outstandingGetAllExcelReq)
    {
        var result = await creditService.GetAllExcelAsync(outstandingGetAllExcelReq,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("clear-outstanding")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> ClearOutstandingAsync([FromBody] OutstandingClearReq outstandingClearReq)
    {
        var result = await creditService.ClearOutstandingAsync(outstandingClearReq,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("get-all-outstanding-ledger")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> GetAllOutstandingLedgerAsync([FromBody] OutstandingLedgerGetAllPaginatedReq outstandingLedgerGetAllPaginatedReq)
    {
        var result = await creditService.GetAllOutstandingLedgerAsync(outstandingLedgerGetAllPaginatedReq,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpGet("get-outstanding")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> GetOutstandingAsync([FromQuery] int outstandingId)
    {
        var result = await creditService.GetOutstandingAsync(outstandingId,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.SuperDistributorOrDistributorOrRetailer)]
    [CustomJwtValidate]
    [HttpGet("get-self-outstanding")]
    [SwaggerOperation(Summary = "super distributor or distributor or retailer auth")]
    public async Task<IActionResult> GetSelfOutstandingAsync()
    {
        var result = await creditService.GetSelfOutstandingAsync(User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.SuperDistributorOrDistributorOrRetailer)]
    [CustomJwtValidate]
    [HttpPost("get-all-self-outstanding-ledger")]
    [SwaggerOperation(Summary = "super distributor or distributor or retailer auth")]
    public async Task<IActionResult> GetSelfOutstandingLedgerAsync([FromBody] OutstandingLedgerSelfGetAllPaginatedReq outstandingLedgerSelfGetAllPaginatedReq)
    {
        var result = await creditService.GetSelfOutstandingLedgerAsync(outstandingLedgerSelfGetAllPaginatedReq,User.GetUserId());
        return Ok(result);
    }
    
    
}