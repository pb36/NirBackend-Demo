using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Consumer;
using NirvedBackend.Models.Responses.Recharge;

namespace NirvedBackend.Consumers;

public class RechargeConsumer(ISendEndpointProvider bus,NirvedContext dbContext,IHttpClientFactory httpClientFactory,IOptions<RechargeApiInfoSettings> rechargeApiInfoSettings) : IConsumer<RechargeConsumerReq>
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly RechargeApiInfoSettings _rechargeApiInfoSettings = rechargeApiInfoSettings.Value;
    private readonly ISendEndpoint _whatsappSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.WhatsappMessageQueue)).Result;
    private readonly ISendEndpoint _walletSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/"+RabbitQueues.WalletQueue)).Result;

    public async Task Consume(ConsumeContext<RechargeConsumerReq> context)
    {
        var adminMobileNumber = await dbContext.Users.Where(u => u.UserType == (int)UserType.Admin).Select(u => u.Mobile).FirstAsync();
        var bill=await dbContext.Bills.Include(bill => bill.CreatedByNavigation).FirstAsync(b => b.DisplayId == context.Message.RefId);
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var availableBalance = await AvailableBalance();
        if (availableBalance < context.Message.Amount)
        {
            await _whatsappSendEndpoint.Send(new WhatsappConsumerReq
            {
                WhatsappMessageType = WhatsappMessageType.Text,
                PhoneNumber =adminMobileNumber,
                Message = $"Recharge failed\nPlease Load Money in 1PaySolution"
            });
            bill.Status=(int)BillStatus.Failed;
            bill.Remark="NIR-IB";
            bill.UpdatedOn=dateTime.Item1;
            bill.UpdatedOnDate=dateTime.Item2;
            await dbContext.SaveChangesAsync();
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
                WhatsappMessageType = WhatsappMessageType.Text,
                PhoneNumber = bill.CreatedByNavigation.Mobile,
                Message = $"Recharge failed\nRefId: {bill.DisplayId}\nAmount: {bill.Amount}\nMobile: {bill.ServiceNumber}\nRemark: {bill.Remark}"
            });
            return;
        }

        var queryDictionary = new Dictionary<string, string>
        {
            {"MobileNo", _rechargeApiInfoSettings.MobileNo},
            {"APIKey", _rechargeApiInfoSettings.ApiKey},
            {"REQTYPE","RECH"},
            {"RESPTYPE","JSON"},
            {"CUSTNO", context.Message.MobileNumber},
            {"SERCODE", context.Message.OperatorCode},
            {"REFNO", context.Message.RefId},
            {"AMT", context.Message.Amount.ToString()}
        };
        var url = GenericHelper.GenerateQueryString("https://www.1paysolution.co.in/OPSAPI/RechargeAPI.aspx", queryDictionary);
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            //RechargeApiResp
            var rechargeApiResp = await response.Content.ReadFromJsonAsync<RechargeApiResp>();
            if (rechargeApiResp != null)
            {
                if (rechargeApiResp.STATUSCODE != "0" && rechargeApiResp.STATUSCODE != "1")
                {
                    bill.Status=(int)BillStatus.Failed;
                    bill.Remark=rechargeApiResp.TRNSTATUSDESC;
                    bill.UpdatedOn=dateTime.Item1;
                    bill.UpdatedOnDate=dateTime.Item2;
                    await dbContext.SaveChangesAsync();
                    await _walletSendEndpoint.Send(new WalletConsumerReq
                    {
                        TransactionType = WalletTransactionType.RefundBill,
                        Data = JsonConvert.SerializeObject(new RefundReq
                        {
                            BillId = bill.BillId
                        }),
                    });
                    await _whatsappSendEndpoint.Send(new WhatsappConsumerReq()
                    {
                        WhatsappMessageType = WhatsappMessageType.Text,
                        PhoneNumber = bill.CreatedByNavigation.Mobile,
                        Message = $"Recharge failed\nRefId: {bill.DisplayId}\nAmount: {bill.Amount}\nMobile: {bill.ServiceNumber}\nRemark: {bill.Remark}"
                    });
                }
            }
            else
            {
                await _whatsappSendEndpoint.Send(new WhatsappConsumerReq()
                {
                    WhatsappMessageType = WhatsappMessageType.Text,
                    PhoneNumber = adminMobileNumber,
                    Message = $"Recharge failed\nRefId: {bill.DisplayId}\nAmount: {bill.Amount}\nMobile: {bill.ServiceNumber}\nRemark: Please check Manually and update the status"
                });
            }
        }
        else
        {
            await _whatsappSendEndpoint.Send(new WhatsappConsumerReq()
            {
                WhatsappMessageType = WhatsappMessageType.Text,
                PhoneNumber = adminMobileNumber,
                Message = $"Recharge failed\nRefId: {bill.DisplayId}\nAmount: {bill.Amount}\nMobile: {bill.ServiceNumber}\nRemark: Please check Manually and update the status"
            });
        }
        
        
    }
    
    //https://www.1paysolution.co.in/OPSAPI/RechargeAPI.aspx?MobileNo=9974990008&APIKey=NZyWte4RXK8DUNjyOQ6gYTXgoXDhTohUz1t&REQTYPE=BAL&RESPTYPE=JSON
    private async Task<decimal> AvailableBalance()
    {
        var queryDictionary = new Dictionary<string, string>
        {
            {"MobileNo", _rechargeApiInfoSettings.MobileNo},
            {"APIKey", _rechargeApiInfoSettings.ApiKey},
            {"REQTYPE","BAL"},
            {"RESPTYPE","JSON"}
        };
        
        var url = GenericHelper.GenerateQueryString("https://www.1paysolution.co.in/OPSAPI/RechargeAPI.aspx", queryDictionary);
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var rechargeApiResp = await response.Content.ReadFromJsonAsync<RechargeApiResp>();
            if (rechargeApiResp != null)
            {
                return rechargeApiResp.BALANCE;
            }
        }
        return 0;
    }
}