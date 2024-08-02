using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.UserNotice;
using NirvedBackend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class UserNoticeController(IUserNoticeService userNoticeService) : ControllerBase
{
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("getUserNoticesDashboard")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetUserNoticesDashboardAsync()
    {
        var result = await userNoticeService.GetUserNoticesDashboardAsync();
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("createUserNotice")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> CreateUserNoticeAsync([FromBody] UserNoticeCreateReq userNoticeCreateReq)
    {
        var result = await userNoticeService.CreateUserNoticeAsync(userNoticeCreateReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPut("updateUserNotice")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> UpdateUserNoticeAsync([FromBody] UserNoticeUpdateReq userNoticeUpdateReq)
    {
        var result = await userNoticeService.UpdateUserNoticeAsync(userNoticeUpdateReq);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPut("toggleUserNotice/{userNoticeId:int:min(1)}")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> ToggleUserNoticeAsync(int userNoticeId)
    {
        var result=await userNoticeService.ToggleUserNoticeAsync(userNoticeId);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpDelete("deleteUserNotice/{userNoticeId:int:min(1)}")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> DeleteUserNoticeAsync(int userNoticeId)
    {
        await userNoticeService.DeleteUserNoticeAsync(userNoticeId);
        return Ok();
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("getUserNoticesPaginated")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> GetUserNoticesPaginatedAsync([FromBody] UserNoticeGetAllPaginatedReq userNoticeGetAllPaginatedReq)
    {
        var result = await userNoticeService.GetUserNoticesPaginatedAsync(userNoticeGetAllPaginatedReq);
        return Ok(result);
    }
    
    [Authorize(Policies.SuperDistributorOrDistributorOrRetailer)]
    [CustomJwtValidate]
    [HttpPost("getUserNoticesActivePaginated")]
    [SwaggerOperation(Summary = "super distributor or distributor or retailer auth")]
    public async Task<IActionResult> GetUserNoticesActivePaginatedAsync([FromBody] UserNoticeGetAllPaginatedReq userNoticeGetAllPaginatedReq)
    {
        var result = await userNoticeService.GetUserNoticesActivePaginatedAsync(userNoticeGetAllPaginatedReq);
        return Ok(result);
    }
    
}