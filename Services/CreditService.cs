using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Consumer;
using NirvedBackend.Models.Requests.Credit;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Models.Responses.Credit;

namespace NirvedBackend.Services;

public interface ICreditService
{
    Task<CreditGetResp> AddCreditRequestAsync(CreditAddReq creditAddReq,int userId);
    Task<CreditGetAllPaginatedResp> GetAllCreditRequestsAsync(CreditGetAllPaginatedReq creditGetAllPaginatedReq,int userId);
    Task<StatusResp> GetAllExcelAsync(CreditGetAllExcelReq creditGetAllExcelReq,int userId);
    Task<CreditGetResp> GetCreditRequestAsync(int creditRequestId,int userId);
    Task<StatusResp> ProcessCreditRequestAsync(CreditProcessReq creditProcessReq,int userId);
    Task<OutstandingGetAllPaginatedResp> GetAllOutstandingRequestsAsync(OutstandingGetAllPaginatedReq outstandingGetAllPaginatedReq,int userId);
    Task<StatusResp> GetAllExcelAsync(OutstandingGetAllExcelReq outstandingGetAllExcelReq,int userId);
    Task<StatusResp> ClearOutstandingAsync(OutstandingClearReq outstandingClearReq,int userId);
    Task<OutstandingGetResp> GetOutstandingAsync(int outstandingId,int userId);
    Task<OutstandingGetResp> GetSelfOutstandingAsync(int userId);
    Task<OutstandingLedgerGetAllPaginatedResp> GetAllOutstandingLedgerAsync(OutstandingLedgerGetAllPaginatedReq outstandingLedgerGetAllPaginatedReq,int userId);
    Task<OutstandingLedgerGetAllPaginatedResp> GetSelfOutstandingLedgerAsync(OutstandingLedgerSelfGetAllPaginatedReq selfGetAllPaginatedReq,int userId);
}

