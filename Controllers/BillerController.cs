using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Biller;
using NirvedBackend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class BillerController(IBillerService billerService) : ControllerBase
{
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpGet("get-biller-categories")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> GetBillerCategoriesAsync()
    {
        var result=await billerService.GetBillerCategoriesAsync();
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("create-biller-category")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> CreateBillerCategoryAsync([FromBody] BillerCategoryCreateReq billerCategoryCreateReq)
    {
        var result=await billerService.CreateBillerCategoryAsync(billerCategoryCreateReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPut("update-biller-category")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> UpdateBillerCategoryAsync([FromBody] BillerCategoryUpdateReq billerCategoryUpdateReq)
    {
        var result=await billerService.UpdateBillerCategoryAsync(billerCategoryUpdateReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPut("toggle-biller-category")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> ToggleBillerCategoryAsync([FromQuery] int billerCategoryId)
    {
        var result=await billerService.ToggleBillerCategoryAsync(billerCategoryId);
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("get-biller-categories-dropdown")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetBillerCategoriesDropDownAsync()
    {
        var result=await billerService.GetBillerCategoriesDropDownAsync(User.GetRole());
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("get-all")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> GetAllAsync([FromBody] BillerGetAllPaginatedReq billerGetAllPaginatedReq)
    {
        var result = await billerService.GetAllBillerPaginatedAsync(billerGetAllPaginatedReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("create")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> CreateAsync([FromBody] BillerCreateReq billerCreateReq)
    {
        var result = await billerService.CreateBillerAsync(billerCreateReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPut("update")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> UpdateAsync([FromBody] BillerUpdateReq billerUpdateReq)
    {
        var result = await billerService.UpdateBillerAsync(billerUpdateReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPut("toggle")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> ToggleAsync([FromQuery] int billerId)
    {
        var result = await billerService.ToggleBillerAsync(billerId);
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("get-biller-config")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetBillerConfigAsync([FromQuery] int billerId)
    {
        var result = await billerService.GetBillerConfigAsync(billerId);
        return Ok(result);
    }
    
    [Authorize(Policies.Retailer)]
    [CustomJwtValidate]
    [HttpGet("get-billers-for-category")]
    [SwaggerOperation(Summary = "retailer auth")]
    public async Task<IActionResult> GetAllBillerRetailerAsync([FromQuery,Required,Range(1,int.MaxValue)] int billerCategoryId)
    {
        var result = await billerService.GetAllBillerRetailerAsync(billerCategoryId, User.GetUserId());
        return Ok(result);
    }
    
    
}