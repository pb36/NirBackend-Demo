using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Requests.UserNotice;
using NirvedBackend.Models.Responses.UserNotice;

namespace NirvedBackend.Services;

public interface IUserNoticeService
{
    Task<UserNoticeGetResp> CreateUserNoticeAsync(UserNoticeCreateReq req);
    Task<UserNoticeGetResp> UpdateUserNoticeAsync(UserNoticeUpdateReq req);
    Task<UserNoticeGetResp> ToggleUserNoticeAsync(int userNoticeId);
    Task DeleteUserNoticeAsync(int userNoticeId);
    Task<UserNoticeGetAllPaginatedResp> GetUserNoticesPaginatedAsync(UserNoticeGetAllPaginatedReq req);
    Task<UserNoticeGetAllPaginatedResp> GetUserNoticesActivePaginatedAsync(UserNoticeGetAllPaginatedReq req);
    Task<UserNoticeGetDashboardResp> GetUserNoticesDashboardAsync();
}

public class UserNoticeService(NirvedContext context) : IUserNoticeService
{
    public async Task<UserNoticeGetResp> CreateUserNoticeAsync(UserNoticeCreateReq req)
    {
        if (req.StartDate.ToDateTime(TimeOnly.MinValue) < DateTime.Now.Date)
            throw new AppException("Start date cannot be in the past");
        
        if (req.EndDate.ToDateTime(TimeOnly.MinValue) < DateTime.Now.Date)
            throw new AppException("End date cannot be in the past");
        
        if (req.EndDate < req.StartDate)
            throw new AppException("End date cannot be before start date");
        
        var userNotice = new UserNotice
        {
            Title = req.Title,
            Message = req.Message,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            IsActive = true,
            CreatedOn = DateTime.Now,
        };

        await context.UserNotices.AddAsync(userNotice);
        await context.SaveChangesAsync();

        return new UserNoticeGetResp
        {
            UserNoticeId = userNotice.UserNoticeId,
            Title = userNotice.Title,
            Message = userNotice.Message,
            StartDate = userNotice.StartDate,
            EndDate = userNotice.EndDate,
            IsActive = userNotice.IsActive,
            CreatedOn = userNotice.CreatedOn,
            UpdatedOn = userNotice.UpdatedOn
        };
    }

    public async Task<UserNoticeGetResp> UpdateUserNoticeAsync(UserNoticeUpdateReq req)
    {
        if (req.StartDate.ToDateTime(TimeOnly.MinValue) < DateTime.Now.Date)
            throw new AppException("Start date cannot be in the past");
        
        if (req.EndDate.ToDateTime(TimeOnly.MinValue) < DateTime.Now.Date)
            throw new AppException("End date cannot be in the past");
        
        if (req.EndDate < req.StartDate)
            throw new AppException("End date cannot be before start date");
        
        var userNotice = await context.UserNotices.FindAsync(req.UserNoticeId);
        if (userNotice == null)
            throw new AppException("User notice not found");
        
        userNotice.Title = req.Title;
        userNotice.Message = req.Message;
        userNotice.StartDate = req.StartDate;
        userNotice.EndDate = req.EndDate;
        userNotice.UpdatedOn = DateTime.Now;
        
        await context.SaveChangesAsync();
        
        return new UserNoticeGetResp
        {
            UserNoticeId = userNotice.UserNoticeId,
            Title = userNotice.Title,
            Message = userNotice.Message,
            StartDate = userNotice.StartDate,
            EndDate = userNotice.EndDate,
            IsActive = userNotice.IsActive,
            CreatedOn = userNotice.CreatedOn,
            UpdatedOn = userNotice.UpdatedOn
        };
        
    }

