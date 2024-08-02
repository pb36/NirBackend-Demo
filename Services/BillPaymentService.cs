using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Ganss.Excel;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.BillPayment;
using NirvedBackend.Models.Requests.Consumer;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Models.Responses.BillPayment;
using StackExchange.Redis;

namespace NirvedBackend.Services;

public interface IBillPaymentService
{
    Task<BillFetchResp> FetchBillAsync(BillFetchReq billFetchReq);
    Task<StatusResp> AddBillAsync(BillAddReq billAddReq, int userId);
    Task<BillGetAllPaginatedResp> GetAllPaginatedAsync(BillGetAllPaginatedReq billGetAllPaginatedReq, int userId, UserType userType, bool onlyPending);
    Task<StatusResp> GetAllExcelAsync(BillGetAllExcelReq billGetAllExcelReq, int userId, UserType userType, bool onlyPending);
    Task<BillGetResp> MarkBillSuccess(BillUpdateReq billUpdateReq);
    Task<StatusResp> MarkBillSuccessExcelList(IFormFile file);
    Task<BillGetResp> MarkBillFailed(BillUpdateReq billUpdateReq);
    Task<BillGetResp> GetBillAsync(int billId, int userId, UserType userType);
    Task PaymentCallbackAsync(PaymentCallbackReq paymentCallbackReq);
    Task<StatusResp> PrintBillAsync(int billId, int userId);
}

