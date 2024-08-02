using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Ganss.Excel;
using MassTransit;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.EmailTemplates;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Consumer;
using NirvedBackend.Models.Responses.Excel;

namespace NirvedBackend.Consumers;

public class EmailConsumer(ILogger<EmailConsumer> logger,IOptions<AwsSesAppSettings> awsSesAppSettings,NirvedContext context,IOptions<AwsS3Cred> awsS3Cred,IAmazonS3 amazonS3) : IConsumer<EmailConsumerReq>
{
    private readonly AwsSesAppSettings _awsSesAppSettings=awsSesAppSettings.Value;
    private readonly AmazonSimpleEmailServiceV2Client _sesClient = new AmazonSimpleEmailServiceV2Client(awsSesAppSettings.Value.AccessKey, awsSesAppSettings.Value.SecretKey, RegionEndpoint.APSouth1);
    private readonly AwsS3Cred _awsS3Cred = awsS3Cred.Value;

    public async Task Consume(ConsumeContext<EmailConsumerReq> context)
    {
        switch (context.Message.EmailSendType)
        {
            case EmailSendType.ForgotPassword:
                await ForgotPassword(JsonConvert.DeserializeObject<SendForgotPasswordReq>(context.Message.Data));
                break;
            case EmailSendType.LoginOtp:
                await SendLoginOtp(JsonConvert.DeserializeObject<SendLoginOtpReq>(context.Message.Data));
                break;
            case EmailSendType.UserGetAllExcel:
                await SendUserGetAllExcel(JsonConvert.DeserializeObject<UserGetAllExcelConsumerReq>(context.Message.Data));
                break;
            case EmailSendType.LedgerGetAllExcel:
                await SendLedgerGetAllExcel(JsonConvert.DeserializeObject<LedgerGetAllExcelConsumerReq>(context.Message.Data));
                break;
            case EmailSendType.BillGetAllExcel:
                await SendBillGetAllExcel(JsonConvert.DeserializeObject<BillGetAllExcelConsumerReq>(context.Message.Data));
                break;
            case EmailSendType.TopUpGetAllExcel:
                await SendTopUpGetAllExcel(JsonConvert.DeserializeObject<TopUpGetAllExcelConsumerReq>(context.Message.Data));
                break;
            case EmailSendType.CreditGetAllExcel:
                await SendCreditGetAllExcel(JsonConvert.DeserializeObject<CreditGetAllExcelConsumerReq>(context.Message.Data));
                break;
            case EmailSendType.OutstandingGetAllExcel:
                await SendOutstandingGetAllExcel(JsonConvert.DeserializeObject<OutstandingGetAllExcelConsumerReq>(context.Message.Data));
                break;
        }
    }

