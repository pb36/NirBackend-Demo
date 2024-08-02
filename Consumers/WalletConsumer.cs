using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Generic.Commission;
using NirvedBackend.Models.Requests.BillPayment;
using NirvedBackend.Models.Requests.Consumer;
using NirvedBackend.Models.Requests.Credit;
using NirvedBackend.Models.Requests.TopUp;
using NirvedBackend.Models.Requests.User;
using StackExchange.Redis;

namespace NirvedBackend.Consumers;
public class WalletConsumer(ISendEndpointProvider bus,NirvedContext context,IConnectionMultiplexer redisCache,ILogger<WalletConsumer> logger) : IConsumer<WalletConsumerReq>
{
    
    private readonly IDatabase _responseCache = redisCache.GetDatabase((int) RedisDatabases.ResponseCache);
    private readonly ISendEndpoint _rechargeSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.RechargeQueue)).Result;
    private readonly ISendEndpoint _whatsappSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.WhatsappMessageQueue)).Result;

    public async Task Consume(ConsumeContext<WalletConsumerReq> context)
    {
        switch (context.Message.TransactionType)
        {
            case WalletTransactionType.AdminTopUp:
                await AdminTopUp(JsonConvert.DeserializeObject<UserAdminTopUpReq>(context.Message.Data));
                break;
            case WalletTransactionType.UserTopUp:
                await UserTopUp(JsonConvert.DeserializeObject<TopUpProcessReq>(context.Message.Data));
                break;
            case WalletTransactionType.JournalVoucher:
                await JournalVoucher(JsonConvert.DeserializeObject<UserJournalVoucherReq>(context.Message.Data));
                break;
            case WalletTransactionType.CreditApproval:
                await CreditApproval(JsonConvert.DeserializeObject<CreditProcessReq>(context.Message.Data));
                break;
            case WalletTransactionType.OutstandingClear:
                await OutstandingClear(JsonConvert.DeserializeObject<OutstandingClearReq>(context.Message.Data));
                break;
            case WalletTransactionType.ProcessBill:
                await ProcessBill(JsonConvert.DeserializeObject<ProcessBillReq>(context.Message.Data));
                break;
            case WalletTransactionType.RefundBill:
                await RefundBill(JsonConvert.DeserializeObject<RefundReq>(context.Message.Data));
                break;
            case WalletTransactionType.ProcessBillList:
                await ProcessBillList(JsonConvert.DeserializeObject<BillUpdateListReq>(context.Message.Data));
                break;
        }
    }

    private async Task ProcessBillList(BillUpdateListReq billUpdateListReq)
    {
        var errorString = new StringBuilder();
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var createdByList = new List<int>();
        foreach (var billUpdateListBase in billUpdateListReq.BillUpdateList)
        {
            var bill = await context.Bills.FirstOrDefaultAsync(b => b.DisplayId == billUpdateListBase.BillId);

            if (bill == null)
            {
                errorString.AppendLine($"{billUpdateListBase.BillId} - not found");
                continue;
            }

            if (bill.Status != (int)BillStatus.Pending)
            {
                errorString.AppendLine($"{billUpdateListBase.BillId} - already processed");
                continue;
            }
            
            //trim remark max 90 chars without checking the length
            bill.Status = (int)BillStatus.Success;
            bill.Remark = billUpdateListBase.Remark.Substring(0, Math.Min(billUpdateListBase.Remark.Length, 90));
            bill.PaymentRef = billUpdateListBase.PaymentRef.Substring(0, Math.Min(billUpdateListBase.PaymentRef.Length, 40));
            bill.UpdatedOn=dateTime.Item1;
            bill.UpdatedOnDate=dateTime.Item2;
            if(createdByList.Any(u=>u==bill.CreatedBy)==false)
                createdByList.Add(bill.CreatedBy);
        }
        await context.SaveChangesAsync();
        foreach (var createdBy in createdByList)
        {
            await _responseCache.KeyDeleteAsync(ResponseCaches.UserDashboard+createdBy);
            await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+createdBy);
        }
        if (errorString.Length > 0)
        {
            var adminMobileNumber = await context.Users.Where(u => u.UserType == (int)UserType.Admin).Select(u => u.Mobile).FirstAsync();
            await _whatsappSendEndpoint.Send(new WhatsappConsumerReq
            {
                Message = errorString.ToString(),
                WhatsappMessageType = WhatsappMessageType.Text,
                PhoneNumber = adminMobileNumber
            });
        }
    }

    private async Task RefundBill(RefundReq refundReq)
    {
        var bill = await context.Bills
            .Include(x => x.Ledgers)
            .FirstAsync(x => x.BillId == refundReq.BillId);
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var ledgers=new List<Ledger>();
        var users=new List<User>();
        foreach (var ledger in bill.Ledgers)
        {
            User user;
            if (users.Any(u => u.UserId == ledger.UserId)==false)
            {
                user = await context.Users.FirstAsync(u => u.UserId == ledger.UserId);
                users.Add(user);
            }
            else
            {
                user = users.First(u => u.UserId == ledger.UserId);
            }
            ledgers.Add(new Ledger
            {
                Amount = ledger.Amount,
                Type = ledger.Type == (int) TransactionType.Credit ? (int) TransactionType.Debit : (int) TransactionType.Credit,
                CreatedOn = dateTime.Item1,
                UserId = ledger.UserId,
                Remark = "Refund " + ledger.Remark,
                CreatedOnDate = dateTime.Item2,
                Opening = user.Balance,
                Closing = ledger.Type == (int) TransactionType.Credit ? user.Balance - ledger.Amount :user.Balance + ledger.Amount,
                BillId = bill.BillId
            });
            user.Balance += ledger.Type == (int) TransactionType.Credit ? -ledger.Amount : ledger.Amount;
            if (user.UserId == bill.CreatedBy)
            {
                user.UpdatedOn = dateTime.Item1;
                user.UpdatedOnDate = dateTime.Item2;
            }
            //update user in list
            users[users.FindIndex(u => u.UserId == user.UserId)] = user;
            await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+user.UserId);
        }
        await context.Ledgers.AddRangeAsync(ledgers);
        bill.Status = (int) BillStatus.Failed;
        bill.UpdatedOn = dateTime.Item1;
        bill.UpdatedOnDate = dateTime.Item2;
        await context.SaveChangesAsync();
    }
    
    private static decimal? GetPercentage(decimal amount, List<CommissionBase> ranges)
    {
        foreach (var range in ranges)
        {
            if (amount >= range.From && amount <= range.To)
            {
                return range.Percentage;
            }
        }
        return null; // No matching range found
    }

    private async Task AutoCommission(CommissionReq commissionReq)
    {
        var bill = await context.Bills
            .Include(x => x.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation)
            .Include(x => x.Biller)
            .FirstAsync(x => x.BillId == commissionReq.BillId);
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var userChains=new List<User>();
        userChains.Add(bill.CreatedByNavigation);
        userChains.Add(bill.CreatedByNavigation.CreatedByNavigation);
        if (bill.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation != null)
        {
            userChains.Add(bill.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation);
            if (bill.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation != null)
            {
                userChains.Add(bill.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation.CreatedByNavigation);
            }
        }
        
        userChains.Sort((x, y) => x.UserType.CompareTo(y.UserType));
        for(var i=1;i<userChains.Count;i++)
        {
            var parentUser = userChains[i - 1];
            var user = userChains[i];
            var data = await context.CommissionPercentages.
                Where(x=>x.UserId==user.UserId && x.BillerCategoryId==bill.Biller.BillerCategoryId).
                FirstAsync();
            
            var commission = GetPercentage((int)bill.Amount, JsonConvert.DeserializeObject<List<CommissionBase>>(data.PercentageJson));
            
            if (commission == null)
            {
               commission=data.Percentage;
            }
            
            var billAmount = bill.Amount;
            var commissionAmount = billAmount * commission.Value / 100;
            var taxAmount = commissionAmount * 5 / 100;
            
            //add commission to the user
            user.Ledgers.Add(new Ledger
            {
                Amount = commissionAmount,
                Type = (int) TransactionType.Credit,
                CreatedOn = dateTime.Item1,
                UserId = user.UserId,
                Remark = "Com",
                CreatedOnDate = dateTime.Item2,
                Opening = user.Balance,
                Closing = user.Balance + commissionAmount,
                BillId = bill.BillId
            });
            user.Balance += commissionAmount;
            user.UpdatedOn = dateTime.Item1;
            user.UpdatedOnDate = dateTime.Item2;
            
            //deduct commission from the parent
            parentUser.Ledgers.Add(new Ledger
            {
                Amount = commissionAmount,
                Type = (int) TransactionType.Debit,
                CreatedOn = dateTime.Item1,
                UserId = parentUser.UserId,
                Remark = "Com",
                CreatedOnDate = dateTime.Item2,
                Opening = parentUser.Balance,
                Closing = parentUser.Balance - commissionAmount,
                BillId = bill.BillId
            });
            parentUser.Balance -= commissionAmount;
            parentUser.UpdatedOn = dateTime.Item1;
            parentUser.UpdatedOnDate = dateTime.Item2;
            
            //deduct tax from the user
            user.Ledgers.Add(new Ledger
            {
                Amount = taxAmount,
                Type = (int) TransactionType.Debit,
                CreatedOn = dateTime.Item1,
                UserId = user.UserId,
                Remark = "TDS",
                CreatedOnDate = dateTime.Item2,
                Opening = user.Balance,
                Closing = user.Balance - taxAmount,
                BillId = bill.BillId
            });
            user.Balance -= taxAmount;
            user.UpdatedOn = dateTime.Item1;
            user.UpdatedOnDate = dateTime.Item2;
            
            //add tax to the parent
            parentUser.Ledgers.Add(new Ledger
            {
                Amount = taxAmount,
                Type = (int) TransactionType.Credit,
                CreatedOn = dateTime.Item1,
                UserId = parentUser.UserId,
                Remark = "TDS",
                CreatedOnDate = dateTime.Item2,
                Opening = parentUser.Balance,
                Closing = parentUser.Balance + taxAmount,
                BillId = bill.BillId,
            });
            parentUser.Balance += taxAmount;
            parentUser.UpdatedOn = dateTime.Item1;
            parentUser.UpdatedOnDate = dateTime.Item2;
            
            await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+user.UserId);
            await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+parentUser.UserId);
            userChains[i] = user;
            userChains[i - 1] = parentUser;
            context.Users.UpdateRange(user,parentUser);
        }
        bill.UpdatedOn = dateTime.Item1;
        bill.UpdatedOnDate = dateTime.Item2;
        bill.CommissionGiven = true;
        await context.SaveChangesAsync();
        
    }

    private async Task ProcessBill(ProcessBillReq processBillReq)
    {
        var admin = await context.Users.FirstAsync(u=>u.UserType==(int)UserType.Admin);
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var bill = await context.Bills
            .Include(x => x.CreatedByNavigation).Include(bill => bill.Biller)
            .FirstAsync(x => x.BillId == processBillReq.BillId);
        if (bill.CreatedByNavigation.Balance < processBillReq.Amount)
        {
            bill.Status = (int) BillStatus.Failed;
            bill.UpdatedOn = dateTime.Item1;
            bill.UpdatedOnDate = dateTime.Item2;
            bill.Remark = "Insufficient Balance at processing time";
            await context.SaveChangesAsync();
            return;
        }
        bill.CreatedByNavigation.Ledgers.Add(new Ledger
        {
            Amount = processBillReq.Amount,
            Type = (int) TransactionType.Debit,
            CreatedOn = dateTime.Item1,
            UserId = bill.CreatedByNavigation.UserId,
            CreatedOnDate = dateTime.Item2,
            Opening = bill.CreatedByNavigation.Balance,
            Closing = bill.CreatedByNavigation.Balance - processBillReq.Amount,
            BillId = bill.BillId
        });
        bill.CreatedByNavigation.Balance -= processBillReq.Amount;
        bill.CreatedByNavigation.UpdatedOn = dateTime.Item1;
        bill.CreatedByNavigation.UpdatedOnDate = dateTime.Item2;
        admin.Ledgers.Add(new Ledger
        {
            Amount = processBillReq.Amount,
            Type = (int) TransactionType.Credit,
            CreatedOn = dateTime.Item1,
            UserId = admin.UserId,
            CreatedOnDate = dateTime.Item2,
            Opening = admin.Balance,
            Closing = admin.Balance + processBillReq.Amount,
            BillId = bill.BillId
        });
        admin.Balance += processBillReq.Amount;
        admin.UpdatedOn = dateTime.Item1;
        admin.UpdatedOnDate = dateTime.Item2;
        bill.Status = (int) BillStatus.Pending;
        bill.UpdatedOn = dateTime.Item1;
        bill.UpdatedOnDate = dateTime.Item2;
        await context.SaveChangesAsync();
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+bill.CreatedByNavigation.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+admin.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserDashboard+bill.CreatedByNavigation.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserDashboard+admin.UserId);
        if (bill.Biller.BillerCategoryId is 4 or 5)
        {
            // await _rechargeSendEndpoint.Send(new RechargeConsumerReq
            // {
            //     MobileNumber = bill.ServiceNumber,
            //     OperatorCode = bill.Biller.Code,
            //     Amount = (int)bill.Amount,
            //     RefId = bill.DisplayId
            // });
        }
        var autoCommission = await context.Configs.FirstOrDefaultAsync(x => x.Key == "AutoCommission");
        if (autoCommission is { Value: "1" })
        {
            await AutoCommission(new CommissionReq
            {
                BillId = bill.BillId
            });
        }
    }

    private async Task AdminTopUp(UserAdminTopUpReq adminTopUpReq)
    {
        var admin = await context.Users.FirstAsync(u=>u.UserType==(int)UserType.Admin);
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        admin.Ledgers.Add(new Ledger
        {
            Amount = adminTopUpReq.Amount,
            Type = (int)TransactionType.Credit,
            CreatedOn = dateTime.Item1,
            UserId = admin.UserId,
            Remark = "Admin Self Top Up",
            CreatedOnDate = dateTime.Item2,
            Opening = admin.Balance,
            Closing = admin.Balance + adminTopUpReq.Amount
        });
        admin.Balance += adminTopUpReq.Amount;
        admin.UpdatedOn = dateTime.Item1;
        admin.UpdatedOnDate = dateTime.Item2;
        await context.SaveChangesAsync();
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+admin.UserId);
    }

    private async Task UserTopUp(TopUpProcessReq topUpProcessReq)
    {
        var topUp = await context.TopUpRequests
            .Include(x => x.User.CreatedByNavigation)
            .FirstAsync(x => x.TopUpRequestId == topUpProcessReq.TopUpRequestId);
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        if (topUp.User.CreatedByNavigation.Balance < topUp.Amount)
        {
            topUp.Status = (int) TopUpRequestStatus.Rejected;
            topUp.UpdatedOn = dateTime.Item1;
            topUp.UpdatedOnDate = dateTime.Item2;
            topUp.Remark = "Insufficient Balance at processing time";
            await context.SaveChangesAsync();
            return;
        }
        
        topUp.User.CreatedByNavigation.Ledgers.Add(new Ledger
        {
            Amount = topUp.Amount,
            Type = (int) TransactionType.Debit,
            CreatedOn = dateTime.Item1,
            UserId = topUp.User.CreatedByNavigation.UserId,
            CreatedOnDate = dateTime.Item2,
            Opening = topUp.User.CreatedByNavigation.Balance,
            Closing = topUp.User.CreatedByNavigation.Balance - topUp.Amount,
            TopUpRequestId = topUp.TopUpRequestId
        });
        topUp.User.CreatedByNavigation.Balance -= topUp.Amount;
        topUp.User.CreatedByNavigation.UpdatedOn = dateTime.Item1;
        topUp.User.CreatedByNavigation.UpdatedOnDate = dateTime.Item2;
        
        topUp.User.Ledgers.Add(new Ledger
        {
            Amount = topUp.Amount,
            Type = (int) TransactionType.Credit,
            CreatedOn = dateTime.Item1,
            UserId = topUp.User.UserId,
            CreatedOnDate = dateTime.Item2,
            Opening = topUp.User.Balance,
            Closing = topUp.User.Balance + topUp.Amount,
            TopUpRequestId = topUp.TopUpRequestId
        });
        topUp.User.Balance += topUp.Amount;
        topUp.User.UpdatedOn = dateTime.Item1;
        topUp.User.UpdatedOnDate = dateTime.Item2;
        
        topUp.Status = (int) TopUpRequestStatus.Approved;
        topUp.UpdatedOn = dateTime.Item1;
        topUp.UpdatedOnDate = dateTime.Item2;
        await context.SaveChangesAsync();
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+topUp.User.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+topUp.User.CreatedByNavigation.UserId);
    }
    
    private async Task CreditApproval(CreditProcessReq creditProcessReq)
    {
        var creditRequest = await context.CreditRequests
            .Include(x => x.User.CreatedByNavigation)
            .FirstOrDefaultAsync(x => x.CreditRequestId == creditProcessReq.CreditRequestId);
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        if (creditRequest.User.CreatedByNavigation.Balance < creditRequest.Amount)
        {
            creditRequest.Status = (int) CreditRequestStatus.Rejected;
            creditRequest.UpdatedOn = dateTime.Item1;
            creditRequest.UpdatedOnDate = dateTime.Item2;
            creditRequest.Remark = "Insufficient Balance at processing time";
            await context.SaveChangesAsync();
            return;
        }
        
        
        creditRequest.User.CreatedByNavigation.Ledgers.Add(new Ledger
        {
            Amount = creditRequest.Amount,
            Type = (int) TransactionType.Debit,
            CreatedOn = dateTime.Item1,
            UserId = creditRequest.User.CreatedByNavigation.UserId,
            CreatedOnDate = dateTime.Item2,
            Opening = creditRequest.User.CreatedByNavigation.Balance,
            Closing = creditRequest.User.CreatedByNavigation.Balance - creditRequest.Amount,
            CreditRequestId = creditRequest.CreditRequestId
        });
        creditRequest.User.CreatedByNavigation.Balance -= creditRequest.Amount;
        creditRequest.User.CreatedByNavigation.UpdatedOn = dateTime.Item1;
        creditRequest.User.CreatedByNavigation.UpdatedOnDate = dateTime.Item2;
        
        creditRequest.User.Ledgers.Add(new Ledger
        {
            Amount = creditRequest.Amount,
            Type = (int) TransactionType.Credit,
            CreatedOn = dateTime.Item1,
            UserId = creditRequest.User.UserId,
            CreatedOnDate = dateTime.Item2,
            Opening = creditRequest.User.Balance,
            Closing = creditRequest.User.Balance + creditRequest.Amount,
            CreditRequestId = creditRequest.CreditRequestId
        });
        creditRequest.User.Balance += creditRequest.Amount;
        creditRequest.User.UpdatedOn = dateTime.Item1;
        creditRequest.User.UpdatedOnDate = dateTime.Item2;
        
        creditRequest.Status = (int) CreditRequestStatus.Approved;
        creditRequest.UpdatedOn = dateTime.Item1;
        creditRequest.UpdatedOnDate = dateTime.Item2;
        await context.SaveChangesAsync();
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+creditRequest.User.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+creditRequest.User.CreatedByNavigation.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserDashboard+creditRequest.User.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserDashboard+creditRequest.User.CreatedByNavigation.UserId);
        await OutstandingAdd(creditRequest.User.UserId, creditRequest.Amount);
    }

    private async Task OutstandingAdd(int userId, decimal amount)
    {
        var outstanding = await context.Outstandings.FirstOrDefaultAsync(x => x.UserId == userId);
        if (outstanding == null)
        {
            outstanding = new Outstanding
            {
                UserId = userId,
                Amount = amount,
            };
            outstanding.OutstandingLedgers.Add(new OutstandingLedger
            {
                Amount = amount,
                Type = (int) TransactionType.Debit,
                CreatedOn = DateTime.Now,
                Opening = 0,
                Closing = amount,
                OutstandingId = outstanding.OutstandingId,
            });
            await context.Outstandings.AddAsync(outstanding);
        }
        else
        {
            outstanding.OutstandingLedgers.Add(new OutstandingLedger
            {
                Amount = amount,
                Type = (int) TransactionType.Debit,
                CreatedOn = DateTime.Now,
                Opening = outstanding.Amount,
                Closing = outstanding.Amount + amount,
                OutstandingId = outstanding.OutstandingId,
            });
            outstanding.Amount += amount;
        }
        await context.SaveChangesAsync();
    }
    
    private async Task OutstandingClear(OutstandingClearReq outstandingClearReq)
    {
        var outstanding = await context.Outstandings.Include(outstanding => outstanding.User)
            .FirstAsync(x => x.OutstandingId == outstandingClearReq.OutstandingId);
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        outstanding.OutstandingLedgers.Add(new OutstandingLedger
        {
            Amount = outstandingClearReq.Amount,
            Type = (int) TransactionType.Credit,
            CreatedOn = dateTime.Item1,
            Opening = outstanding.Amount,
            Closing = outstanding.Amount - outstandingClearReq.Amount,
            OutstandingId = outstanding.OutstandingId,
            Remark = outstandingClearReq.Remark,
        });
        outstanding.Amount -= outstandingClearReq.Amount;
        await context.SaveChangesAsync();
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserDashboard+outstanding.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserDashboard+outstanding.User.CreatedBy);
    }
    
    private async Task JournalVoucher(UserJournalVoucherReq journalVoucherReq)
    {
        var fromUser = await context.Users.FirstAsync(u=>u.UserId==journalVoucherReq.FromUserId);
        var toUser = await context.Users.FirstAsync(u=>u.UserId==journalVoucherReq.UserId);
        var dateTime=GenericHelper.GetDateTimeWithDateOnly();
        if (journalVoucherReq.TransactionType == TransactionType.Credit)
        {
            fromUser.Ledgers.Add(new Ledger
            {
                Amount = journalVoucherReq.Amount,
                Type = (int) TransactionType.Debit,
                CreatedOn = dateTime.Item1,
                UserId = fromUser.UserId,
                Remark = "JV - "+journalVoucherReq.Remark,
                CreatedOnDate = dateTime.Item2,
                Opening = fromUser.Balance,
                Closing = fromUser.Balance - journalVoucherReq.Amount
            });
            fromUser.Balance -= journalVoucherReq.Amount;
            fromUser.UpdatedOn = dateTime.Item1;
            fromUser.UpdatedOnDate = dateTime.Item2;
            toUser.Ledgers.Add(new Ledger
            {
                Amount = journalVoucherReq.Amount,
                Type = (int) TransactionType.Credit,
                CreatedOn = dateTime.Item1,
                UserId = toUser.UserId,
                Remark = "JV - "+journalVoucherReq.Remark,
                CreatedOnDate = dateTime.Item2,
                Opening = toUser.Balance,
                Closing = toUser.Balance + journalVoucherReq.Amount
            });
            toUser.Balance += journalVoucherReq.Amount;
            toUser.UpdatedOn = dateTime.Item1;
            toUser.UpdatedOnDate = dateTime.Item2;
        }
        else
        {
            toUser.Ledgers.Add(new Ledger
            {
                Amount = journalVoucherReq.Amount,
                Type = (int) TransactionType.Debit,
                CreatedOn = dateTime.Item1,
                UserId = toUser.UserId,
                Remark = "JV - "+journalVoucherReq.Remark,
                CreatedOnDate = dateTime.Item2,
                Opening = toUser.Balance,
                Closing = toUser.Balance - journalVoucherReq.Amount
            });
            toUser.Balance -= journalVoucherReq.Amount;
            toUser.UpdatedOn = dateTime.Item1;
            toUser.UpdatedOnDate = dateTime.Item2;
            fromUser.Ledgers.Add(new Ledger
            {
                Amount = journalVoucherReq.Amount,
                Type = (int) TransactionType.Credit,
                CreatedOn = dateTime.Item1,
                UserId = fromUser.UserId,
                Remark = "JV - "+journalVoucherReq.Remark,
                CreatedOnDate = dateTime.Item2,
                Opening = fromUser.Balance,
                Closing = fromUser.Balance + journalVoucherReq.Amount
            });
            fromUser.Balance += journalVoucherReq.Amount;
            fromUser.UpdatedOn = dateTime.Item1;
            fromUser.UpdatedOnDate = dateTime.Item2;
        }
        await context.SaveChangesAsync();
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+fromUser.UserId);
        await _responseCache.KeyDeleteAsync(ResponseCaches.UserBalance+toUser.UserId);
    }
}