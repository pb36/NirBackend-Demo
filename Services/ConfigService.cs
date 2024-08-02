using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Config;
using NirvedBackend.Models.Responses.Config;

namespace NirvedBackend.Services;

public interface IConfigService
{
    Task<StatusResp> UpdateTxnPasswordAsync(ConfigUpdateTxnPasswordReq configUpdateTxnPasswordReq);
    Task<ConfigGetServerTimeResp> UpdateServerTimeAsync(ConfigUpdateServerTimeReq configUpdateServerTimeReq);
    Task<ConfigGetServerTimeResp> GetServerTimeAsync();
    Task<ConfigGetAutoCommissionResp> GetAutoCommissionAsync();
    Task<ConfigGetAutoCommissionResp> ToggleAutoCommissionAsync();
}

public class ConfigService(NirvedContext context) : IConfigService
{
    public async Task<StatusResp> UpdateTxnPasswordAsync(ConfigUpdateTxnPasswordReq configUpdateTxnPasswordReq)
    {
        var txnPassword = await context.Configs.FirstOrDefaultAsync(x => x.Key == "TxnPassword");
        if (txnPassword == null)
            throw new AppException("Old TxnPassword not found");
        if (configUpdateTxnPasswordReq.OldTxnPassword != txnPassword.Value)
            throw new AppException("Old TxnPassword is incorrect");
        txnPassword.Value = configUpdateTxnPasswordReq.NewTxnPassword;
        txnPassword.UpdatedOn = DateTime.Now;
        txnPassword.UpdatedOnDate=DateOnly.FromDateTime(txnPassword.UpdatedOn.Value);
        context.Configs.Update(txnPassword);
        await context.SaveChangesAsync();
        return new StatusResp {Message = "TxnPassword updated successfully"};
    }

    public async Task<ConfigGetServerTimeResp> UpdateServerTimeAsync(ConfigUpdateServerTimeReq configUpdateServerTimeReq)
    {
        var serverTime = await context.Configs.FirstOrDefaultAsync(x => x.Key == "ServerTime");
        if (serverTime == null)
        {
            var newServerTime = new Config
            {
                Key = "ServerTime",
                Value = $"{configUpdateServerTimeReq.StartTime.ToString("t")}^^{configUpdateServerTimeReq.EndTime.ToString("t")}",
                CreatedOn = DateTime.Now,
                UpdatedOn = null,
                UpdatedOnDate = null
            };
            newServerTime.UpdatedOnDate = DateOnly.FromDateTime(newServerTime.CreatedOn);
            await context.Configs.AddAsync(newServerTime);
            await context.SaveChangesAsync();
            return new ConfigGetServerTimeResp
            {
                StartTime = configUpdateServerTimeReq.StartTime,
                EndTime = configUpdateServerTimeReq.EndTime
            };
        }
        else
        {
            serverTime.Value = $"{configUpdateServerTimeReq.StartTime.ToString("t")}^^{configUpdateServerTimeReq.EndTime.ToString("t")}";
            serverTime.UpdatedOn = DateTime.Now;
            serverTime.UpdatedOnDate = DateOnly.FromDateTime(serverTime.UpdatedOn.Value);
            context.Configs.Update(serverTime);
            await context.SaveChangesAsync();
            return new ConfigGetServerTimeResp
            {
                StartTime = configUpdateServerTimeReq.StartTime,
                EndTime = configUpdateServerTimeReq.EndTime
            };
        }
    }

    public async Task<ConfigGetServerTimeResp> GetServerTimeAsync()
    {
        var serverTime = await context.Configs.FirstOrDefaultAsync(x => x.Key == "ServerTime");
        if (serverTime == null)
        {
            return new ConfigGetServerTimeResp
            {
                StartTime = TimeOnly.Parse("08:00"),
                EndTime = TimeOnly.Parse("20:00")
            };
        }
        var serverTimeSplit = serverTime.Value.Split("^^");
        return new ConfigGetServerTimeResp
        {
            StartTime = TimeOnly.Parse(serverTimeSplit[0]),
            EndTime = TimeOnly.Parse(serverTimeSplit[1])
        };
    }

    public async Task<ConfigGetAutoCommissionResp> GetAutoCommissionAsync()
    {
        var autoCommission = await context.Configs.FirstOrDefaultAsync(x => x.Key == "AutoCommission");
        if (autoCommission == null)
        {
            autoCommission = new Config
            {
                Key = "AutoCommission",
                Value = "1",
                CreatedOn = DateTime.Now,
                UpdatedOn = null,
                UpdatedOnDate = null
            };
            context.Configs.Add(autoCommission);
            await context.SaveChangesAsync();
            return new ConfigGetAutoCommissionResp
            {
                IsAutoCommission = true,
                LastUpdatedOn = autoCommission.CreatedOn
            };
        }
        return new ConfigGetAutoCommissionResp
        {
            IsAutoCommission = autoCommission.Value == "1",
            LastUpdatedOn = autoCommission.UpdatedOn ?? autoCommission.CreatedOn
        };
    }

    public async Task<ConfigGetAutoCommissionResp> ToggleAutoCommissionAsync()
    {
        var autoCommission = await context.Configs.FirstOrDefaultAsync(x => x.Key == "AutoCommission");
        if (autoCommission == null)
        {
            autoCommission = new Config
            {
                Key = "AutoCommission",
                Value = "1",
                CreatedOn = DateTime.Now,
                UpdatedOn = null,
                UpdatedOnDate = null
            };
            context.Configs.Add(autoCommission);
            await context.SaveChangesAsync();
            return new ConfigGetAutoCommissionResp
            {
                IsAutoCommission = true,
                LastUpdatedOn = autoCommission.CreatedOn
            };
        }
        autoCommission.Value = autoCommission.Value == "1" ? "0" : "1";
        autoCommission.UpdatedOn = DateTime.Now;
        autoCommission.UpdatedOnDate = DateOnly.FromDateTime(autoCommission.UpdatedOn.Value);
        context.Configs.Update(autoCommission);
        await context.SaveChangesAsync();
        return new ConfigGetAutoCommissionResp
        {
            IsAutoCommission = autoCommission.Value == "1",
            LastUpdatedOn = autoCommission.UpdatedOn ?? autoCommission.CreatedOn
        };
    }
}