    private async Task SendOutstandingGetAllExcel(OutstandingGetAllExcelConsumerReq outstandingGetAllExcelConsumerReq)
    {
        var currentUser=await context.Users.FirstAsync(u => u.UserId == outstandingGetAllExcelConsumerReq.CurrentUserId);
        
        var query = context.Outstandings.Where(x => x.User.CreatedBy == outstandingGetAllExcelConsumerReq.CurrentUserId ||
                                                     x.UserId==outstandingGetAllExcelConsumerReq.CurrentUserId).AsNoTracking();

        if (!string.IsNullOrEmpty(outstandingGetAllExcelConsumerReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(outstandingGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Email.StartsWith(outstandingGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Username.StartsWith(outstandingGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Mobile.StartsWith(outstandingGetAllExcelConsumerReq.SearchString));
        }
        
        var outstanding = await query.OrderByDescending(x => x.OutstandingId).Select(x => new OutstandingGetAllBaseExcelResp
        {
            Amount = x.Amount,
            Id = x.OutstandingId,
            Name = x.User.Name,
            Number = x.User.Mobile
        }).ToListAsync();
        
        using var memoryStream = new MemoryStream();
        await new ExcelMapper().SaveAsync(memoryStream, outstanding, "Outstanding");
        memoryStream.Position = 0;
        
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "Email/" + $"Outstanding-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx",
            InputStream = memoryStream,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        };
        await amazonS3.PutObjectAsync(putRequest);
        
        var urlResp = GeneratePreSignedViewUrl(putRequest.Key);
        // var html=$"Exported Outstanding List <a href='{urlResp.Url}'>Click here</a> to download the file.<br/>This link will expire in 15 minutes";
        // var text = $"Exported Outstanding List {urlResp.Url} to download the file.\nThis link will expire in 15 minutes";
        
        var htmlFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.html");
        var textFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.txt");
        
        htmlFile = htmlFile.Replace("{0}", $"Exported Outstanding List");
        htmlFile = htmlFile.Replace("{1}", urlResp.Url);
        htmlFile = htmlFile.Replace("{2}", $"Download Outstanding-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx");
        htmlFile = htmlFile.Replace("{3}", urlResp.Url);
        
        textFile = textFile.Replace("{0}", $"Exported Outstanding List");
        textFile = textFile.Replace("{1}", urlResp.Url);
        
        await SendEmailRawGeneric(htmlFile, textFile, currentUser.Name, currentUser.Email, "Outstanding Export List");
        
    }

    private async Task SendCreditGetAllExcel(CreditGetAllExcelConsumerReq creditGetAllExcelConsumerReq)
    {
        var currentUser=await context.Users.FirstAsync(u => u.UserId == creditGetAllExcelConsumerReq.CurrentUserId);
        
        var query = context.CreditRequests.Where(x => x.User.CreatedBy == creditGetAllExcelConsumerReq.CurrentUserId ||
                                                      x.UserId==creditGetAllExcelConsumerReq.CurrentUserId).AsNoTracking();
        var dateOnly = DateOnly.FromDateTime(DateTime.Now.AddYears(-1));
        query = query.Where(x => x.CreatedOnDate >= dateOnly);
        if (!string.IsNullOrEmpty(creditGetAllExcelConsumerReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(creditGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Email.StartsWith(creditGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Username.StartsWith(creditGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Mobile.StartsWith(creditGetAllExcelConsumerReq.SearchString));
        }
        
        var creditRequests = await query.OrderByDescending(x => x.CreditRequestId).Select(x => new CreditGetAllBaseExcelResp
        {
            Amount = x.Amount,
            Id = x.CreditRequestId,
            Remark = x.Remark,
            RemitterName = x.User.Name,
            Status = ((CreditRequestStatus) x.Status).ToString(),
            TransactionDate = x.CreatedOn,
            UpdatedDate = x.UpdatedOn
        }).ToListAsync();
        
        using var memoryStream = new MemoryStream();
        await new ExcelMapper().SaveAsync(memoryStream, creditRequests, "Credit Requests");
        memoryStream.Position = 0;
        
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "Email/" + $"CreditRequests-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx",
            InputStream = memoryStream,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        };
        await amazonS3.PutObjectAsync(putRequest);
        
        var urlResp = GeneratePreSignedViewUrl(putRequest.Key);
        // var html=$"Exported Credit Requests List <a href='{urlResp.Url}'>Click here</a> to download the file.<br/>This link will expire in 15 minutes";
        // var text = $"Exported Credit Requests List {urlResp.Url} to download the file.\nThis link will expire in 15 minutes";
        
        var htmlFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.html");
        var textFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.txt");
        
        htmlFile = htmlFile.Replace("{0}", $"Exported Credit Requests List");
        htmlFile = htmlFile.Replace("{1}", urlResp.Url);
        htmlFile = htmlFile.Replace("{2}", $"Download Credit Requests-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx");
        htmlFile = htmlFile.Replace("{3}", urlResp.Url);
        
        textFile = textFile.Replace("{0}", $"Exported Credit Requests List");
        textFile = textFile.Replace("{1}", urlResp.Url);
        
        await SendEmailRawGeneric(htmlFile,textFile, currentUser.Name, currentUser.Email, "Credit Requests Export List");

    }

    private async Task SendTopUpGetAllExcel(TopUpGetAllExcelConsumerReq topUpGetAllExcelConsumerReq)
    {
        var currentUser=await context.Users.FirstAsync(u => u.UserId == topUpGetAllExcelConsumerReq.CurrentUserId);

        var query = context.TopUpRequests.Where(x => x.User.CreatedBy == topUpGetAllExcelConsumerReq.CurrentUserId || 
                                                     x.UserId==topUpGetAllExcelConsumerReq.CurrentUserId).AsNoTracking();
        var dateOnly = DateOnly.FromDateTime(DateTime.Now.AddYears(-1));
        query = query.Where(x => x.CreatedOnDate >= dateOnly);
        
        if (!string.IsNullOrEmpty(topUpGetAllExcelConsumerReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(topUpGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Email.StartsWith(topUpGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Username.StartsWith(topUpGetAllExcelConsumerReq.SearchString) ||
                                     x.User.Mobile.StartsWith(topUpGetAllExcelConsumerReq.SearchString));
        }
        
        var topUpRequests = await query.OrderByDescending(x => x.TopUpRequestId).Select(x => new TopUpGetAllBaseExcelResp
        {
            Amount = x.Amount,
            Bank = x.BankId == null ? "" : x.Bank.Name,
            DepositDate = x.DepositDate,
            Id = x.TopUpRequestId,
            PaymentMode = ((PaymentMode) x.PaymentMode).ToString(),
            ReferenceNumber = x.ReferenceNumber,
            Remark = x.Remark,
            RemitterName = x.User.Name,
            Status = ((TopUpRequestStatus) x.Status).ToString(),
            TransactionDate = x.CreatedOn,
            UpdatedDate = x.UpdatedOn,
        }).ToListAsync();
        using var memoryStream = new MemoryStream();
        await new ExcelMapper().SaveAsync(memoryStream, topUpRequests, "TopUp Requests");
        memoryStream.Position = 0;
        
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "Email/" + $"TopUpRequests-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx",
            InputStream = memoryStream,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        };
        await amazonS3.PutObjectAsync(putRequest);
        
        var urlResp = GeneratePreSignedViewUrl(putRequest.Key);
        // var html=$"Exported TopUp Requests List <a href='{urlResp.Url}'>Click here</a> to download the file.<br/>This link will expire in 15 minutes";
        // var text = $"Exported TopUp Requests List {urlResp.Url} to download the file.\nThis link will expire in 15 minutes";
        
        var htmlFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.html");
        var textFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.txt");
        
        htmlFile = htmlFile.Replace("{0}", $"Exported TopUp Requests List");
        htmlFile = htmlFile.Replace("{1}", urlResp.Url);
        htmlFile = htmlFile.Replace("{2}", $"Download TopUp Requests-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx");
        htmlFile = htmlFile.Replace("{3}", urlResp.Url);
        
        textFile = textFile.Replace("{0}", $"Exported TopUp Requests List");
        textFile = textFile.Replace("{1}", urlResp.Url);
        
        await SendEmailRawGeneric(htmlFile, textFile, currentUser.Name, currentUser.Email, "TopUp Requests Export List");

    }

    private async Task SendBillGetAllExcel(BillGetAllExcelConsumerReq billGetAllExcelConsumerReq)
    {
        var currentUser=await context.Users.FirstAsync(u => u.UserId == billGetAllExcelConsumerReq.CurrentUserId);
        var query = context.Bills.AsNoTracking();
        if (billGetAllExcelConsumerReq.OnlyPending)
        {
            query = query.Where(b => b.Status == (int)BillStatus.Queued || b.Status == (int)BillStatus.Pending);
        }

        switch (billGetAllExcelConsumerReq.DateRange)
        {
            case PaginatedDateRange.Today:
                query = query.Where(x => x.CreatedOnDate == DateOnly.FromDateTime(DateTime.Now));
                break;
            case PaginatedDateRange.Month:
                billGetAllExcelConsumerReq.StartDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
                billGetAllExcelConsumerReq.EndDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)));
                query = query.Where(x => x.CreatedOnDate >= billGetAllExcelConsumerReq.StartDate &&
                                         x.CreatedOnDate <= billGetAllExcelConsumerReq.EndDate);
                break;
            case PaginatedDateRange.Custom:
                if (billGetAllExcelConsumerReq.StartDate == null || billGetAllExcelConsumerReq.EndDate == null)
                {
                    // throw new AppException("Start date and end date must be provided when date range is custom");
                    return;
                }

                if (billGetAllExcelConsumerReq.StartDate > billGetAllExcelConsumerReq.EndDate)
                {
                    // throw new AppException("Start date must be less than end date");
                    return;
                }

                if (billGetAllExcelConsumerReq.EndDate.Value.AddDays(-7) > billGetAllExcelConsumerReq.StartDate)
                {
                    // throw new AppException("Date range must be less than or equal to 90 days");
                    return;
                }

                query = query.Where(x => x.CreatedOnDate >= billGetAllExcelConsumerReq.StartDate && x.CreatedOnDate <= billGetAllExcelConsumerReq.EndDate);
                break;
            default:
                billGetAllExcelConsumerReq.StartDate = DateOnly.FromDateTime(DateTime.Today);
                break;
        }

        switch (billGetAllExcelConsumerReq.UserType)
        {
            case UserType.Admin:
                break;
            case UserType.SuperDistributor:
                query = query.Where(x => x.CreatedBy == billGetAllExcelConsumerReq.CurrentUserId || x.CreatedByNavigation.CreatedBy == billGetAllExcelConsumerReq.CurrentUserId || x.CreatedByNavigation.CreatedByNavigation.CreatedBy == billGetAllExcelConsumerReq.CurrentUserId);
                break;
            case UserType.Distributor:
                query = query.Where(x => x.CreatedBy == billGetAllExcelConsumerReq.CurrentUserId || x.CreatedByNavigation.CreatedBy == billGetAllExcelConsumerReq.CurrentUserId);
                break;
            case UserType.Retailer:
                query = query.Where(x => x.CreatedBy == billGetAllExcelConsumerReq.CurrentUserId);
                break;
        }

        if (!string.IsNullOrEmpty(billGetAllExcelConsumerReq.SearchString))
        {
            query = query.Where(x => x.DisplayId.Contains(billGetAllExcelConsumerReq.SearchString) ||
                                     x.CustomerName.StartsWith(billGetAllExcelConsumerReq.SearchString) ||
                                     x.ServiceNumber.StartsWith(billGetAllExcelConsumerReq.SearchString));
        }
        
        var bills = await query.OrderByDescending(x => x.BillId).Select(x => new BillGetAllBaseExcelResp
        {
            Amount = x.Amount,
            CustomerName = x.CustomerName,
            DueDate = x.DueDate,
            ExtraInfo = x.ExtraInfo,
            Name = x.CreatedByNavigation.Name,
            Remark = x.Remark,
            ServiceNumber = x.ServiceNumber,
            Status = ((BillStatus) x.Status).ToString(),
            TransactionDate = x.CreatedOn,
            UpdatedDate = x.UpdatedOn,
            TransactionId = x.DisplayId,
            Type = $"{x.Biller.Name} - {x.Biller.BillerCategory.Name}",
            PaymentRef = x.PaymentRef
        }).ToListAsync();
        using var memoryStream = new MemoryStream();
        await new ExcelMapper().SaveAsync(memoryStream, bills, "Bills");
        memoryStream.Position = 0;
        
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "Email/" + $"Bills-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx",
            InputStream = memoryStream,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
        await amazonS3.PutObjectAsync(putRequest);
        
        var urlResp = GeneratePreSignedViewUrl(putRequest.Key);
        // var html=$"Exported Bills List <a href='{urlResp.Url}'>Click here</a> to download the file.<br/>This link will expire in 15 minutes";
        // var text = $"Exported Bills List {urlResp.Url} to download the file.\nThis link will expire in 15 minutes";
        
        var htmlFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.html");
        var textFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.txt");
        
        htmlFile = htmlFile.Replace("{0}", $"Exported Bills List");
        htmlFile = htmlFile.Replace("{1}", urlResp.Url);
        htmlFile = htmlFile.Replace("{2}", $"Download Bills-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx");
        htmlFile = htmlFile.Replace("{3}", urlResp.Url);
        
        textFile = textFile.Replace("{0}", $"Exported Bills List");
        textFile = textFile.Replace("{1}", urlResp.Url);
        
        await SendEmailRawGeneric(htmlFile, textFile, currentUser.Name, currentUser.Email, "Bills Export List");
        
        
    }
    
    private UrlResp GeneratePreSignedViewUrl(string id)
    {
        var url=_awsS3Cred.CloudFrontDomain + "/" + id;
        var signedUrl = GenericHelper.GenerateCloudFrontUrl(url, _awsS3Cred.KeyPairId, 60*12);
        return new UrlResp
        {
            Url = signedUrl,
            Id = id
        };
    }

    private async Task SendLedgerGetAllExcel(LedgerGetAllExcelConsumerReq ledgerGetAllExcelConsumerReq)
    {
        var currentUser=await context.Users.FirstAsync(u => u.UserId == ledgerGetAllExcelConsumerReq.CurrentUserId);
        var query= context.Ledgers.AsQueryable();
        switch (ledgerGetAllExcelConsumerReq.DateRange)
        {
            case PaginatedDateRange.Today:
                query = query.Where(x => x.CreatedOnDate == DateOnly.FromDateTime(DateTime.Now));
                break;
            case PaginatedDateRange.Month:
                ledgerGetAllExcelConsumerReq.StartDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
                ledgerGetAllExcelConsumerReq.EndDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)));
                query = query.Where(x => x.CreatedOnDate >= ledgerGetAllExcelConsumerReq.StartDate && 
                    x.CreatedOnDate <= ledgerGetAllExcelConsumerReq.EndDate);
                break;
            case PaginatedDateRange.Custom:
                if (ledgerGetAllExcelConsumerReq.StartDate == null || ledgerGetAllExcelConsumerReq.EndDate == null)
                {
                    // throw new AppException("Start date and end date must be provided when date range is custom");
                    return;
                }
                if (ledgerGetAllExcelConsumerReq.StartDate > ledgerGetAllExcelConsumerReq.EndDate)
                {
                    // throw new AppException("Start date must be less than end date");
                    return;
                }
                if (ledgerGetAllExcelConsumerReq.EndDate.Value.AddDays(-7) > ledgerGetAllExcelConsumerReq.StartDate)
                {
                    // throw new AppException("Date range must be less than or equal to 90 days");
                    return;
                }
                query = query.Where(x => x.CreatedOnDate >= ledgerGetAllExcelConsumerReq.StartDate && x.CreatedOnDate <= ledgerGetAllExcelConsumerReq.EndDate);
                break;
            default:
                ledgerGetAllExcelConsumerReq.StartDate = DateOnly.FromDateTime(DateTime.Today);
                break;
        }

