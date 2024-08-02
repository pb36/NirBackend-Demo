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
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Models.Requests.Ledger;
using NirvedBackend.Models.Responses.Ledger;

namespace NirvedBackend.Services;

public interface ILedgerService
{
    Task<LedgerGetAllPaginatedResp> GetAllPaginatedAsync(LedgerGetAllPaginatedReq ledgerGetAllPaginatedReq,int currentUserId, UserType userType);
    Task<StatusResp> GetAllExcelAsync(LedgerGetAllExcelReq ledgerGetAllExcelReq, int currentUserId,UserType currentUserType);
}

public class LedgerService(NirvedContext context,ISendEndpointProvider bus) : ILedgerService
{
    private readonly ISendEndpoint _emailSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/" + RabbitQueues.EmailQueue)).Result;

    public async Task<LedgerGetAllPaginatedResp> GetAllPaginatedAsync(LedgerGetAllPaginatedReq ledgerGetAllPaginatedReq, int currentUserId, UserType userType)
    {
        var query= context.Ledgers.AsQueryable();
        switch (ledgerGetAllPaginatedReq.DateRange)
        {
            case PaginatedDateRange.Today:
                query = query.Where(x => x.CreatedOnDate == DateOnly.FromDateTime(DateTime.Now));
                break;
            case PaginatedDateRange.Month:
                ledgerGetAllPaginatedReq.StartDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
                ledgerGetAllPaginatedReq.EndDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)));
                query = query.Where(x => x.CreatedOnDate >= ledgerGetAllPaginatedReq.StartDate && 
                    x.CreatedOnDate <= ledgerGetAllPaginatedReq.EndDate);
                break;
            case PaginatedDateRange.Custom:
                if (ledgerGetAllPaginatedReq.StartDate == null || ledgerGetAllPaginatedReq.EndDate == null)
                {
                    throw new AppException("Start date and end date must be provided when date range is custom");
                }
                if (ledgerGetAllPaginatedReq.StartDate > ledgerGetAllPaginatedReq.EndDate)
                {
                    throw new AppException("Start date must be less than end date");
                }
                if (ledgerGetAllPaginatedReq.EndDate.Value.AddDays(-90) > ledgerGetAllPaginatedReq.StartDate)
                {
                    throw new AppException("Date range must be less than or equal to 90 days");
                }
                query = query.Where(x => x.CreatedOnDate >= ledgerGetAllPaginatedReq.StartDate && x.CreatedOnDate <= ledgerGetAllPaginatedReq.EndDate);
                break;
            default:
                ledgerGetAllPaginatedReq.StartDate = DateOnly.FromDateTime(DateTime.Today);
                break;
        }

        query = userType switch
        {
            UserType.SuperDistributor or UserType.Distributor => query.Where(x => x.UserId == currentUserId || x.User.CreatedBy == currentUserId),
            UserType.Retailer => query.Where(x => x.UserId == currentUserId),
            _ => query
        };
        if (!string.IsNullOrEmpty(ledgerGetAllPaginatedReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(ledgerGetAllPaginatedReq.SearchString) || x.User.Mobile.StartsWith(ledgerGetAllPaginatedReq.SearchString));
        }
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new LedgerGetAllPaginatedResp
            {
                Ledgers = new List<LedgerGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };
        
        var ledgers = await query.OrderByDescending(u => u.LedgerId)
            .Skip((ledgerGetAllPaginatedReq.Page - 1) * ledgerGetAllPaginatedReq.Size)
            .Take(ledgerGetAllPaginatedReq.Size)
            .Select(x => new LedgerGetResp
            {

                CreatedOn = x.CreatedOn,
                Amount = x.Amount,
                CreditRequestId = x.CreditRequestId,
                Closing = x.Closing,
                Name = x.User.Name,
                Remark = x.Remark,
                Opening = x.Opening,
                Type = ((TransactionType) x.Type).ToString(),
                TypeId = x.Type,
                TopUpRequestId = x.TopUpRequestId,
                BillId = x.BillId,
                LedgerId = x.LedgerId
            }).ToListAsync();

        return new LedgerGetAllPaginatedResp
        {
            Ledgers = ledgers,
            PageCount = (int)Math.Ceiling((decimal)totalRecords / ledgerGetAllPaginatedReq.Size),
            PageNumber = ledgerGetAllPaginatedReq.Page,
            PageSize = ledgerGetAllPaginatedReq.Size,
            TotalCount = totalRecords
        };
    }
    
    public async Task<StatusResp> GetAllExcelAsync(LedgerGetAllExcelReq ledgerGetAllExcelReq, int currentUserId, UserType userType)
    {
        throw new AppException("This functionality is only available in production copy, please contact support @ +918866605050");
        var query= context.Ledgers.AsQueryable();
        switch (ledgerGetAllExcelReq.DateRange)
        {
            case PaginatedDateRange.Today:
                query = query.Where(x => x.CreatedOnDate == DateOnly.FromDateTime(DateTime.Now));
                break;
            case PaginatedDateRange.Month:
                throw new AppException("Please use custom date range for max weekly report of data");
                ledgerGetAllExcelReq.StartDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
                ledgerGetAllExcelReq.EndDate = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)));
                query = query.Where(x => x.CreatedOnDate >= ledgerGetAllExcelReq.StartDate && 
                    x.CreatedOnDate <= ledgerGetAllExcelReq.EndDate);
                break;
            case PaginatedDateRange.Custom:
                if (ledgerGetAllExcelReq.StartDate == null || ledgerGetAllExcelReq.EndDate == null)
                {
                    throw new AppException("Start date and end date must be provided when date range is custom");
                }
                if (ledgerGetAllExcelReq.StartDate > ledgerGetAllExcelReq.EndDate)
                {
                    throw new AppException("Start date must be less than end date");
                }
                if (ledgerGetAllExcelReq.EndDate.Value.AddDays(-7) > ledgerGetAllExcelReq.StartDate)
                {
                    throw new AppException("Date range must be less than or equal to 7 days");
                }
                query = query.Where(x => x.CreatedOnDate >= ledgerGetAllExcelReq.StartDate && x.CreatedOnDate <= ledgerGetAllExcelReq.EndDate);
                break;
            default:
                ledgerGetAllExcelReq.StartDate = DateOnly.FromDateTime(DateTime.Today);
                break;
        }

        query = userType switch
        {
            UserType.SuperDistributor or UserType.Distributor => query.Where(x => x.UserId == currentUserId || x.User.CreatedBy == currentUserId),
            UserType.Retailer => query.Where(x => x.UserId == currentUserId),
            _ => query
        };
        if (!string.IsNullOrEmpty(ledgerGetAllExcelReq.SearchString))
        {
            query = query.Where(x => x.User.Name.StartsWith(ledgerGetAllExcelReq.SearchString) || x.User.Mobile.StartsWith(ledgerGetAllExcelReq.SearchString));
        }
        
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            throw new AppException("No records found");
        
        await _emailSendEndpoint.Send(new EmailConsumerReq
        {
            EmailSendType = EmailSendType.LedgerGetAllExcel,
            Data = JsonConvert.SerializeObject(new LedgerGetAllExcelConsumerReq
            {
                UserType = userType,
                SearchString = ledgerGetAllExcelReq.SearchString,
                CurrentUserId = currentUserId,
                DateRange = ledgerGetAllExcelReq.DateRange,
                StartDate = ledgerGetAllExcelReq.StartDate,
                EndDate = ledgerGetAllExcelReq.EndDate
            })
        });

        return new StatusResp
        {
            Message = "Email Request received successfully",
        };

    }
}
