using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Generic.Commission;
using NirvedBackend.Models.Requests.Biller;
using NirvedBackend.Models.Responses.Biller;
using NirvedBackend.Models.Responses.DropDown;

namespace NirvedBackend.Services;

public interface IBillerService
{
    Task<BillerCategoryGetResp> CreateBillerCategoryAsync(BillerCategoryCreateReq billerCategoryCreateReq);
    Task<BillerCategoryGetResp> UpdateBillerCategoryAsync(BillerCategoryUpdateReq billerCategoryUpdateReq);
    Task<BillerCategoryGetResp> ToggleBillerCategoryAsync(int billerCategoryId);
    Task<BillerCategoryGetListResp> GetBillerCategoriesAsync();
    Task<BillerCategoryDropDownListResp> GetBillerCategoriesDropDownAsync(UserType userType);
    Task<BillerGetResp> CreateBillerAsync(BillerCreateReq billerCreateReq);
    Task<BillerGetResp> UpdateBillerAsync(BillerUpdateReq billerUpdateReq);
    Task<BillerGetResp> ToggleBillerAsync(int billerId);
    Task<BillerGetAllPaginatedResp> GetAllBillerPaginatedAsync(BillerGetAllPaginatedReq billerGetAllPaginatedReq);
    Task<BillerRetailerGetAllResp> GetAllBillerRetailerAsync(int billerCategoryId, int retailerId);
    Task<BillerConfigGetResp> GetBillerConfigAsync(int billerId);
}

public class BillerService(NirvedContext context) : IBillerService
{
    #region BillerCategory

    public async Task<BillerCategoryGetResp> CreateBillerCategoryAsync(BillerCategoryCreateReq billerCategoryCreateReq)
    {
        if (await context.BillerCategories.AnyAsync(b => b.Name == billerCategoryCreateReq.BillerName))
            throw new AppException("Biller already exists");

        var billerCategory = new BillerCategory
        {
            Name = billerCategoryCreateReq.BillerName,
            IsActive = true,
            CreatedOn = DateTime.Now
        };
        var listOfAllUserIds = await context.Users.Where(x => x.UserType != (int)UserType.Admin).Select(x => x.UserId).ToListAsync();
        var listOfCommissionPercentage = listOfAllUserIds.Select(x => new CommissionPercentage
        {
            UserId = x,
            BillerCategoryId = billerCategory.BillerCategoryId,
            CreatedOn = DateTime.Now,
            PercentageJson = JsonConvert.SerializeObject(new List<CommissionBase>
            {
                new()
                {
                    From = 0,
                    To = 1000000,
                    Percentage = 0.5M
                }
            }),
            Percentage = 0.5M
        }).ToList();
        billerCategory.CommissionPercentages = listOfCommissionPercentage;
        context.BillerCategories.Add(billerCategory);
        await context.SaveChangesAsync();
        return new BillerCategoryGetResp
        {
            BillerCategoryId = billerCategory.BillerCategoryId,
            Name = billerCategory.Name,
            IsActive = billerCategory.IsActive,
            CreatedOn = billerCategory.CreatedOn
        };
    }

    public async Task<BillerCategoryGetResp> UpdateBillerCategoryAsync(BillerCategoryUpdateReq billerCategoryUpdateReq)
    {
        if (await context.BillerCategories.AnyAsync(b => b.Name == billerCategoryUpdateReq.BillerName && b.BillerCategoryId != billerCategoryUpdateReq.BillerCategoryId))
            throw new AppException("Biller already exists");

        var billerCategory = await context.BillerCategories.FirstOrDefaultAsync(x => x.BillerCategoryId == billerCategoryUpdateReq.BillerCategoryId);
        if (billerCategory == null)
            throw new AppException("Biller not found");
        billerCategory.Name = billerCategoryUpdateReq.BillerName;
        context.BillerCategories.Update(billerCategory);
        await context.SaveChangesAsync();
        return new BillerCategoryGetResp
        {
            BillerCategoryId = billerCategory.BillerCategoryId,
            Name = billerCategory.Name,
            IsActive = billerCategory.IsActive,
            CreatedOn = billerCategory.CreatedOn
        };
    }

    public async Task<BillerCategoryGetResp> ToggleBillerCategoryAsync(int billerCategoryId)
    {
        var billerCategory = await context.BillerCategories.FirstOrDefaultAsync(x => x.BillerCategoryId == billerCategoryId);
        if (billerCategory == null)
            throw new AppException("Biller not found");
        billerCategory.IsActive = !billerCategory.IsActive;
        context.BillerCategories.Update(billerCategory);
        await context.SaveChangesAsync();
        return new BillerCategoryGetResp
        {
            BillerCategoryId = billerCategory.BillerCategoryId,
            Name = billerCategory.Name,
            IsActive = billerCategory.IsActive,
            CreatedOn = billerCategory.CreatedOn
        };
    }