        query = ledgerGetAllExcelConsumerReq.UserType switch
        {
            UserType.SuperDistributor or UserType.Distributor => query.Where(x => x.UserId == ledgerGetAllExcelConsumerReq.CurrentUserId || x.User.CreatedBy == ledgerGetAllExcelConsumerReq.CurrentUserId),
            UserType.Retailer => query.Where(x => x.UserId == ledgerGetAllExcelConsumerReq.CurrentUserId),
            _ => query
        };
        if (!string.IsNullOrEmpty(ledgerGetAllExcelConsumerReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(ledgerGetAllExcelConsumerReq.SearchString) || x.User.Mobile.StartsWith(ledgerGetAllExcelConsumerReq.SearchString));
        }

        var tempQuery = query.OrderByDescending(x => x.LedgerId).Select(x => new LedgerGetAllBaseExcelResp
        {
            Amount = x.Amount,
            BillId = x.BillId != null ? x.Bill.DisplayId : "",
            Closing = x.Closing,
            CreatedOn = x.CreatedOn,
            LedgerId = x.LedgerId,
            Name = x.User.Name,
            UserType = ((UserType)x.User.UserType).ToString(),
            Opening = x.Opening,
            Remark = x.Remark,
            TopUpRequestId = x.TopUpRequestId,
            Type = ((TransactionType)x.Type).ToString(),
            CreditRequestId = x.CreditRequestId,
            BillType = x.BillId != null ? "Bill" : x.TopUpRequestId != null ? "TopUp" : x.CreditRequestId != null ? "Credit" : ""
        });
        