public class CreditService(NirvedContext context,ISendEndpointProvider bus) : ICreditService
{
    private readonly ISendEndpoint _walletSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.WalletQueue)).Result;
    private readonly ISendEndpoint _whatsappSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.WhatsappMessageQueue)).Result;
    private readonly ISendEndpoint _emailSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/" + RabbitQueues.EmailQueue)).Result;



    public async Task<CreditGetResp> AddCreditRequestAsync(CreditAddReq creditAddReq,int userId)
    {
       if (await context.CreditRequests.AnyAsync(x => x.UserId == userId && x.Status == (int)CreditRequestStatus.Pending))
           throw new AppException("You already have a pending request for credit");

       var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var creditRequest = new CreditRequest
        {
            Amount = creditAddReq.Amount,
            Status = (int)CreditRequestStatus.Pending,
            CreatedOn = dateTime.Item1,
            CreatedOnDate = dateTime.Item2,
            UserId = userId,
        };

        await context.CreditRequests.AddAsync(creditRequest);
        await context.SaveChangesAsync();
        creditRequest.User = await context.Users.Include(x => x.CreatedByNavigation).FirstAsync(x => x.UserId == userId);
        
        await _whatsappSendEndpoint.Send(new WhatsappConsumerReq
        {
            WhatsappMessageType = WhatsappMessageType.Text,
            PhoneNumber = creditRequest.User.CreatedByNavigation.Mobile,
            Message = $"Credit request Received\n\nId: *{creditRequest.CreditRequestId}*\nAmount: *{creditRequest.Amount}*\nFrom: *{creditRequest.User.Name}*"
        });
        
        return new CreditGetResp
        {
            CreditRequestId = creditRequest.CreditRequestId,
            Amount = creditRequest.Amount,
            Status = ((CreditRequestStatus) creditRequest.Status).ToString(),
            StatusId = creditRequest.Status,
            RemitterName = creditRequest.User.Name,
            Remark = creditRequest.Remark,
            CreatedOn = creditRequest.CreatedOn,
            UpdatedOn = creditRequest.UpdatedOn,
            CurrentUser = creditRequest.UserId == userId
        };
    }

    public async Task<CreditGetAllPaginatedResp> GetAllCreditRequestsAsync(CreditGetAllPaginatedReq creditGetAllPaginatedReq, int userId)
    {
        var query = context.CreditRequests.Where(x => x.User.CreatedBy == userId ||
                                                     x.UserId==userId).AsNoTracking();
        if (!string.IsNullOrEmpty(creditGetAllPaginatedReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(creditGetAllPaginatedReq.SearchString) ||
                                     x.User.Email.StartsWith(creditGetAllPaginatedReq.SearchString) ||
                                     x.User.Username.StartsWith(creditGetAllPaginatedReq.SearchString) ||
                                     x.User.Mobile.StartsWith(creditGetAllPaginatedReq.SearchString));
        }
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new CreditGetAllPaginatedResp
            {
                CreditRequests = new List<CreditGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };
        
        var creditRequests = await query
            .OrderByDescending(u => u.CreditRequestId)
            .Skip((creditGetAllPaginatedReq.Page - 1) * creditGetAllPaginatedReq.Size)
            .Take(creditGetAllPaginatedReq.Size)
            .Select(x => new CreditGetResp
            {
                CreditRequestId = x.CreditRequestId,
                Amount = x.Amount,
                Status = ((CreditRequestStatus) x.Status).ToString(),
                StatusId = x.Status,
                RemitterName = x.User.Name,
                Remark = x.Remark,
                CreatedOn = x.CreatedOn,
                UpdatedOn = x.UpdatedOn,
                CurrentUser = x.UserId == userId
            }).ToListAsync();
        
        return new CreditGetAllPaginatedResp
        {
            CreditRequests = creditRequests,
            PageCount = (int)Math.Ceiling(totalRecords / (double)creditGetAllPaginatedReq.Size),
            PageNumber = creditGetAllPaginatedReq.Page,
            PageSize = creditGetAllPaginatedReq.Size,
            TotalCount = totalRecords
        };
    }

    public async Task<StatusResp> GetAllExcelAsync(CreditGetAllExcelReq creditGetAllExcelReq, int userId)
    {
        throw new AppException("This functionality is only available in production copy, please contact support @ +918866605050");
        var query = context.CreditRequests.Where(x => x.User.CreatedBy == userId ||
                                                      x.UserId==userId).AsNoTracking();
        if (!string.IsNullOrEmpty(creditGetAllExcelReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(creditGetAllExcelReq.SearchString) ||
                                     x.User.Email.StartsWith(creditGetAllExcelReq.SearchString) ||
                                     x.User.Username.StartsWith(creditGetAllExcelReq.SearchString) ||
                                     x.User.Mobile.StartsWith(creditGetAllExcelReq.SearchString));
        }
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            throw new AppException("No records found");
        
        await _emailSendEndpoint.Send(new EmailConsumerReq
        {
            EmailSendType = EmailSendType.CreditGetAllExcel,
            Data = JsonConvert.SerializeObject(new CreditGetAllExcelConsumerReq
            {
                CurrentUserId = userId,
                SearchString = creditGetAllExcelReq.SearchString,
            })
        });
        
        return new StatusResp
        {
            Message = "Email Request received successfully",
        };
    }

    public async Task<CreditGetResp> GetCreditRequestAsync(int creditRequestId, int userId)
    {
        var creditRequest = await context.CreditRequests
            .Include(x => x.User.CreatedByNavigation)
            .FirstOrDefaultAsync(x => x.CreditRequestId == creditRequestId);
        if (creditRequest == null)
            throw new AppException("Credit request not found");
        if (creditRequest.User.CreatedBy != userId && creditRequest.UserId != userId)
            throw new AppException("You are not authorized to view this credit request");
        return new CreditGetResp
        {
            CreditRequestId = creditRequest.CreditRequestId,
            Amount = creditRequest.Amount,
            Status = ((CreditRequestStatus) creditRequest.Status).ToString(),
            StatusId = creditRequest.Status,
            RemitterName = creditRequest.User.Name,
            Remark = creditRequest.Remark,
            CreatedOn = creditRequest.CreatedOn,
            UpdatedOn = creditRequest.UpdatedOn,
            CurrentUser = creditRequest.UserId == userId
        };
    }

    public async Task<StatusResp> ProcessCreditRequestAsync(CreditProcessReq creditProcessReq, int userId)
    {
        if (creditProcessReq.Status == CreditRequestStatus.Pending)
            throw new AppException("Invalid status");
        if (creditProcessReq.Status == CreditRequestStatus.Rejected && string.IsNullOrEmpty(creditProcessReq.Remark))
            throw new AppException("Remark is required for rejected status");
        var creditRequest = await context.CreditRequests
            .Include(x => x.User.CreatedByNavigation)
            .FirstOrDefaultAsync(x => x.CreditRequestId == creditProcessReq.CreditRequestId);
        if (creditRequest == null)
            throw new AppException("Credit request not found");
        if (creditRequest.User.CreatedBy != userId)
            throw new AppException("You are not authorized to process this credit request");
        if (creditRequest.Status != (int)CreditRequestStatus.Pending)
            throw new AppException("Credit request is already processed");
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        if (creditProcessReq.Status == CreditRequestStatus.Approved)
        {
            if (creditRequest.User.CreatedByNavigation.Balance < creditRequest.Amount)
                throw new AppException("Insufficient balance");
            creditRequest.Status = (int)CreditRequestStatus.Approved;
            await context.SaveChangesAsync();
            await _walletSendEndpoint.Send(new WalletConsumerReq
            {
                TransactionType = WalletTransactionType.CreditApproval,
                Data = JsonConvert.SerializeObject(creditProcessReq),
            });
            return new StatusResp
            {
                Message = "Credit request approval request received, balance will be updated shortly"
            };
        }
        else
        {
            creditRequest.Status = (int)CreditRequestStatus.Rejected;
            creditRequest.Remark = creditProcessReq.Remark;
            creditRequest.UpdatedOn = dateTime.Item1;
            creditRequest.UpdatedOnDate = dateTime.Item2;
            await context.SaveChangesAsync();
            await _whatsappSendEndpoint.Send(new WhatsappConsumerReq
            {
                WhatsappMessageType = WhatsappMessageType.Text,
                PhoneNumber = creditRequest.User.Mobile,
                Message = $"Credit request Rejected\n\nId: *{creditRequest.CreditRequestId}*\nAmount: *{creditRequest.Amount}*\nBy: *{creditRequest.User.CreatedByNavigation.Name}*\nRemark: *{creditRequest.Remark}*"
            });
            return new StatusResp
            {
                Message = "Credit request rejected successfully"
            };
        }
    }

    public async Task<OutstandingGetAllPaginatedResp> GetAllOutstandingRequestsAsync(OutstandingGetAllPaginatedReq outstandingGetAllPaginatedReq, int userId)
    {
        var query = context.Outstandings.Where(x => x.User.CreatedBy == userId).AsNoTracking();
        if (!string.IsNullOrEmpty(outstandingGetAllPaginatedReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(outstandingGetAllPaginatedReq.SearchString) ||
                                     x.User.Email.StartsWith(outstandingGetAllPaginatedReq.SearchString) ||
                                     x.User.Username.StartsWith(outstandingGetAllPaginatedReq.SearchString) ||
                                     x.User.Mobile.StartsWith(outstandingGetAllPaginatedReq.SearchString));
        }
        
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new OutstandingGetAllPaginatedResp
            {
                Outstanding = new List<OutstandingGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };
        
        var outstanding = await query.OrderByDescending(u => u.Amount)
            .Skip((outstandingGetAllPaginatedReq.Page - 1) * outstandingGetAllPaginatedReq.Size)
            .Take(outstandingGetAllPaginatedReq.Size)
            .Select(x => new OutstandingGetResp
            {
                OutstandingId = x.OutstandingId,
                UserId = x.UserId,
                Name = x.User.Name,
                Mobile = x.User.Mobile,
                OutstandingAmount = x.Amount,
            }).ToListAsync();

        return new OutstandingGetAllPaginatedResp
        {
            Outstanding = outstanding,
            PageCount = (int)Math.Ceiling(totalRecords / (double)outstandingGetAllPaginatedReq.Size),
            PageNumber = outstandingGetAllPaginatedReq.Page,
            PageSize = outstandingGetAllPaginatedReq.Size,
            TotalCount = totalRecords
        };
    }

    public async Task<StatusResp> GetAllExcelAsync(OutstandingGetAllExcelReq outstandingGetAllExcelReq, int userId)
    {
        throw new AppException("This functionality is only available in production copy, please contact support @ +918866605050");
        var query = context.Outstandings.Where(x => x.User.CreatedBy == userId).AsNoTracking();
        if (!string.IsNullOrEmpty(outstandingGetAllExcelReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(outstandingGetAllExcelReq.SearchString) ||
                                     x.User.Email.StartsWith(outstandingGetAllExcelReq.SearchString) ||
                                     x.User.Username.StartsWith(outstandingGetAllExcelReq.SearchString) ||
                                     x.User.Mobile.StartsWith(outstandingGetAllExcelReq.SearchString));
        }
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            throw new AppException("No records found");
        
        await _emailSendEndpoint.Send(new EmailConsumerReq
        {
            EmailSendType = EmailSendType.OutstandingGetAllExcel,
            Data = JsonConvert.SerializeObject(new OutstandingGetAllExcelConsumerReq
            {
                CurrentUserId = userId,
                SearchString = outstandingGetAllExcelReq.SearchString,
            })
        });
        
        return new StatusResp
        {
            Message = "Email Request received successfully",
        };
    }

    public async Task<StatusResp> ClearOutstandingAsync(OutstandingClearReq outstandingClearReq, int userId)
    {
        var outstanding = await context.Outstandings
            .Include(x => x.User.CreatedByNavigation)
            .FirstOrDefaultAsync(x => x.OutstandingId == outstandingClearReq.OutstandingId);
        if (outstanding == null)
            throw new AppException("Outstanding not found");
        if (outstanding.User.CreatedBy != userId)
            throw new AppException("You are not authorized to clear this outstanding");
        if (outstanding.Amount < outstandingClearReq.Amount)
            throw new AppException("Amount cannot be greater than outstanding amount");
        await _walletSendEndpoint.Send(new WalletConsumerReq
            
        {
            TransactionType = WalletTransactionType.OutstandingClear,
            Data = JsonConvert.SerializeObject(outstandingClearReq),
        });
        return new StatusResp
        {
            Message = "Outstanding clear request received, balance will be updated shortly"
        };
    }

    public async Task<OutstandingGetResp> GetOutstandingAsync(int outstandingId, int userId)
    {
        var outstanding = await context.Outstandings
            .Include(x => x.User.CreatedByNavigation)
            .FirstOrDefaultAsync(x => x.OutstandingId == outstandingId);
        if (outstanding == null)
            throw new AppException("Outstanding not found");
        if (outstanding.User.CreatedBy != userId)
            throw new AppException("You are not authorized to view this outstanding");
        return new OutstandingGetResp
        {
            OutstandingId = outstanding.OutstandingId,
            UserId = outstanding.UserId,
            Name = outstanding.User.Name,
            Mobile = outstanding.User.Mobile,
            OutstandingAmount = outstanding.Amount,
        };
    }

    public async Task<OutstandingGetResp> GetSelfOutstandingAsync(int userId)
    {
        var outstanding = await context.Outstandings.Include(outstanding => outstanding.User).ThenInclude(user => user.CreatedByNavigation)
            .FirstOrDefaultAsync(x => x.UserId == userId);
        if (outstanding == null)
            throw new AppException("Outstanding not found");
        return new OutstandingGetResp
        {
            OutstandingId = outstanding.OutstandingId,
            UserId = outstanding.UserId,
            Name = outstanding.User.CreatedByNavigation.Name,
            Mobile = outstanding.User.CreatedByNavigation.Mobile,
            OutstandingAmount = outstanding.Amount,
        };
    }

    public async Task<OutstandingLedgerGetAllPaginatedResp> GetAllOutstandingLedgerAsync(OutstandingLedgerGetAllPaginatedReq outstandingLedgerGetAllPaginatedReq, int userId)
    {
        if (await context.Outstandings.AnyAsync(x => x.OutstandingId == outstandingLedgerGetAllPaginatedReq.OutstandingId && x.User.CreatedBy == userId)==false)
            throw new AppException("You are not authorized to view this outstanding ledger");
        
        var query = context.OutstandingLedgers.Where(x => x.OutstandingId == outstandingLedgerGetAllPaginatedReq.OutstandingId).AsNoTracking();
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new OutstandingLedgerGetAllPaginatedResp
            {
                OutstandingLedgers = new List<OutstandingLedgerGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };
        
        var outstandingLedgers = await query.OrderByDescending(u => u.OutstandingLedgerId).
            Skip((outstandingLedgerGetAllPaginatedReq.Page - 1) * outstandingLedgerGetAllPaginatedReq.Size)
            .Take(outstandingLedgerGetAllPaginatedReq.Size)
            .Select(x => new OutstandingLedgerGetResp
            {
                OutstandingLedgerId = x.OutstandingLedgerId,
                Amount = x.Amount,
                Opening = x.Opening,
                Closing = x.Closing,
                TransactionType = ((TransactionType) x.Type).ToString(),
                TransactionTypeId = x.Type,
                TransactionDate = x. CreatedOn,
                Remark = x.Remark
            }).ToListAsync();

        return new OutstandingLedgerGetAllPaginatedResp
        {
            OutstandingLedgers = outstandingLedgers,
            PageCount = (int)Math.Ceiling(totalRecords / (double)outstandingLedgerGetAllPaginatedReq.Size),
            PageNumber = outstandingLedgerGetAllPaginatedReq.Page,
            PageSize = outstandingLedgerGetAllPaginatedReq.Size,
            TotalCount = totalRecords
        };
    }

    public async Task<OutstandingLedgerGetAllPaginatedResp> GetSelfOutstandingLedgerAsync(OutstandingLedgerSelfGetAllPaginatedReq selfGetAllPaginatedReq, int userId)
    {
        var query = context.OutstandingLedgers.Where(x => x.Outstanding.UserId == userId).AsNoTracking();
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new OutstandingLedgerGetAllPaginatedResp
            {
                OutstandingLedgers = new List<OutstandingLedgerGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };
        
        var outstandingLedgers = await query.OrderByDescending(u => u.OutstandingLedgerId).
            Skip((selfGetAllPaginatedReq.Page - 1) * selfGetAllPaginatedReq.Size)
            .Take(selfGetAllPaginatedReq.Size)
            .Select(x => new OutstandingLedgerGetResp
            {
                OutstandingLedgerId = x.OutstandingLedgerId,
                Amount = x.Amount,
                Opening = x.Opening,
                Closing = x.Closing,
                TransactionType = ((TransactionType) x.Type).ToString(),
                TransactionTypeId = x.Type,
                TransactionDate = x. CreatedOn,
                Remark = x.Remark
            }).ToListAsync();

        return new OutstandingLedgerGetAllPaginatedResp
        {
            OutstandingLedgers = outstandingLedgers,
            PageCount = (int)Math.Ceiling(totalRecords / (double)selfGetAllPaginatedReq.Size),
            PageNumber = selfGetAllPaginatedReq.Page,
            PageSize = selfGetAllPaginatedReq.Size,
            TotalCount = totalRecords
        };
    }
}