    public async Task<BillerCategoryGetListResp> GetBillerCategoriesAsync()
    {
        var billerCategories = await context.BillerCategories.Select(x => new BillerCategoryGetResp
        {
            BillerCategoryId = x.BillerCategoryId,
            Name = x.Name,
            IsActive = x.IsActive,
            CreatedOn = x.CreatedOn
        }).ToListAsync();
        return new BillerCategoryGetListResp
        {
            BillerCategories = billerCategories
        };
    }

    public async Task<BillerCategoryDropDownListResp> GetBillerCategoriesDropDownAsync(UserType userType)
    {
        var query = context.BillerCategories.AsNoTracking();
        if (userType != UserType.Admin)
            query = query.Where(x => x.IsActive);
        var billerCategories = await query.Select(x => new BillerCategoryDropDownBaseResp
        {
            BillerCategoryId = x.BillerCategoryId,
            Name = x.Name
        }).ToListAsync();
        return new BillerCategoryDropDownListResp
        {
            BillerCategories = billerCategories
        };
    }

    #endregion

    #region Biller

    public async Task<BillerGetResp> CreateBillerAsync(BillerCreateReq billerCreateReq)
    {
        if (await context.Billers.AnyAsync(b => b.Code == billerCreateReq.Code))
            throw new AppException("Biller code already exists");

        if (await context.Billers.AnyAsync(b => b.Name == billerCreateReq.Name))
            throw new AppException("Biller already exists");

        var billerCategory = await context.BillerCategories.FirstOrDefaultAsync(x => x.BillerCategoryId == billerCreateReq.BillerCategoryId);
        if (billerCategory == null)
            throw new AppException("Biller category not found");


        var biller = new Biller
        {
            Name = billerCreateReq.Name,
            BillerCategoryId = billerCreateReq.BillerCategoryId,
            IsActive = true,
            CreatedOn = DateTime.Now,
            Code = billerCreateReq.Code
        };

        await context.Billers.AddAsync(biller);
        await context.SaveChangesAsync();
        return new BillerGetResp
        {
            BillerId = biller.BillerId,
            Name = biller.Name,
            BillerCategory = biller.BillerCategory.Name,
            BillerCategoryId = biller.BillerCategoryId,
            IsActive = biller.IsActive,
            CreatedOn = biller.CreatedOn,
            Code = biller.Code
        };
    }

    public async Task<BillerGetResp> UpdateBillerAsync(BillerUpdateReq billerUpdateReq)
    {
        if (await context.Billers.AnyAsync(b => b.Code == billerUpdateReq.Code && b.BillerId != billerUpdateReq.BillerId))
            throw new AppException("Biller code already exists");

        if (await context.Billers.AnyAsync(b => b.Name == billerUpdateReq.Name && b.BillerId != billerUpdateReq.BillerId))
            throw new AppException("Biller already exists");

        var billerCategory = await context.BillerCategories.FirstOrDefaultAsync(x => x.BillerCategoryId == billerUpdateReq.BillerCategoryId);
        if (billerCategory == null)
            throw new AppException("Biller category not found");

        var biller = await context.Billers.FirstOrDefaultAsync(x => x.BillerId == billerUpdateReq.BillerId);
        if (biller == null)
            throw new AppException("Biller not found");

        biller.Name = billerUpdateReq.Name;
        biller.Code = billerUpdateReq.Code;
        if (biller.BillerCategoryId != billerUpdateReq.BillerCategoryId)
        {
            biller.BillerCategoryId = billerUpdateReq.BillerCategoryId;
        }

        context.Billers.Update(biller);
        await context.SaveChangesAsync();
        return new BillerGetResp
        {
            BillerId = biller.BillerId,
            Name = biller.Name,
            BillerCategory = billerCategory.Name,
            BillerCategoryId = biller.BillerCategoryId,
            IsActive = biller.IsActive,
            CreatedOn = biller.CreatedOn,
            Code = biller.Code
        };
    }

    public async Task<BillerGetResp> ToggleBillerAsync(int billerId)
    {
        var biller = await context.Billers.Include(biller => biller.BillerCategory).FirstOrDefaultAsync(x => x.BillerId == billerId);
        if (biller == null)
            throw new AppException("Biller not found");

        biller.IsActive = !biller.IsActive;
        context.Billers.Update(biller);
        await context.SaveChangesAsync();
        return new BillerGetResp
        {
            BillerId = biller.BillerId,
            Name = biller.Name,
            BillerCategory = biller.BillerCategory.Name,
            BillerCategoryId = biller.BillerCategoryId,
            IsActive = biller.IsActive,
            CreatedOn = biller.CreatedOn,
            Code = biller.Code
        };
    }