        logger.LogInformation("Ledger Query {Query}", tempQuery.ToQueryString());
        
        var ledgers = await query.OrderByDescending(x => x.LedgerId).Select(x => new LedgerGetAllBaseExcelResp
        {
            Amount = x.Amount,
            BillId = x.BillId!=null?x.Bill.DisplayId:"",
            Closing = x.Closing,
            CreatedOn = x.CreatedOn,
            LedgerId = x.LedgerId,
            Name = x.User.Name,
            UserType = ((UserType) x.User.UserType).ToString(),
            Opening = x.Opening,
            Remark = x.Remark,
            TopUpRequestId = x.TopUpRequestId,
            Type = ((TransactionType) x.Type).ToString(),
            CreditRequestId = x.CreditRequestId,
            BillType = x.BillId!=null?"Bill":x.TopUpRequestId!=null?"TopUp":x.CreditRequestId!=null?"Credit":""
        }).ToListAsync();
        using var memoryStream = new MemoryStream();
        await new ExcelMapper().SaveAsync(memoryStream, ledgers, "Account Summary");
        memoryStream.Position = 0;
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "Email/" + $"AccountSummary-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx",
            InputStream = memoryStream,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
        await amazonS3.PutObjectAsync(putRequest);
        
        var urlResp = GeneratePreSignedViewUrl(putRequest.Key);
        // var html=$"Exported Account Summary List <a href='{urlResp.Url}'>Click here</a> to download the file.<br/>This link will expire in 15 minutes";
        // var text = $"Exported Account Summary List {urlResp.Url} to download the file.\nThis link will expire in 15 minutes";

