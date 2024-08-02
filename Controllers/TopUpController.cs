using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Models.Requests.TopUp;
using NirvedBackend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class TopUpController(ITopUpService topUpService):ControllerBase
{
    [Authorize(Policies.SuperDistributorOrDistributorOrRetailer)]
    [CustomJwtValidate]
    [HttpPost("add-top-up-request")]
    [SwaggerOperation(Summary = "super distributor or distributor or retailer auth")]
    public async Task<IActionResult> AddTopUpRequestAsync([FromBody] TopUpAddReq topUpAddReq)
    {
        var result = await topUpService.AddTopUpRequestAsync(topUpAddReq,User.GetUserId());
        return Ok(result);
    }
    

    [Authorize]
    [CustomJwtValidate]
    [HttpGet("generate-pre-signed-view-url")]
    [SwaggerOperation(Summary = "super distributor or distributor or retailer auth")]
    public IActionResult GeneratePreSignedViewUrl([FromQuery,Required] string id)
    {
        var result =  topUpService.GeneratePreSignedViewUrl(id);
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("get-all-top-up-requests")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetAllTopUpRequestsAsync([FromBody] TopUpGetAllPaginatedReq topUpGetAllPaginatedReq)
    {
        var result = await topUpService.GetAllTopUpRequestsAsync(topUpGetAllPaginatedReq,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("get-all-top-up-requests-excel")]
    [SwaggerOperation(Summary = "auth")]
    [EnableRateLimiting("ExcelRateLimitPolicy")]
    public async Task<IActionResult> GetAllExcelAsync([FromBody] TopUpGetAllExcelReq topUpGetAllExcelReq)
    {
        var result = await topUpService.GetAllExcelAsync(topUpGetAllExcelReq,User.GetUserId());
        return Ok(result);
    }
    

    [Authorize]
    [CustomJwtValidate]
    [HttpGet("get-top-up-request")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetTopUpRequestAsync([FromQuery,Required] int topUpRequestId)
    {
        var result = await topUpService.GetTopUpRequestAsync(topUpRequestId,User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("process-top-up-request")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> ProcessTopUpRequestAsync([FromBody] TopUpProcessReq topUpProcessReq)
    {
        var result = await topUpService.ProcessTopUpRequestAsync(topUpProcessReq,User.GetUserId());
        return Ok(result);
    }
    
}