public class BillPaymentService(NirvedContext context,IOptions<BillConfig> billconfig, IOptions<AwsS3Cred> awsS3Cred,IAmazonS3 amazonS3, ISendEndpointProvider bus, IConnectionMultiplexer redisCache) : IBillPaymentService
{
    private readonly HttpClient _client = new();
    private readonly IDatabase _hmacCache = redisCache.GetDatabase((int)RedisDatabases.HmacNonce);
    private readonly ISendEndpoint _walletSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/" + RabbitQueues.WalletQueue)).Result;
    private readonly ISendEndpoint _emailSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/" + RabbitQueues.EmailQueue)).Result;
    private readonly ISendEndpoint _whatsappSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.WhatsappMessageQueue)).Result;

    private readonly AwsS3Cred _awsS3Cred = awsS3Cred.Value;

    public async Task<BillFetchResp> FetchBillAsync(BillFetchReq billFetchReq)
    {
        var biller = await context.Billers.FindAsync(billFetchReq.BillerId);
        if (biller == null)
        {
            return new BillFetchResp
            {
                Success = false,
                ErrorCode = "BillerNotFound",
                ErrorMessage = "Biller not found"
            };
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "https://f2b34jkofeyrcpb6f6ozfncnre0ztmnr.lambda-url.ap-south-1.on.aws/");
        request.RequestUri = new Uri(request.RequestUri + $"?biller_id={biller.BillerId}&invoice_no={billFetchReq.ServiceNumber}&extra={billFetchReq.ExtraInfo}");
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return new BillFetchResp
            {
                Success = false,
                ErrorCode = "BillFetchFailed",
                ErrorMessage = "Bill fetch failed"
            };
        }

        var responseString = await response.Content.ReadAsStringAsync();
        var respData = JsonConvert.DeserializeObject<BillFetchResp>(responseString);
        return respData;
    }

    public async Task<StatusResp> AddBillAsync(BillAddReq billAddReq, int userId)
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(currentTimestamp - long.Parse(billAddReq.Timestamp)) > 60)
        {
            throw new AppException("Timestamp expired, please check your system time");
        }

        if (await _hmacCache.KeyExistsAsync(billAddReq.Nonce))
        {
            throw new AppException("Nonce already used");
        }

        var hmacString = new StringBuilder();
        hmacString.Append(billAddReq.Timestamp);
        hmacString.Append(billAddReq.Nonce);
        hmacString.Append(userId);
        foreach (var billAddBaseReq in billAddReq.Bills)
        {
            hmacString.Append(billAddBaseReq.BillerId);
            hmacString.Append(billAddBaseReq.ServiceNumber);
            hmacString.Append((int)billAddBaseReq.BillAmount);
        }

        var isValid = GenericHelper.IsValidHmac(billAddReq.Signature, hmacString.ToString(), "Check1ksd");
        if (!isValid)
        {
            throw new AppException($"Invalid signature");
        }

        await _hmacCache.StringSetAsync(billAddReq.Nonce, "1", TimeSpan.FromMinutes(1));

        var serverTime = await context.Configs.FirstOrDefaultAsync(x => x.Key == "ServerTime");
        if (serverTime == null)
        {
            serverTime = new Config
            {
                Key = "ServerTime",
                Value = $"{TimeOnly.Parse("08:00").ToString("t")}^^{TimeOnly.Parse("20:00").ToString("t")}",
                CreatedOn = DateTime.Now,
                UpdatedOn = null,
                UpdatedOnDate = null
            };
        }

        var serverTimeSplit = serverTime.Value.Split("^^");
        var startTime = TimeOnly.Parse(serverTimeSplit[0]);
        var endTime = TimeOnly.Parse(serverTimeSplit[1]);
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);
        if (currentTime < startTime || currentTime > endTime)
        {
            throw new AppException("Bill payment is not allowed at this time");
        }

        var totalAmount = billAddReq.Bills.Sum(b => b.BillAmount);
        var user= await context.Users.Where(x => x.UserId == userId).FirstAsync();
        var userBalance = user.Balance;
        if (userBalance < totalAmount)
        {
            throw new AppException("Insufficient balance");
        }


        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var billers = billAddReq.Bills.Select(bill => bill.BillerId).Distinct().ToList();
        var billerList = await context.Billers.Include(b => b.BillerCategory)
            .Where(biller => billers.Contains(biller.BillerId)).ToListAsync();

        foreach (var bill in billAddReq.Bills)
        {
            if (bill.BillAmount > billconfig.Value.MaxAmount)
            {
                throw new AppException("Bill amount must be less than or equal to "+billconfig.Value.MaxAmount);
            }
            
            var biller = billerList.FirstOrDefault(b => b.BillerId == bill.BillerId);
            if (biller == null)
            {
                throw new AppException("Biller not found for ServiceNumber: " + bill.ServiceNumber);
            }

            if (biller.BillerCategory.IsActive == false)
            {
                throw new AppException("Biller is not active for ServiceNumber: " + bill.ServiceNumber);
            }

            if (biller.IsActive == false)
            {
                throw new AppException("Biller is not active for ServiceNumber: " + bill.ServiceNumber);
            }

            var billExists = await context.Bills.AnyAsync(b => b.BillerId == bill.BillerId && 
                                                               b.ServiceNumber == bill.ServiceNumber &&
                                                               b.CreatedOnDate == dateTime.Item2 && b.Status != (int)BillStatus.Failed);
            if (billExists)
            {
                throw new AppException("Bill already added for ServiceNumber: " + bill.ServiceNumber + " in last 24 hours");
            }
        }

        var bills = new List<Bill>();
        foreach (var bill in billAddReq.Bills)
        {
            var biller = billerList.First(b => b.BillerId == bill.BillerId);
            var newBill = new Bill
            {
                BillerId = bill.BillerId,
                ServiceNumber = bill.ServiceNumber,
                Amount = bill.BillAmount,
                ExtraInfo = bill.ExtraInfo,
                CustomerName = bill.CustomerName,
                DueDate = bill.DueDate,
                CreatedOn = dateTime.Item1,
                CreatedOnDate = dateTime.Item2,
                Status = (int)BillStatus.Queued,
                CreatedBy = userId
            };
            var displayId = new StringBuilder();
            displayId.Append("NIR");
            displayId.Append(Guid.NewGuid().ToString()[..8]);
            // displayId.Append(newBill.CreatedOnDate.ToString("yy"));
            // displayId.Append(newBill.CreatedOnDate.DayOfYear.ToString("000"));
            displayId.Append(biller.Code);
            displayId.Append(bill.ServiceNumber.Length < 2 ? bill.ServiceNumber.PadLeft(2, '0') : bill.ServiceNumber[..2]);
            displayId.Append(bill.ServiceNumber.Length < 2 ? bill.ServiceNumber.PadRight(2, '0') : bill.ServiceNumber[^2..]);
            // displayId.Append(DateTime.Now.Millisecond.ToString("000"));
            newBill.DisplayId = displayId.ToString().ToUpper();
            await context.Bills.AddAsync(newBill);
            bills.Add(newBill);
        }

        await context.SaveChangesAsync();
        foreach (var bill in bills)
        {
            await _walletSendEndpoint.Send(new WalletConsumerReq
            {
                TransactionType = WalletTransactionType.ProcessBill,
                Data = JsonConvert.SerializeObject(new ProcessBillReq
                {
                    BillId = bill.BillId,
                    Amount = bill.Amount
                }),
            });
        }

        var firstPrintHtml = GenerateBillReceiptHtml(bills,user.DisplayId, true);
        var key = Guid.NewGuid().ToString();
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "BillPayment/" + key,
            ContentBody = firstPrintHtml,
            ContentType = "text/html"
        };
        await amazonS3.PutObjectAsync(putRequest);
        
        var url=_awsS3Cred.CloudFrontDomain + "/BillPayment/" + key;
        var signedUrl = GenericHelper.GenerateCloudFrontUrl(url, _awsS3Cred.KeyPairId, 5);
        return new StatusResp
        {
            Message = "Bill(s) Received Successfully",
            Url = signedUrl
        };
    }

    public async Task<StatusResp> PrintBillAsync(int billId,int userId)
    {
        var bill = await context.Bills.Include(b => b.Biller).
            Include(bill => bill.CreatedByNavigation).
            ThenInclude(user => user.CreatedByNavigation).
            ThenInclude(user=>user.CreatedByNavigation).
            ThenInclude(user=>user.CreatedByNavigation).FirstOrDefaultAsync(b => b.BillId == billId);
        if (bill == null)
        {
            throw new AppException("Bill not found");
        }
        if (bill.CreatedBy != userId)
        {
            if (bill.CreatedByNavigation.CreatedBy != userId)
            {
                if (bill.CreatedByNavigation.CreatedByNavigation!=null && bill.CreatedByNavigation.CreatedByNavigation.CreatedBy != userId)
                {
                    if (bill.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation != null && bill.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation.CreatedBy != userId)
                    {
                        throw new AppException("You are not authorized to print this bill");
                    }
                }
            }
        }
        
        var html = GenerateBillReceiptHtml(new List<Bill> { bill },bill.CreatedByNavigation.DisplayId);
        var key = Guid.NewGuid().ToString();
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "BillPayment/" + key,
            ContentBody = html,
            ContentType = "text/html"
        };
        await amazonS3.PutObjectAsync(putRequest);
        var url=_awsS3Cred.CloudFrontDomain + "/BillPayment/" + key;
        var signedUrl = GenericHelper.GenerateCloudFrontUrl(url, _awsS3Cred.KeyPairId, 5);
        return new StatusResp
        {
            Message = "Bill Printed Successfully",
            Url = signedUrl
        };
    }

    private static string GenerateBillReceiptHtml(List<Bill> bills,string merchantId, bool isFirstPrint = false)
    {
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang='en'><head>  <meta charset='UTF-8' />  <meta name='viewport' content='width=device-width, initial-scale=1.0' />  <title>Payment Receipt</title>  <style>    @media print {      body {        font-size: 4mm;      }      .main {        width: 72mm;        margin: 0 auto;      }    }  </style><script type='text/javascript'>window.print();  const regex = /Mobi|Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i;if(regex.test(navigator.userAgent)===false){window.onafterprint = window.close;};</script></head><body style='font-size: 4mm'>");
        foreach (var bill in bills)
        {
            html.Append("<div style='width: 72mm; margin: 0 auto' class='main'>    <div style='text-align: center; margin-top: 2.5mm; margin-bottom: 2.5mm'>      <div>        Demo Retail      </div>     <span>");
            html.Append(bill.Status == (int)BillStatus.Success ? "Online Payment Receipt" : "Cash Payment Receipt");
            html.Append("</span>    </div>    <hr style='border: 0.2mm dashed black' />    <div style='display: flex; align-items: center; justify-content: space-between; margin: 0 1.5mm'>");
            html.Append($"<span>Date: {bill.CreatedOn:dd-MM-yyyy}</span>");
            html.Append($"<span>Time: {bill.CreatedOn:hh:mm tt}</span></div><hr style='border: 0.2mm dashed black' />");
            html.Append("<div><table>");
            html.Append($"<tr><td width='49%'>Service Number</td><td width='1%'>:</td><td width='50%'>{bill.ServiceNumber}</td></tr>");
            html.Append($"<tr><td width='49%'>Customer Name</td><td width='1%'>:</td><td width='50%'>{bill.CustomerName}</td></tr>");
            html.Append($"<tr><td width='49%'>Transaction ID</td><td width='1%'>:</td><td width='50%'>{bill.DisplayId}</td></tr>");
            html.Append($"<tr><td width='49%'>Merchant ID</td><td width='1%'>:</td><td width='50%'>{merchantId}</td></tr>");
            if (isFirstPrint)
            {
                html.Append($"<tr><td width='49%'>Txn Status</td><td width='1%'>:</td><td width='50%'>Under Process</td></tr>");
            }
            else
            {
                html.Append($"<tr><td width='49%'>Txn Status</td><td width='1%'>:</td><td width='50%'>{((BillStatus)bill.Status).ToString()}</td></tr>");
                if (bill.Status != (int)BillStatus.Pending && bill.Status != (int)BillStatus.Queued)
                {
                    if (string.IsNullOrWhiteSpace(bill.PaymentRef) == false)
                        html.Append($"<tr><td width='49%'>Payment Ref</td><td width='1%'>:</td><td width='50%'>{bill.PaymentRef}</td></tr>");
                    if (string.IsNullOrWhiteSpace(bill.PaymentRef) == false)
                        html.Append($"<tr><td width='49%'>Remark</td><td width='1%'>:</td><td width='50%'>{bill.Remark}</td></tr>");
                }
            }

            html.Append($"<tr><td width='49%'>Biller</td><td width='1%'>:</td><td width='50%'>{bill.Biller.Name}</td></tr>");
            html.Append($"<tr><td width='49%'>Bill amount</td><td width='1%'>:</td><td width='50%'>{bill.Amount.ToString("F")}</td></tr>");
            html.Append("</table></div><hr style='border: 0.2mm dashed black' />");
            html.Append("<div><h4 style='text-align: center; margin: 2mm 0'>Thank You For Using Our Services</h4><h5 style='margin: 0'>Disclaimer -</h5><ol style='font-size: 2.5mm; padding-left: 2.6458333333mm'><li>Please check Service Number and amount mentioned in your receipt.</li><li>Biller may acknowledge payment within 2 business days.</li><li>Receipt required for the settlement of deposit, If any.</li></ol></div>");
            html.Append("</div>");
        }

        html.Append("</body></html>");
        return html.ToString();
    }

    public async Task<BillGetAllPaginatedResp> GetAllPaginatedAsync(BillGetAllPaginatedReq billGetAllPaginatedReq, int userId, UserType userType, bool onlyPending)
    {
        var query = context.Bills.AsNoTracking();
        if (onlyPending)
        {
            query = query.Where(b => b.Status == (int)BillStatus.Queued || b.Status == (int)BillStatus.Pending);
        }

        switch (billGetAllPaginatedReq.DateRange)
        {
            case PaginatedDateRange.Today:
                query = query.Where(x => x.CreatedOnDate == DateOnly.FromDateTime(DateTime.Now));
                break;
            case PaginatedDateRange.Month:
                billGetAllPaginatedReq.StartDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
                billGetAllPaginatedReq.EndDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)));
                query = query.Where(x => x.CreatedOnDate >= billGetAllPaginatedReq.StartDate &&
                                         x.CreatedOnDate <= billGetAllPaginatedReq.EndDate);
                break;
            case PaginatedDateRange.Custom:
                if (billGetAllPaginatedReq.StartDate == null || billGetAllPaginatedReq.EndDate == null)
                {
                    throw new AppException("Start date and end date must be provided when date range is custom");
                }

                if (billGetAllPaginatedReq.StartDate > billGetAllPaginatedReq.EndDate)
                {
                    throw new AppException("Start date must be less than end date");
                }

                if (billGetAllPaginatedReq.EndDate.Value.AddDays(-90) > billGetAllPaginatedReq.StartDate)
                {
                    throw new AppException("Date range must be less than or equal to 90 days");
                }

                query = query.Where(x => x.CreatedOnDate >= billGetAllPaginatedReq.StartDate && x.CreatedOnDate <= billGetAllPaginatedReq.EndDate);
                break;
            default:
                billGetAllPaginatedReq.StartDate = DateOnly.FromDateTime(DateTime.Today);
                break;
        }

        switch (userType)
        {
            case UserType.Admin:
                break;
            case UserType.SuperDistributor:
                query = query.Where(x => x.CreatedBy == userId || x.CreatedByNavigation.CreatedBy == userId || x.CreatedByNavigation.CreatedByNavigation.CreatedBy == userId);
                break;
            case UserType.Distributor:
                query = query.Where(x => x.CreatedBy == userId || x.CreatedByNavigation.CreatedBy == userId);
                break;
            case UserType.Retailer:
                query = query.Where(x => x.CreatedBy == userId);
                break;
        }

        if (!string.IsNullOrEmpty(billGetAllPaginatedReq.SearchString))
        {
            query = query.Where(x => x.DisplayId.Contains(billGetAllPaginatedReq.SearchString) ||
                                     x.CustomerName.StartsWith(billGetAllPaginatedReq.SearchString) ||
                                     x.ServiceNumber.StartsWith(billGetAllPaginatedReq.SearchString));
        }

        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new BillGetAllPaginatedResp
            {
                Bills = new List<BillGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };

        var bills = await query.OrderByDescending(u => u.BillId)
            .Skip((billGetAllPaginatedReq.Page - 1) * billGetAllPaginatedReq.Size)
            .Take(billGetAllPaginatedReq.Size)
            .Select(b => new BillGetResp()
            {
                CustomerName = b.CustomerName,
                BillId = b.BillId,
                Biller = b.Biller.Name,
                CreatedOn = b.CreatedOn,
                UpdatedOn = b.UpdatedOn,
                ExtraInfo = b.ExtraInfo,
                Status = ((BillStatus)b.Status).ToString(),
                StatusId = b.Status,
                BillAmount = b.Amount.ToString("F"),
                ServiceNumber = b.ServiceNumber,
                DueDate = b.DueDate,
                RetailerName = b.CreatedByNavigation.Name,
                DisplayId = b.DisplayId,
                ReferenceId = b.ReferenceNumber,
                Remark = b.Remark,
                PaymentRef = b.PaymentRef
            }).ToListAsync();

        return new BillGetAllPaginatedResp
        {
            Bills = bills,
            PageNumber = billGetAllPaginatedReq.Page,
            PageSize = billGetAllPaginatedReq.Size,
            TotalCount = totalRecords,
            PageCount = (int)Math.Ceiling((decimal)totalRecords / billGetAllPaginatedReq.Size)
        };
    }

    public async Task<StatusResp> GetAllExcelAsync(BillGetAllExcelReq billGetAllExcelReq, int userId, UserType userType, bool onlyPending)
    {
        throw new AppException("This functionality is only available in production copy, please contact support @ +918866605050");
        var query = context.Bills.AsNoTracking();
        if (onlyPending)
        {
            query = query.Where(b => b.Status == (int)BillStatus.Queued || b.Status == (int)BillStatus.Pending);
        }

        switch (billGetAllExcelReq.DateRange)
        {
            case PaginatedDateRange.Today:
                query = query.Where(x => x.CreatedOnDate == DateOnly.FromDateTime(DateTime.Now));
                break;
            case PaginatedDateRange.Month:
                throw new AppException("Please use custom date range for max weekly report of data");
                billGetAllExcelReq.StartDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
                billGetAllExcelReq.EndDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)));
                query = query.Where(x => x.CreatedOnDate >= billGetAllExcelReq.StartDate &&
                                         x.CreatedOnDate <= billGetAllExcelReq.EndDate);
                break;
            case PaginatedDateRange.Custom:
                if (billGetAllExcelReq.StartDate == null || billGetAllExcelReq.EndDate == null)
                {
                    throw new AppException("Start date and end date must be provided when date range is custom");
                }

                if (billGetAllExcelReq.StartDate > billGetAllExcelReq.EndDate)
                {
                    throw new AppException("Start date must be less than end date");
                }

                if (billGetAllExcelReq.EndDate.Value.AddDays(-7) > billGetAllExcelReq.StartDate)
                {
                    throw new AppException("Date range must be less than or equal to 7 days");
                }

                query = query.Where(x => x.CreatedOnDate >= billGetAllExcelReq.StartDate && x.CreatedOnDate <= billGetAllExcelReq.EndDate);
                break;
            default:
                billGetAllExcelReq.StartDate = DateOnly.FromDateTime(DateTime.Today);
                break;
        }

        switch (userType)
        {
            case UserType.Admin:
                break;
            case UserType.SuperDistributor:
                query = query.Where(x => x.CreatedBy == userId || x.CreatedByNavigation.CreatedBy == userId || x.CreatedByNavigation.CreatedByNavigation.CreatedBy == userId);
                break;
            case UserType.Distributor:
                query = query.Where(x => x.CreatedBy == userId || x.CreatedByNavigation.CreatedBy == userId);
                break;
            case UserType.Retailer:
                query = query.Where(x => x.CreatedBy == userId);
                break;
        }

        if (!string.IsNullOrEmpty(billGetAllExcelReq.SearchString))
        {
            query = query.Where(x => x.DisplayId.Contains(billGetAllExcelReq.SearchString) ||
                                     x.CustomerName.StartsWith(billGetAllExcelReq.SearchString) ||
                                     x.ServiceNumber.StartsWith(billGetAllExcelReq.SearchString));
        }

        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            throw new AppException("No records found");

        await _emailSendEndpoint.Send(new EmailConsumerReq
        {
            EmailSendType = EmailSendType.BillGetAllExcel,
            Data = JsonConvert.SerializeObject(new BillGetAllExcelConsumerReq()
            {
                UserType = userType,
                CurrentUserId = userId,
                DateRange = billGetAllExcelReq.DateRange,
                StartDate = billGetAllExcelReq.StartDate,
                EndDate = billGetAllExcelReq.EndDate,
                SearchString = billGetAllExcelReq.SearchString,
                OnlyPending = onlyPending
            })
        });

        return new StatusResp
        {
            Message = "Email Request received successfully",
        };
    }

    public async Task<BillGetResp> MarkBillSuccess(BillUpdateReq billUpdateReq)
    {
        // if (string.IsNullOrWhiteSpace(billUpdateReq.PaymentRef))
        //     throw new AppException("Payment reference is required");


        var bill = await context.Bills.Include(b => b.Biller).Include(bill => bill.CreatedByNavigation).FirstOrDefaultAsync(b => b.BillId == billUpdateReq.BillId);

        if (bill == null)
        {
            throw new AppException("Bill not found");
        }

        if (bill.Status != (int)BillStatus.Pending)
        {
            throw new AppException("Bill is not pending status");
        }

        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        
        bill.Status = (int)BillStatus.Success;
        bill.Remark = billUpdateReq.Reason;
        bill.PaymentRef = billUpdateReq.PaymentRef;
        bill.UpdatedOn = dateTime.Item1;
        bill.UpdatedOnDate = dateTime.Item2;
        await context.SaveChangesAsync();

        return new BillGetResp
        {
            Biller = bill.Biller.Name,
            BillAmount = bill.Amount.ToString("F"),
            BillId = bill.BillId,
            CustomerName = bill.CustomerName,
            DisplayId = bill.DisplayId,
            DueDate = bill.DueDate,
            ExtraInfo = bill.ExtraInfo,
            ServiceNumber = bill.ServiceNumber,
            Status = ((BillStatus)bill.Status).ToString(),
            StatusId = bill.Status,
            CreatedOn = bill.CreatedOn,
            UpdatedOn = bill.UpdatedOn,
            RetailerName = bill.CreatedByNavigation.Name,
            ReferenceId = bill.ReferenceNumber,
            Remark = bill.Remark,
            PaymentRef = bill.PaymentRef
        };
    }

    public async Task<StatusResp> MarkBillSuccessExcelList(IFormFile file)
    {
        throw new AppException("This functionality is only available in production copy, please contact support @ +918866605050");
        var bills = new ExcelMapper(file.OpenReadStream()).Fetch<BillUpdateListBase>().ToList();
        switch (bills.Count)
        {
            case 0:
                throw new AppException("No records found");
            case > 2000:
                throw new AppException("Maximum 2000 records allowed");
        }

        await _walletSendEndpoint.Send(new WalletConsumerReq
        {
            TransactionType = WalletTransactionType.ProcessBillList,
            Data = JsonConvert.SerializeObject(new BillUpdateListReq
            {
                BillUpdateList = bills
            }),
        });
        return new StatusResp
        {
            Message = "Bill(s) Received Successfully, status will be updated soon",
        };
    }

    public async Task<BillGetResp> MarkBillFailed(BillUpdateReq billUpdateReq)
    {
        
        var bill = await context.Bills.Include(b => b.Biller).Include(bill => bill.CreatedByNavigation).FirstOrDefaultAsync(b => b.BillId == billUpdateReq.BillId);
        if (bill == null)
        {
            throw new AppException("Bill not found");
        }

        if (bill.Status != (int)BillStatus.Pending && bill.Status != (int)BillStatus.Success)
        {
            throw new AppException("Bill is not in pending or success status");
        }
        
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        
        bill.Status = (int)BillStatus.Failed;
        bill.Remark = billUpdateReq.Reason;
        bill.UpdatedOn = dateTime.Item1;
        bill.UpdatedOnDate = dateTime.Item2;
        await context.SaveChangesAsync();
        await _walletSendEndpoint.Send(new WalletConsumerReq
        {
            TransactionType = WalletTransactionType.RefundBill,
            Data = JsonConvert.SerializeObject(new RefundReq
            {
                BillId = bill.BillId
            }),
        });
        await _whatsappSendEndpoint.Send(new WhatsappConsumerReq
        {
            Message = $"Id: {bill.DisplayId}\nService Number: {bill.ServiceNumber}\nAmount:{bill.Amount:F}\nTransaction Failed, Contact your customer to refund this amount",
            WhatsappMessageType = WhatsappMessageType.Text,
            PhoneNumber = bill.CreatedByNavigation.Mobile
        }); 
        return new BillGetResp
        {
            Biller = bill.Biller.Name,
            BillAmount = bill.Amount.ToString("F"),
            BillId = bill.BillId,
            CustomerName = bill.CustomerName,
            DisplayId = bill.DisplayId,
            DueDate = bill.DueDate,
            ExtraInfo = bill.ExtraInfo,
            ServiceNumber = bill.ServiceNumber,
            Status = ((BillStatus)bill.Status).ToString(),
            StatusId = bill.Status,
            CreatedOn = bill.CreatedOn,
            UpdatedOn = bill.UpdatedOn,
            RetailerName = bill.CreatedByNavigation.Name,
            ReferenceId = bill.ReferenceNumber,
            Remark = bill.Remark,
            PaymentRef = bill.PaymentRef
        };
    }

    public async Task<BillGetResp> GetBillAsync(int billId, int userId, UserType userType)
    {
        var bill = await context.Bills.Include(b => b.Biller).Include(bill => bill.CreatedByNavigation).ThenInclude(user => user.CreatedByNavigation).FirstOrDefaultAsync(b => b.BillId == billId);
        if (bill == null)
            throw new AppException("Bill not found");
        return userType switch
        {
            UserType.SuperDistributor when bill.CreatedBy != userId && bill.CreatedByNavigation.CreatedBy != userId && bill.CreatedByNavigation.CreatedByNavigation.CreatedBy != userId => throw new AppException("You are not authorized to view this bill"),
            UserType.Distributor when bill.CreatedBy != userId && bill.CreatedByNavigation.CreatedBy != userId => throw new AppException("You are not authorized to view this bill"),
            UserType.Retailer when bill.CreatedBy != userId => throw new AppException("You are not authorized to view this bill"),
            _ => new BillGetResp
            {
                Biller = bill.Biller.Name,
                BillAmount = bill.Amount.ToString("F"),
                CreatedOn = bill.CreatedOn,
                UpdatedOn = bill.UpdatedOn,
                RetailerName = bill.CreatedByNavigation.Name,
                Status = ((BillStatus)bill.Status).ToString(),
                StatusId = bill.Status,
                CustomerName = bill.CustomerName,
                DisplayId = bill.DisplayId,
                DueDate = bill.DueDate,
                ExtraInfo = bill.ExtraInfo,
                ServiceNumber = bill.ServiceNumber,
                BillId = bill.BillId,
                ReferenceId = bill.ReferenceNumber,
                Remark = bill.Remark,
                PaymentRef = bill.PaymentRef
            }
        };
    }

    public async Task PaymentCallbackAsync(PaymentCallbackReq paymentCallbackReq)
    {
        var bill = await context.Bills.FirstOrDefaultAsync(b => b.DisplayId == paymentCallbackReq.ClientRefNo);
        if (bill == null)
        {
            return;
        }

        if (paymentCallbackReq.Status == "0" || paymentCallbackReq.Status == "1")
        {
            await MarkBillSuccess(new BillUpdateReq
            {
                BillId = bill.BillId,
                Reason = paymentCallbackReq.StatusMsg,
                PaymentRef = paymentCallbackReq.TrnID + " " + paymentCallbackReq.OprID
            });
        }
        else
        {
            await MarkBillFailed(new BillUpdateReq
            {
                BillId = bill.BillId,
                Reason = paymentCallbackReq.StatusMsg
            });
        }
    }
}