using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Config;
using NirvedBackend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigController(IConfigService configService) : ControllerBase
{
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("update-txn-password")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> UpdateTxnPasswordAsync([FromBody] ConfigUpdateTxnPasswordReq configUpdateTxnPasswordReq)
    {
        var result = await configService.UpdateTxnPasswordAsync(configUpdateTxnPasswordReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("update-server-time")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> UpdateServerTimeAsync([FromBody] ConfigUpdateServerTimeReq configUpdateServerTimeReq)
    {
        var result = await configService.UpdateServerTimeAsync(configUpdateServerTimeReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpGet("get-server-time")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> GetServerTimeAsync()
    {
        var result = await configService.GetServerTimeAsync();
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpGet("get-auto-commission")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> GetAutoCommissionAsync()
    {
        var result = await configService.GetAutoCommissionAsync();
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("toggle-auto-commission")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> ToggleAutoCommissionAsync()
    {
        var result = await configService.ToggleAutoCommissionAsync();
        return Ok(result);
    }
}