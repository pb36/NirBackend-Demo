using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Consumer;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Models.Requests.TopUp;
using NirvedBackend.Models.Responses.TopUp;

namespace NirvedBackend.Services;

public interface ITopUpService
{
    Task<TopUpGetResp> AddTopUpRequestAsync(TopUpAddReq topUpAddReq, int userId);
    UrlResp GeneratePreSignedViewUrl(string id);
    Task<TopUpGetAllPaginatedResp> GetAllTopUpRequestsAsync(TopUpGetAllPaginatedReq topUpGetAllPaginatedReq, int userId);
    Task<StatusResp> GetAllExcelAsync(TopUpGetAllExcelReq topUpGetAllExcelReq, int userId);
    Task<TopUpGetResp> GetTopUpRequestAsync(int topUpRequestId, int userId);
    Task<StatusResp> ProcessTopUpRequestAsync(TopUpProcessReq topUpProcessReq, int userId);
}

public class TopUpService(NirvedContext context,IOptions<AwsS3Cred> awsS3Cred,IAmazonS3 amazonS3,ISendEndpointProvider bus) : ITopUpService
{
    
    private readonly AwsS3Cred _awsS3Cred = awsS3Cred.Value;
    private readonly ISendEndpoint _walletSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.WalletQueue)).Result;
    private readonly ISendEndpoint _whatsappSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.WhatsappMessageQueue)).Result;
    private readonly ISendEndpoint _emailSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/" + RabbitQueues.EmailQueue)).Result;


    public async Task<TopUpGetResp> AddTopUpRequestAsync(TopUpAddReq topUpAddReq, int userId)
    {
        Bank bank = null;
        if (topUpAddReq.BankId != null && topUpAddReq.BankId != 0)
        {
            bank = await context.Banks.FirstOrDefaultAsync(x => x.BankId == topUpAddReq.BankId);
            if (bank == null)
                throw new AppException("Bank not found");
        }

        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var imageId = Guid.NewGuid()+"."+topUpAddReq.Extension;
        var topUp = new TopUpRequest
        {
            Amount = topUpAddReq.Amount,
            PaymentMode = (int)topUpAddReq.PaymentMode,
            BankId = bank?.BankId,
            DepositDate = topUpAddReq.DepositDate,
            ReferenceNumber = topUpAddReq.ReferenceNumber,
            ImageId = imageId,
            Status = (int)TopUpRequestStatus.Pending,
            CreatedOn = dateTime.Item1,
            CreatedOnDate = dateTime.Item2,
            UserId = userId,
        };
        await context.TopUpRequests.AddAsync(topUp);
        await context.SaveChangesAsync();
        var putRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "TopUp/" + imageId,
            InputStream = GenericHelper.GenerateStreamFromBase64String(topUpAddReq.ImageBase64),
            ContentType = "image/"+topUpAddReq.Extension,
            StorageClass = S3StorageClass.IntelligentTiering
        };
        await amazonS3.PutObjectAsync(putRequest);
        topUp.User = await context.Users.Include(x=>x.CreatedByNavigation).FirstAsync(x => x.UserId == userId);
        
        await _whatsappSendEndpoint.Send(new WhatsappConsumerReq
        {
            WhatsappMessageType = WhatsappMessageType.Image,
            PhoneNumber = topUp.User.CreatedByNavigation.Mobile,
            Url = GeneratePreSignedViewUrl(imageId).Url,
            Message = $"Id: *{topUp.TopUpRequestId}*"
        });
        
        await _whatsappSendEndpoint.Send(new WhatsappConsumerReq
        {
            WhatsappMessageType = WhatsappMessageType.Text,
            PhoneNumber = topUp.User.CreatedByNavigation.Mobile,
            Message = $"TopUp request Received\n\nId: *{topUp.TopUpRequestId}*\nAmount: *{topUpAddReq.Amount}*\nFrom: *{topUp.User.Name}*\nPayment Mode: *{((PaymentMode) topUp.PaymentMode).ToString()}*\nReference Number: *{topUpAddReq.ReferenceNumber}*"
        });
        
        return new TopUpGetResp
        {
            TopUpRequestId = topUp.TopUpRequestId,
            Amount = topUp.Amount,
            PaymentMode = ((PaymentMode) topUp.PaymentMode).ToString(),
            PaymentModeId = topUp.PaymentMode,
            BankName = bank != null ? $"{bank.Name} - {bank.AccountNumber}" : null,
            BankId = topUp.BankId,
            DepositDate = topUp.DepositDate,
            ReferenceNumber = topUp.ReferenceNumber,
            Remark = topUp.Remark,
            ImageId = topUp.ImageId,
            Status = ((TopUpRequestStatus) topUp.Status).ToString(),
            StatusId = topUp.Status,
            CreatedOn = topUp.CreatedOn,
            UpdatedOn = topUp.UpdatedOn,
            RemitterName = topUp.User.Name,
            CurrentUser = topUp.UserId == userId
        };
    }

    public UrlResp GeneratePreSignedViewUrl(string id)
    {
        var url=_awsS3Cred.CloudFrontDomain + "/TopUp/" + id;
        var signedUrl = GenericHelper.GenerateCloudFrontUrl(url, _awsS3Cred.KeyPairId, 5);
        return new UrlResp
        {
            Url = signedUrl,
            Id = id
        };
    }

    public async Task<TopUpGetAllPaginatedResp> GetAllTopUpRequestsAsync(TopUpGetAllPaginatedReq topUpGetAllPaginatedReq, int userId)
    {
        var query = context.TopUpRequests.Where(x => x.User.CreatedBy == userId || x.UserId==userId).AsNoTracking();
        if (!string.IsNullOrEmpty(topUpGetAllPaginatedReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(topUpGetAllPaginatedReq.SearchString) ||
                                     x.User.Email.StartsWith(topUpGetAllPaginatedReq.SearchString) ||
                                     x.User.Username.StartsWith(topUpGetAllPaginatedReq.SearchString) ||
                                     x.User.Mobile.StartsWith(topUpGetAllPaginatedReq.SearchString));
        }
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new TopUpGetAllPaginatedResp
            {
                TopUps = new List<TopUpGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };
        
        var topUps = await query
            .OrderByDescending(u => u.TopUpRequestId)
            .Skip((topUpGetAllPaginatedReq.Page - 1) * topUpGetAllPaginatedReq.Size)
            .Take(topUpGetAllPaginatedReq.Size)
            .Select(x => new TopUpGetResp
            {
                TopUpRequestId = x.TopUpRequestId,
                Amount = x.Amount,
                PaymentMode = ((PaymentMode) x.PaymentMode).ToString(),
                PaymentModeId = x.PaymentMode,
                BankName = x.Bank != null ? $"{x.Bank.Name} - {x.Bank.AccountNumber}" : null,
                BankId = x.BankId,
                DepositDate = x.DepositDate,
                ReferenceNumber = x.ReferenceNumber,
                Remark = x.Remark,
                ImageId = x.ImageId.ToString(),
                Status = ((TopUpRequestStatus) x.Status).ToString(),
                StatusId = x.Status,
                CreatedOn = x.CreatedOn,
                UpdatedOn = x.UpdatedOn,
                RemitterName = x.User.Name,
                CurrentUser = x.UserId == userId
            }).ToListAsync();
        
        return new TopUpGetAllPaginatedResp
        {
            TopUps = topUps,
            PageCount = (int)Math.Ceiling(totalRecords / (double)topUpGetAllPaginatedReq.Size),
            PageNumber = topUpGetAllPaginatedReq.Page,
            PageSize = topUpGetAllPaginatedReq.Size,
            TotalCount = totalRecords
        };
    }

    public async Task<StatusResp> GetAllExcelAsync(TopUpGetAllExcelReq topUpGetAllExcelReq, int userId)
    {
        throw new AppException("This functionality is only available in production copy, please contact support @ +918866605050");
        var query = context.TopUpRequests.Where(x => x.User.CreatedBy == userId || x.UserId==userId).AsNoTracking();
        if (!string.IsNullOrEmpty(topUpGetAllExcelReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(topUpGetAllExcelReq.SearchString) ||
                                     x.User.Email.StartsWith(topUpGetAllExcelReq.SearchString) ||
                                     x.User.Username.StartsWith(topUpGetAllExcelReq.SearchString) ||
                                     x.User.Mobile.StartsWith(topUpGetAllExcelReq.SearchString));
        }
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            throw new AppException("No records found");
        
        await _emailSendEndpoint.Send(new EmailConsumerReq
        {
            EmailSendType = EmailSendType.TopUpGetAllExcel,
            Data = JsonConvert.SerializeObject(new TopUpGetAllExcelConsumerReq
            {
                SearchString = topUpGetAllExcelReq.SearchString,
                CurrentUserId = userId
            })
        });
        
        
        return new StatusResp
        {
            Message = "Email Request received successfully",
        };
    }

    public async Task<TopUpGetResp> GetTopUpRequestAsync(int topUpRequestId, int userId)
    {
        var topUp = await context.TopUpRequests
            .Include(x => x.User)
            .Include(x => x.Bank)
            .FirstOrDefaultAsync(x => x.TopUpRequestId == topUpRequestId);
        if (topUp == null)
            throw new AppException("TopUp request not found");
        if (topUp.User.CreatedBy != userId && topUp.UserId != userId)
            throw new AppException("You are not authorized to view this topUp request");
        return new TopUpGetResp
        {
            TopUpRequestId = topUp.TopUpRequestId,
            Amount = topUp.Amount,
            PaymentMode = ((PaymentMode) topUp.PaymentMode).ToString(),
            PaymentModeId = topUp.PaymentMode,
            BankName = topUp.Bank != null ? $"{topUp.Bank.Name} - {topUp.Bank.AccountNumber}" : null,
            BankId = topUp.BankId,
            DepositDate = topUp.DepositDate,
            ReferenceNumber = topUp.ReferenceNumber,
            Remark = topUp.Remark,
            ImageId = topUp.ImageId,
            Status = ((TopUpRequestStatus) topUp.Status).ToString(),
            StatusId = topUp.Status,
            CreatedOn = topUp.CreatedOn,
            UpdatedOn = topUp.UpdatedOn,
            RemitterName = topUp.User.Name,
            CurrentUser = topUp.UserId == userId
        };
    }

    public async Task<StatusResp> ProcessTopUpRequestAsync(TopUpProcessReq topUpProcessReq, int userId)
    {
        if (topUpProcessReq.Status == TopUpRequestStatus.Pending)
            throw new AppException("Invalid status");
        if (topUpProcessReq.Status == TopUpRequestStatus.Rejected && string.IsNullOrEmpty(topUpProcessReq.Remark))
            throw new AppException("Remark is required for rejected status");
        var topUp = await context.TopUpRequests
            .Include(x => x.User.CreatedByNavigation)
            .FirstOrDefaultAsync(x => x.TopUpRequestId == topUpProcessReq.TopUpRequestId);
        if (topUp == null)
            throw new AppException("TopUp request not found");
        if (topUp.User.CreatedBy != userId)
            throw new AppException("You are not authorized to process this topUp request");
        if (topUp.Status != (int)TopUpRequestStatus.Pending)
            throw new AppException("TopUp request is already processed");
        if (topUp.User.CreatedByNavigation.Balance < topUp.Amount)
            throw new AppException("Insufficient balance");
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        if (topUpProcessReq.Status == TopUpRequestStatus.Approved)
        {
            topUp.Status = (int)TopUpRequestStatus.Approved;
            await context.SaveChangesAsync();
            await _walletSendEndpoint.Send(new WalletConsumerReq
            {
                TransactionType = WalletTransactionType.UserTopUp,
                Data = JsonConvert.SerializeObject(topUpProcessReq),
            });
            return new StatusResp
            {
                Message = "TopUp request approval request received, balance will be updated shortly"
            };
        }
        else
        {
            topUp.Status = (int)TopUpRequestStatus.Rejected;
            topUp.Remark = topUpProcessReq.Remark;
            topUp.UpdatedOn = dateTime.Item1;
            topUp.UpdatedOnDate = dateTime.Item2;
            await context.SaveChangesAsync();
            await _whatsappSendEndpoint.Send(new WhatsappConsumerReq
            {
                WhatsappMessageType = WhatsappMessageType.Text,
                PhoneNumber = topUp.User.Mobile,
                Message = $"TopUp request Rejected\n\nId: *{topUp.TopUpRequestId}*\nAmount: *{topUp.Amount}*\nBy: *{topUp.User.CreatedByNavigation.Name}*\nRemark: *{topUp.Remark}*"
            });
            return new StatusResp
            {
                Message = "TopUp request rejected successfully"
            };
        }
    }
}