    public async Task<BillerGetAllPaginatedResp> GetAllBillerPaginatedAsync(BillerGetAllPaginatedReq billerGetAllPaginatedReq)
    {
        var query = context.Billers.Include(biller => biller.BillerCategory).AsQueryable();

        if (billerGetAllPaginatedReq.BillerCategoryId is > 0)
            query = query.Where(x => x.BillerCategoryId == billerGetAllPaginatedReq.BillerCategoryId.Value);

        if (!string.IsNullOrWhiteSpace(billerGetAllPaginatedReq.SearchString))
            query = query.Where(x => x.Name.Contains(billerGetAllPaginatedReq.SearchString) || x.Code.Contains(billerGetAllPaginatedReq.SearchString));

        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new BillerGetAllPaginatedResp
            {
                Billers = new List<BillerGetResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };

        var billers = await query
            .OrderByDescending(u => u.BillerId)
            .Skip((billerGetAllPaginatedReq.Page - 1) * billerGetAllPaginatedReq.Size)
            .Take(billerGetAllPaginatedReq.Size)
            .Select(x => new BillerGetResp
            {
                BillerId = x.BillerId,
                Name = x.Name,
                BillerCategory = x.BillerCategory.Name,
                BillerCategoryId = x.BillerCategoryId,
                IsActive = x.IsActive,
                CreatedOn = x.CreatedOn,
                Code = x.Code
            }).ToListAsync();

        return new BillerGetAllPaginatedResp
        {
            Billers = billers,
            PageCount = (int)Math.Ceiling((double)totalRecords / billerGetAllPaginatedReq.Size),
            PageNumber = billerGetAllPaginatedReq.Page,
            PageSize = billerGetAllPaginatedReq.Size,
            TotalCount = totalRecords
        };
    }

    public async Task<BillerRetailerGetAllResp> GetAllBillerRetailerAsync(int billerCategoryId, int retailerId)
    {
        var billerCategory = await context.BillerCategories.Include(billerCategory => billerCategory.Billers).ThenInclude(biller => biller.BillerInfo.CityNavigation.State).Where(x => x.BillerCategoryId == billerCategoryId).FirstOrDefaultAsync();
        var retailer = await context.Users.Where(x => x.UserId == retailerId).FirstAsync();
        if (billerCategory == null)
            throw new AppException("Biller category not found");
        if (billerCategory.IsActive == false)
            throw new AppException("Biller category is not active");
        if (billerCategory.Billers == null || billerCategory.Billers.Count == 0)
            return new BillerRetailerGetAllResp
            {
                Billers = new List<BillerRetailerBaseGetResp>(),
                DefaultBillerId = 0
            };
        var billers = billerCategory.Billers.Where(b => b.IsActive).Select(x => new BillerRetailerBaseGetResp
        {
            BillerId = x.BillerId,
            Name = x.Name,
            BillerConfig = x.BillerInfo != null
                ? new BillerConfigGetResp
                {
                    Fetching = x.BillerInfo.Fetching,
                    FieldsData = x.BillerInfo.FieldsData,
                    CityAndState = x.BillerInfo.City != null ? x.BillerInfo.CityNavigation.Name + ", " + x.BillerInfo.CityNavigation.State.Name : "",
                    CityId = x.BillerInfo.City
                }
                : null
        }).ToList();
        var defaultBiller = billers.Where(x => x.BillerConfig is { CityId: not null } && x.BillerConfig.CityId == retailer.CityId).MinBy(x => x.BillerId) ?? billers.First();
        return new BillerRetailerGetAllResp
        {
            Billers = billers,
            DefaultBillerId = defaultBiller.BillerId
        };
    }

    public async Task<BillerConfigGetResp> GetBillerConfigAsync(int billerId)
    {
        var biller = await context.Billers.Include(b => b.BillerInfo).ThenInclude(billerInfo => billerInfo.CityNavigation.State).FirstOrDefaultAsync(b => b.BillerId == billerId);
        if (biller == null)
            throw new AppException("Biller not found");
        if (biller.BillerInfo == null)
            return new BillerConfigGetResp
            {
                Fetching = false,
                FieldsData = "",
                CityAndState = "",
                CityId = 0
            };
        return new BillerConfigGetResp
        {
            Fetching = biller.BillerInfo.Fetching,
            FieldsData = biller.BillerInfo.FieldsData,
            CityAndState = biller.BillerInfo.City != null ? biller.BillerInfo.CityNavigation.Name + ", " + biller.BillerInfo.CityNavigation.State.Name : "",
            CityId = biller.BillerInfo.City
        };
    }

    #endregion
}