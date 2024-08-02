using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Bank;
using NirvedBackend.Models.Responses.Bank;

namespace NirvedBackend.Services;
public interface IBankService
{
    Task<BankGetResp> CreateBankAsync(BankCreateReq bankCreateReq);
    Task<BankGetResp> GetBankAsync(int bankId);
    Task<BankGetListResp> GetBanksAsync();
    Task<BankGetResp> UpdateBankAsync(BankUpdateReq bankUpdateReq);
    Task DeleteBankAsync(int bankId);
    Task<BankGetResp> ToggleBankAsync(int bankId);
    Task<BankGetListResp> GetActiveBanksAsync();
}

public class BankService(NirvedContext context) : IBankService
{
    public async Task<BankGetResp> CreateBankAsync(BankCreateReq bankCreateReq)
    {
        //check if bank account with same name, account number and ifsc code exists
        if (await context.Banks.AnyAsync(x => x.Name == bankCreateReq.BankName && x.AccountNumber == bankCreateReq.AccountNumber && x.IfscCode == bankCreateReq.IfscCode))
            throw new AppException("Bank account with same name, account number and ifsc code already exists");

        var bank = new Bank
        {
            AccountNumber = bankCreateReq.AccountNumber,
            IfscCode = bankCreateReq.IfscCode,
            Name = bankCreateReq.BankName,
            AccountName = bankCreateReq.AccountName,
            Address = bankCreateReq.Address,
            BranchName = bankCreateReq.BranchName,
            Type = (int)bankCreateReq.Type,
            IsActive = true
        };
        await context.Banks.AddAsync(bank);
        await context.SaveChangesAsync();
        return new BankGetResp
        {
            AccountName = bank.AccountName,
            AccountNumber = bank.AccountNumber,
            Address = bank.Address,
            BankId = bank.BankId,
            BankName = bank.Name,
            BranchName = bank.BranchName,
            IfscCode = bank.IfscCode,
            IsActive = bank.IsActive,
            Type = ((BankType) bank.Type).ToString(),
            TypeId = bank.Type
        };
    }

    public async Task<BankGetResp> GetBankAsync(int bankId)
    {
        var bank = await context.Banks.FirstOrDefaultAsync(x => x.BankId == bankId);
        if (bank == null)
            throw new AppException("Bank not found");
        return new BankGetResp
        {
            AccountName = bank.AccountName,
            AccountNumber = bank.AccountNumber,
            Address = bank.Address,
            BankId = bank.BankId,
            BankName = bank.Name,
            BranchName = bank.BranchName,
            IfscCode = bank.IfscCode,
            IsActive = bank.IsActive,
            Type = ((BankType) bank.Type).ToString(),
            TypeId = bank.Type
        };
    }

    public async Task<BankGetListResp> GetBanksAsync()
    {
        var banks = await context.Banks.Select(x => new BankGetResp
        {
            AccountName = x.AccountName,
            AccountNumber = x.AccountNumber,
            Address = x.Address,
            BankId = x.BankId,
            BankName = x.Name,
            BranchName = x.BranchName,
            IfscCode = x.IfscCode,
            IsActive = x.IsActive,
            Type = ((BankType) x.Type).ToString(),
            TypeId = x.Type
        }).ToListAsync();
        return new BankGetListResp {Banks = banks};
    }

    public async Task<BankGetResp> UpdateBankAsync(BankUpdateReq bankUpdateReq)
    {
        //check if bank account with same name, account number and ifsc code exists except this bank
        if (await context.Banks.AnyAsync(x => x.Name == bankUpdateReq.BankName && x.AccountNumber == bankUpdateReq.AccountNumber && x.IfscCode == bankUpdateReq.IfscCode && x.BankId != bankUpdateReq.BankId))
            throw new AppException("Bank account with same name, account number and ifsc code already exists");
        
        var bank = await context.Banks.FirstOrDefaultAsync(x => x.BankId == bankUpdateReq.BankId);
        if (bank == null)
            throw new AppException("Bank not found");
        
        bank.AccountName = bankUpdateReq.AccountName;
        bank.AccountNumber = bankUpdateReq.AccountNumber;
        bank.Address = bankUpdateReq.Address;
        bank.BranchName = bankUpdateReq.BranchName;
        bank.IfscCode = bankUpdateReq.IfscCode;
        bank.Name = bankUpdateReq.BankName;
        bank.Type = (int)bankUpdateReq.Type;
        await context.SaveChangesAsync();
        return new BankGetResp
        {
            AccountName = bank.AccountName,
            AccountNumber = bank.AccountNumber,
            Address = bank.Address,
            BankId = bank.BankId,
            BankName = bank.Name,
            BranchName = bank.BranchName,
            IfscCode = bank.IfscCode,
            IsActive = bank.IsActive,
            Type = ((BankType) bank.Type).ToString(),
            TypeId = bank.Type
        };
    }

    public async Task DeleteBankAsync(int bankId)
    {
        await context.Banks.Where(x => x.BankId == bankId).ExecuteDeleteAsync();
    }

    public async Task<BankGetResp> ToggleBankAsync(int bankId)
    {
        var bank = await context.Banks.FirstOrDefaultAsync(x => x.BankId == bankId);
        if (bank == null)
            throw new AppException("Bank not found");
        bank.IsActive = !bank.IsActive;
        await context.SaveChangesAsync();
        return new BankGetResp
        {
            AccountName = bank.AccountName,
            AccountNumber = bank.AccountNumber,
            Address = bank.Address,
            BankId = bank.BankId,
            BankName = bank.Name,
            BranchName = bank.BranchName,
            IfscCode = bank.IfscCode,
            IsActive = bank.IsActive,
            Type = ((BankType) bank.Type).ToString(),
            TypeId = bank.Type
        };
    }

    public async Task<BankGetListResp> GetActiveBanksAsync()
    {
        var banks = await context.Banks.Where(x=>x.IsActive).Select(x => new BankGetResp
        {
            AccountName = x.AccountName,
            AccountNumber = x.AccountNumber,
            Address = x.Address,
            BankId = x.BankId,
            BankName = x.Name,
            BranchName = x.BranchName,
            IfscCode = x.IfscCode,
            IsActive = x.IsActive,
            Type = ((BankType) x.Type).ToString(),
            TypeId = x.Type
        }).ToListAsync();
        return new BankGetListResp {Banks = banks};
    }
}