        var htmlFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.html");
        var textFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.txt");
        
        htmlFile = htmlFile.Replace("{0}", $"Exported Account Summary List");
        htmlFile = htmlFile.Replace("{1}", urlResp.Url);
        htmlFile = htmlFile.Replace("{2}", $"Download Account Summary-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx");
        htmlFile = htmlFile.Replace("{3}", urlResp.Url);
        
        textFile = textFile.Replace("{0}", $"Exported Account Summary List");
        textFile = textFile.Replace("{1}", urlResp.Url);
        
        
        await SendEmailRawGeneric(htmlFile, textFile, currentUser.Name, currentUser.Email, "Account Summary Export List");
    }

    private async Task SendUserGetAllExcel(UserGetAllExcelConsumerReq userGetAllExcelConsumerReq)
    {
        var currentUser=await context.Users.FirstAsync(u => u.UserId == userGetAllExcelConsumerReq.CurrentUserId);
        User parentUser = null;
        if (userGetAllExcelConsumerReq.ParentId != null && userGetAllExcelConsumerReq.ParentId != 0 && userGetAllExcelConsumerReq.ParentId != userGetAllExcelConsumerReq.CurrentUserId)
        {
            parentUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userGetAllExcelConsumerReq.ParentId);
            if (parentUser == null)
            {
                logger.LogError("Parent User Not Found {ParentId}", userGetAllExcelConsumerReq.ParentId);
                return;
            }
                
        }

        var query = context.Users.AsQueryable();
        switch (userGetAllExcelConsumerReq.UserType)
        {
            case UserType.Admin:
            {
                if (parentUser != null)
                {
                    query = parentUser.UserType == (int)UserType.SuperDistributor ? query.Where(u => u.CreatedBy == userGetAllExcelConsumerReq.ParentId || u.CreatedByNavigation.CreatedBy == userGetAllExcelConsumerReq.ParentId) : query.Where(u => u.CreatedBy == userGetAllExcelConsumerReq.ParentId);
                }

                break;
            }
            case UserType.SuperDistributor:
                if (parentUser != null)
                {
                    switch (parentUser.UserType)
                    {
                        case (int)UserType.Admin:
                            // throw new AppException("Parent user not found");
                            return;
                        case (int)UserType.SuperDistributor when parentUser.UserId != userGetAllExcelConsumerReq.CurrentUserId:
                            // throw new AppException("Super distributor can not get other super distributor's retailer");
                            return;
                        case (int)UserType.Distributor when parentUser.CreatedBy != userGetAllExcelConsumerReq.CurrentUserId:
                            // throw new AppException("Super distributor can not get other distributor's retailer");
                            return;
                        default:
                            query = parentUser.UserType == (int)UserType.Distributor ? query.Where(u => u.CreatedBy == userGetAllExcelConsumerReq.ParentId) : query.Where(u => u.CreatedBy == userGetAllExcelConsumerReq.ParentId || u.CreatedByNavigation.CreatedBy == userGetAllExcelConsumerReq.ParentId);
                            break;
                    }
                }
                else
                {
                    query = query.Where(u => u.CreatedBy == userGetAllExcelConsumerReq.CurrentUserId || u.CreatedByNavigation.CreatedBy == userGetAllExcelConsumerReq.CurrentUserId);
                }
                break;
            case UserType.Distributor:
                query = query.Where(u => u.CreatedBy == userGetAllExcelConsumerReq.CurrentUserId);
                break;
            case UserType.Retailer:
                break;
        }

        if (!string.IsNullOrWhiteSpace(userGetAllExcelConsumerReq.SearchString))
            query = query.Where(u => u.Name.StartsWith(userGetAllExcelConsumerReq.SearchString) ||
                                     u.Username.StartsWith(userGetAllExcelConsumerReq.SearchString) ||
                                     u.Email.StartsWith(userGetAllExcelConsumerReq.SearchString) ||
                                     u.Mobile.StartsWith(userGetAllExcelConsumerReq.SearchString));
        
        var users = await query.OrderByDescending(u => u.UserId).Select(x => new UserGetAllBaseExcelResp
        {
            UserType = ((UserType) x.UserType).ToString(),
            Name = x.Name,
            Username = x.Username,
            Balance = x.Balance,
            Mobile = x.Mobile,
            IsActive = x.IsActive,
            ParentUser = x.CreatedByNavigation.Name,
            DisplayId = x.DisplayId,
        }).ToListAsync();
        using var memoryStream = new MemoryStream();
        await new ExcelMapper().SaveAsync(memoryStream, users, "Users");
        memoryStream.Position = 0;
        
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "Email/" + $"Users-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx",
            InputStream = memoryStream,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
        await amazonS3.PutObjectAsync(putRequest);
        
        var urlResp = GeneratePreSignedViewUrl(putRequest.Key);
        
        var htmlFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.html");
        var textFile = await File.ReadAllTextAsync(@"Models/EmailTemplates/Export.txt");
        
        htmlFile = htmlFile.Replace("{0}", $"Exported Users List");
        htmlFile = htmlFile.Replace("{1}", urlResp.Url);
        htmlFile = htmlFile.Replace("{2}", $"Download Users-{currentUser.Username}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx");
        htmlFile = htmlFile.Replace("{3}", urlResp.Url);
        
        textFile = textFile.Replace("{0}", $"Exported Users List");
        textFile = textFile.Replace("{1}", urlResp.Url);
        

        await SendEmailRawGeneric(htmlFile, textFile, currentUser.Name, currentUser.Email, "Users Export List");
        
        
    }

    private async Task ForgotPassword(SendForgotPasswordReq sendForgotPasswordReq)
    {
        var actionUrl = sendForgotPasswordReq.Origin + "/auth/resetPassword?token=" + sendForgotPasswordReq.Guid;
        var htmlFile =await File.ReadAllTextAsync(@"Models/EmailTemplates/ForgotPassword.html");
        htmlFile = htmlFile.Replace("{0}", sendForgotPasswordReq.Name);
        htmlFile = htmlFile.Replace("{1}", actionUrl);
        htmlFile = htmlFile.Replace("{2}", actionUrl);
        
        var textFile =string.Format(await File.ReadAllTextAsync(@"Models/EmailTemplates/ForgotPassword.txt"), sendForgotPasswordReq.Name, actionUrl);
        await SendEmailRawGeneric(htmlFile, textFile, sendForgotPasswordReq.Name,sendForgotPasswordReq.Email, "Reset Password Request From Nirved MultiServices LLP");
    }

    private async Task SendLoginOtp(SendLoginOtpReq sendLoginOtpReq)
    {
        var htmlFile = string.Format(await File.ReadAllTextAsync(@"Models/EmailTemplates/Otp.html"), sendLoginOtpReq.Name, sendLoginOtpReq.Otp, sendLoginOtpReq.Ip);
        var textFile = string.Format(await File.ReadAllTextAsync(@"Models/EmailTemplates/Otp.txt"), sendLoginOtpReq.Name, sendLoginOtpReq.Otp, sendLoginOtpReq.Ip);
        await SendEmailRawGeneric(htmlFile, textFile, sendLoginOtpReq.Name,sendLoginOtpReq.Email, "OTP From Nirved MultiServices LLP");
    }
    
    private async Task SendEmailRawGeneric(string html, string text,string name, string email, string subject,MemoryStream attachment=null,string attachmentName=null)
    {

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_awsSesAppSettings.Name, _awsSesAppSettings.From));
        message.To.Add(new MailboxAddress(name, email));
        message.Subject = subject;
        message.ReplyTo.Add(new MailboxAddress(_awsSesAppSettings.Name, _awsSesAppSettings.ReplyTo));
        message.Headers.Add("List-Unsubscribe", $"<mailto:{_awsSesAppSettings.ReplyTo}?subject=Unsubscribe&body={email}>");
        var builder = new BodyBuilder
        {
            HtmlBody = html,
            TextBody = text
        };
        if (attachment != null)
        {
            await builder.Attachments.AddAsync(attachmentName, attachment);
        }
        message.Body = builder.ToMessageBody();
        using var memoryStream = new MemoryStream();
        await message.WriteToAsync(memoryStream);
        
        
        
        var sendEmailReq = await _sesClient.SendEmailAsync(new SendEmailRequest
        {
            Content = new EmailContent
            {
                Raw = new RawMessage
                {
                    Data = memoryStream
                }
            },
            //with name and email
            FromEmailAddress = $"{_awsSesAppSettings.Name} <{_awsSesAppSettings.From}>",
            Destination = new Destination
            {
                ToAddresses = [$"{name} <{email}>"]
            },
            ReplyToAddresses = [$"{_awsSesAppSettings.Name} <{_awsSesAppSettings.ReplyTo}>"],
        });
        if (sendEmailReq.HttpStatusCode != HttpStatusCode.OK)
            logger.LogError("Email Send Failed {Email} {Subject}", email, subject);
    }
}