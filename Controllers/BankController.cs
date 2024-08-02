using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Bank;
using NirvedBackend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class BankController(IBankService bankService) : ControllerBase
{
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("addBank")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> CreateBankAsync([FromBody] BankCreateReq bankCreateReq)
    {
        var result = await bankService.CreateBankAsync(bankCreateReq);
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("getBank/{bankId:int:min(1)}")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetBankAsync(int bankId)
    {
        var result = await bankService.GetBankAsync(bankId);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpGet("getBanks")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> GetBanksAsync()
    {
        var result = await bankService.GetBanksAsync();
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("updateBank")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> UpdateBankAsync([FromBody] BankUpdateReq bankUpdateReq)
    {
        var result = await bankService.UpdateBankAsync(bankUpdateReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpDelete("deleteBank/{bankId:int:min(1)}")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> DeleteBankAsync(int bankId)
    {
        await bankService.DeleteBankAsync(bankId);
        return Ok(new StatusResp {Message = "Bank deleted successfully"});
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("toggleBank/{bankId:int:min(1)}")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> ToggleBankAsync(int bankId)
    {
        var result = await bankService.ToggleBankAsync(bankId);
        return Ok(result);
    }
    
    [Authorize(Policies.SuperDistributorOrDistributorOrRetailer)]
    [CustomJwtValidate]
    [HttpGet("getActiveBanks")]
    [SwaggerOperation(Summary = "super distributor or distributor or retailer auth")]
    public async Task<IActionResult> GetActiveBanksAsync()
    {
        var result = await bankService.GetActiveBanksAsync();
        return Ok(result);
    }
    
}