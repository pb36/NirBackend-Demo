using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.BillPayment;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NirvedBackend.Controllers;

[ApiController]
[Route("bills")]
public class BillPaymentController(IBillPaymentService billPaymentService,ILogger<BillPaymentController> logger) : ControllerBase
{
    [Authorize(Policies.Retailer)]
    [CustomJwtValidate]
    [HttpPost("fetchBill")]
    [SwaggerOperation(Summary = "retailer auth")]
    public async Task<IActionResult> FetchBillAsync(BillFetchReq billFetchReq)
    {
        var resp = await billPaymentService.FetchBillAsync(billFetchReq);
        return Ok(resp);
    }
    
    [Authorize(Policies.Retailer)]
    [CustomJwtValidate]
    [HttpPost("addBill")]
    [SwaggerOperation(Summary = "retailer auth")]
    [EnableRateLimiting("BillRateLimitPolicy")]
    public async Task<IActionResult> AddBillAsync(BillAddReq billAddReq)
    {
        var resp = await billPaymentService.AddBillAsync(billAddReq, User.GetUserId());
        return Ok(resp);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("getAllPendingBills")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetAllPendingBillsAsync(BillGetAllPaginatedReq billGetAllPaginatedReq)
    {
        var resp = await billPaymentService.GetAllPaginatedAsync(billGetAllPaginatedReq, User.GetUserId(), User.GetRole(),true);
        return Ok(resp);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("getAllPendingBillsExcel")]
    [SwaggerOperation(Summary = "auth")]
    [EnableRateLimiting("ExcelRateLimitPolicy")]
    public async Task<IActionResult> GetAllPendingBillsExcelAsync(BillGetAllExcelReq billGetAllExcelReq)
    {
        var resp = await billPaymentService.GetAllExcelAsync(billGetAllExcelReq, User.GetUserId(), User.GetRole(),true);
        return Ok(resp);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("getAllBills")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetAllBillsAsync(BillGetAllPaginatedReq billGetAllPaginatedReq)
    {
        var resp = await billPaymentService.GetAllPaginatedAsync(billGetAllPaginatedReq, User.GetUserId(), User.GetRole(),false);
        return Ok(resp);
    }
    
    
    [Authorize]
    [CustomJwtValidate]
    [HttpPost("getAllBillsExcel")]
    [SwaggerOperation(Summary = "auth")]
    [EnableRateLimiting("ExcelRateLimitPolicy")]
    public async Task<IActionResult> GetAllBillsExcelAsync(BillGetAllExcelReq billGetAllExcelReq)
    {
        var resp = await billPaymentService.GetAllExcelAsync(billGetAllExcelReq, User.GetUserId(), User.GetRole(),false);
        return Ok(resp);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("markBillAsPaid")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> MarkBillAsPaidAsync(BillUpdateReq billUpdateReq)
    {
        var resp = await billPaymentService.MarkBillSuccess(billUpdateReq);
        return Ok(resp);
    }

    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("markBillAsPaidList")]
    [SwaggerOperation(Summary = "admin auth")]
    [RequestSizeLimit(5242880)]
    public async Task<IActionResult> MarkBillAsPaidListAsync([Required]IFormFile excelFile)
    {
        var resp = await billPaymentService.MarkBillSuccessExcelList(excelFile);
        return Ok(resp);
    }
    
    [Authorize(Policies.Admin)]
    [CustomJwtValidate]
    [HttpPost("markBillAsFailed")]
    [SwaggerOperation(Summary = "admin auth")]
    public async Task<IActionResult> MarkBillAsFailedAsync(BillUpdateReq billUpdateReq)
    {
        var resp = await billPaymentService.MarkBillFailed(billUpdateReq);
        return Ok(resp);
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("getBill")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetBillAsync([FromQuery] int billId)
    {
        var resp = await billPaymentService.GetBillAsync(billId, User.GetUserId(), User.GetRole());
        return Ok(resp);
    }
    
    [HttpPost]
    //accept application/x-www-form-urlencoded
    [Route("paymentCallback")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> PaymentCallbackAsync([FromForm] PaymentCallbackReq paymentCallbackReq)
    {
        await billPaymentService.PaymentCallbackAsync(paymentCallbackReq);
        return Ok();
    }
    
    [Authorize]
    [CustomJwtValidate]
    [HttpGet("getPrintBill")]
    [SwaggerOperation(Summary = "auth")]
    public async Task<IActionResult> GetPrintBillAsync([FromQuery,Required] int billId)
    {
        var resp = await billPaymentService.PrintBillAsync(billId, User.GetUserId());
        return Ok(resp);
    }
    
    // [HttpGet("generateSignatureForTest")]
    // public IActionResult GenerateSignatureForTest([FromQuery] string data)
    // {
    //     var signature = GenericHelper.GenerateSignature(data,"Check1ksd");
    //     return Ok(signature);
    // }
    
    
}