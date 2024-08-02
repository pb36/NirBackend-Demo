using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Amazon.CloudFront;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Models.Requests.User;
using NirvedBackend.Models.Responses.User;
using NirvedBackend.Services;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(IConnectionMultiplexer redisCache, IUserService userService)
    : ControllerBase
{
    private readonly IDatabase _responseCache = redisCache.GetDatabase((int) RedisDatabases.ResponseCache);

    [HttpPost("login")]
    [SwaggerOperation(Summary = "no auth")]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginReq userLoginReq)
    {
        var result=await userService.LoginAsync(userLoginReq,HttpContext.GetRemoteIpAddress());
        return Ok(result);
    }
    
    [HttpPost("login-otp")]
    [SwaggerOperation(Summary = "no auth")]
    public async Task<IActionResult> LoginOtpAsync([FromBody] UserLoginOtpReq userLoginOtpReq)
    {
        var result=await userService.LoginOtpAsync(userLoginOtpReq,HttpContext.GetRemoteIpAddress());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("addUser")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> AddUserAsync([FromBody] UserCreateReq userCreateReq)
    {
        var result = await userService.CreateUserAsync(userCreateReq,User.GetRole(),User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPut("updateUser")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> UpdateUserAsync([FromBody] UserUpdateReq userUpdateReq)
    {
        var result = await userService.UpdateUserAsync(userUpdateReq,User.GetRole(),User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpGet("AdharCardPresignedUrl")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public IActionResult AdharCardPresignedUrlAsync([FromQuery] string id)
    {
        var result = userService.GeneratePreSignedViewUrl(id,"AadharCard");
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpGet("PanCardPresignedUrl")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public IActionResult PanCardPresignedUrlAsync([FromQuery] string id)
    {
        var result = userService.GeneratePreSignedViewUrl(id,"PanCard");
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpGet("get-otp")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> GetOtpAsync([FromQuery] int userId)
    {
        var result = await userService.GetOtpAsync(userId);
        return Ok(new
        {
            otp = result
        });
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("get-commission-display")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetCommissionDisplayAsync([FromBody] UserCommissionDisplayReq userCommissionDisplayReq)
    {
        var result = await userService.GetCommissionDisplayAsync(userCommissionDisplayReq,User.GetUserId(),User.GetRole());
        return Ok(result);
    }
    
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("get-all")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> GetAllAsync([FromBody] UserGetAllPaginatedReq getAllReq)
    {
        var result = await userService.GetAllPaginatedAsync(getAllReq,User.GetRole(),User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPost("get-all-excel")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    [EnableRateLimiting("ExcelRateLimitPolicy")]
    public async Task<IActionResult> GetAllExcelAsync([FromBody]UserGetAllExcelReq getAllExcelReq)
    {
        var result = await userService.GetAllExcelAsync(getAllExcelReq,User.GetRole(),User.GetUserId());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributor)]
    [CustomJwtValidate]
    [HttpGet("get-distributor-dropdown")]
    [SwaggerOperation(Summary = "admin or super distributor auth")]
    public async Task<IActionResult> GetDistributorDropdownAsync([FromQuery] string searchString,[FromQuery] int parentId)
    {
        var result = await userService.GetDistributorDropDownListAsync(searchString,parentId);
        return Ok(result);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpGet("get-super-distributor-dropdown")]
    [SwaggerOperation(Summary = "admin  auth")]
    public async Task<IActionResult> GetSuperDistributorDropdownAsync([FromQuery] string searchString)
    {
        var result = await userService.GetSuperDistributorDropDownListAsync(searchString);
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpGet("get-user")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> GetUserAsync([FromQuery] int userId)
    {
        var result = await userService.GetAsync(userId,User.GetRole(),User.GetUserId());
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("state-dropdown")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> StateDropDownAsync()
    {
        var result = await userService.GetStateDropDownListAsync();
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("city-dropdown")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> CityDropDownAsync([FromQuery] int stateId)
    {
        var result = await userService.GetCityDropDownListAsync(stateId);
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPut("toggle-user-status")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> ToggleUserStatusAsync([FromQuery] int userId)
    {
        var result = await userService.ToggleActiveAsync(userId,User.GetRole(),User.GetUserId());
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("checkAuth")]
    [SwaggerOperation(Summary = "auth")]
    public IActionResult CheckAuthAsync()
    {
        return Ok();
    }
    
    [HttpPost("forgot-password")]
    [SwaggerOperation(Summary = "no auth")]
    public async Task<IActionResult> ForgotPasswordAsync([FromQuery,Required] string email)
    {
        //check if origin header is present and fetch it
        var origin = Request.Headers.ContainsKey("origin") ? Request.Headers["origin"].ToString() : "http://localhost:3000";
        await userService.ForgotPasswordInitRequestAsync(email,origin);
        return Ok();
    }
    
    [HttpPost("reset-password")]
    [SwaggerOperation(Summary = "no auth")]
    public async Task<IActionResult> ResetPasswordAsync([FromQuery,Required] string token,[FromQuery,Required] string password)
    {
        await userService.ResetPasswordAsync(token,password);
        return Ok();
    }
    
    [HttpGet("check-reset-password-token")]
    [SwaggerOperation(Summary = "no auth")]
    public async Task<IActionResult> CheckForgotPasswordTokenAsync([FromQuery,Required] string token)
    {
        await userService.CheckForgotPasswordTokenAsync(token);
        return Ok();
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPut("update-password")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> UpdatePasswordAsync([FromBody] UserPasswordUpdateReq userPasswordUpdateReq)
    {
        await userService.PasswordChangeAsync(userPasswordUpdateReq,User.GetUserId());
        return Ok();
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPut("force-update-password")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> ForceUpdatePasswordAsync([FromBody] UserForcePasswordUpdateReq userForcePasswordUpdateReq)
    {
        await userService.ForcePasswordChangeAsync(userForcePasswordUpdateReq,User.GetUserId(),User.GetRole());
        return Ok();
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("get-user-profile")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetUserProfileAsync()
    {
        var result = await userService.GetUserInfoAsync(User.GetUserId());
        return Ok(result);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> LogoutAsync()
    {
        await userService.LogoutAsync(User.GetUserId());
        return Ok();
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("get-user-commissions")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetUserCommissionsAsync([FromQuery,Required] int userId)
    {
        var result = await userService.GetCommissionAsync(userId,User.GetUserId(),User.GetRole());
        return Ok(result);
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPut("update-user-commissions")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> UpdateUserCommissionsAsync([FromQuery,Required] int userId,[FromBody] UserCommissionReq userCommissionReq)
    {
        await userService.UpdateCommissionAsync(userCommissionReq,userId,User.GetUserId(),User.GetRole());
        return Ok();
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPut("admin-top-up")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> AdminTopUpAsync([FromBody] UserAdminTopUpReq userAdminTopUpReq)
    {
        return Ok(await userService.AdminTopUpAsync(userAdminTopUpReq));
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpPut("journal-voucher")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> JournalVoucherAsync([FromBody] UserJournalVoucherReq userJournalVoucherReq)
    {
        userJournalVoucherReq.FromUserId = User.GetUserId();
        return Ok(await userService.JournalVoucherAsync(userJournalVoucherReq));
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("get-user-balance")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetUserBalanceAsync()
    {
        var userId = User.GetUserId();
        var key = ResponseCaches.UserBalance+userId;
        if (_responseCache.KeyExists(key))
            return Ok(await _responseCache.StringGetAsync(key));
        else
        {
            var balance = await userService.GetBalanceAsync(userId);
            await _responseCache.StringSetAsync(key,$"{balance.ToString("F",CultureInfo.InvariantCulture)}",TimeSpan.FromMinutes(5));
            return Ok(balance.ToString("F",CultureInfo.InvariantCulture));
        }
    }
    
    [Authorize(Policies.AdminOrSuperDistributorOrDistributor)]
    [CustomJwtValidate]
    [HttpGet("get-journal-voucher-user-dropdown")]
    [SwaggerOperation(Summary = "admin or super distributor or distributor auth")]
    public async Task<IActionResult> GetJournalVoucherListAsync([FromQuery] string searchString)
    {
        return Ok(await userService.GetJournalVoucherListAsync(searchString,User.GetUserId()));
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("get-dashboard-data")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetDashboardDataAsync()
    {
        var userId = User.GetUserId();
        var key = ResponseCaches.UserDashboard+userId;
        if (_responseCache.KeyExists(key))
            return Ok(JsonConvert.DeserializeObject<UserDashboardResp>(await _responseCache.StringGetAsync(key)));
        else
        {
            var result = await userService.GetDashboardAsync(User.GetUserId(),User.GetRole());
            await _responseCache.StringSetAsync(key,JsonConvert.SerializeObject(result),TimeSpan.FromMinutes(15));
            return Ok(result);
        }
    }
    
}