    public async Task<UserNoticeGetResp> ToggleUserNoticeAsync(int userNoticeId)
    {
        var userNotice = await context.UserNotices.FindAsync(userNoticeId);
        if (userNotice == null)
            throw new AppException("User notice not found");
        
        userNotice.IsActive = !userNotice.IsActive;
        userNotice.UpdatedOn = DateTime.Now;
        
        await context.SaveChangesAsync();
        
        return new UserNoticeGetResp
        {
            UserNoticeId = userNotice.UserNoticeId,
            Title = userNotice.Title,
            Message = userNotice.Message,
            StartDate = userNotice.StartDate,
            EndDate = userNotice.EndDate,
            IsActive = userNotice.IsActive,
            CreatedOn = userNotice.CreatedOn,
            UpdatedOn = userNotice.UpdatedOn
        };
    }

    public async Task DeleteUserNoticeAsync(int userNoticeId)
    {
        await context.UserNotices.Where(x => x.UserNoticeId == userNoticeId).ExecuteDeleteAsync();
    }

    public async Task<UserNoticeGetAllPaginatedResp> GetUserNoticesPaginatedAsync(UserNoticeGetAllPaginatedReq req)
    {
        var query = context.UserNotices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.SearchString))
            query = query.Where(u => u.Title.Contains(req.SearchString) || 
                                     u.Message.Contains(req.SearchString));
        
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new UserNoticeGetAllPaginatedResp
            {
                UserNotices = new List<UserNoticeGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };

        var userNotices = await query
            .OrderByDescending(u => u.UserNoticeId)
            .Skip((req.Page - 1) * req.Size)
            .Take(req.Size)
            .Select(x => new UserNoticeGetResp
            {
                UserNoticeId = x.UserNoticeId,
                Title = x.Title,
                Message = x.Message,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive,
                CreatedOn = x.CreatedOn,
                UpdatedOn = x.UpdatedOn
            }).ToListAsync();

        return new UserNoticeGetAllPaginatedResp
        {
            UserNotices = userNotices,
            PageCount = (int)Math.Ceiling(totalRecords / (double)req.Size),
            PageNumber = req.Page,
            PageSize = req.Size,
            TotalCount = totalRecords
        };
    }

    public async Task<UserNoticeGetAllPaginatedResp> GetUserNoticesActivePaginatedAsync(UserNoticeGetAllPaginatedReq req)
    {
        var query = context.UserNotices.AsQueryable();

        query = query.Where(u => u.IsActive);
        
        if (!string.IsNullOrWhiteSpace(req.SearchString))
            query = query.Where(u => u.Title.Contains(req.SearchString) || 
                                     u.Message.Contains(req.SearchString));
        
        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new UserNoticeGetAllPaginatedResp
            {
                UserNotices = new List<UserNoticeGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };

        var userNotices = await query
            .OrderByDescending(u => u.UserNoticeId)
            .Skip((req.Page - 1) * req.Size)
            .Take(req.Size)
            .Select(x => new UserNoticeGetResp
            {
                UserNoticeId = x.UserNoticeId,
                Title = x.Title,
                Message = x.Message,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive,
                CreatedOn = x.CreatedOn,
                UpdatedOn = x.UpdatedOn
            }).ToListAsync();

        return new UserNoticeGetAllPaginatedResp
        {
            UserNotices = userNotices,
            PageCount = (int)Math.Ceiling(totalRecords / (double)req.Size),
            PageNumber = req.Page,
            PageSize = req.Size,
            TotalCount = totalRecords
        };
    }

    public async Task<UserNoticeGetDashboardResp> GetUserNoticesDashboardAsync()
    {
        var today=DateOnly.FromDateTime(DateTime.Now);
        var userNotices = await context.UserNotices
            .Where(x => x.IsActive && x.StartDate <= today && x.EndDate >= today)
            .OrderByDescending(x => x.UserNoticeId)
            .Take(5)
            .Select(x => new UserNoticeGetResp
            {
                UserNoticeId = x.UserNoticeId,
                Title = x.Title,
                Message = x.Message,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive,
                CreatedOn = x.CreatedOn,
                UpdatedOn = x.UpdatedOn
            }).ToListAsync();

        return new UserNoticeGetDashboardResp
        {
            UserNotices = userNotices
        };
    }
}