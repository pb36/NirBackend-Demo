using System;
using System.Net.Http;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Requests.Consumer;

namespace NirvedBackend.Consumers;

public class WhatsappMessageConsumer(ILogger<WhatsappMessageConsumer> logger, IHttpClientFactory httpClientFactory, IOptions<WhatsappNotificationConfig> whatsappNotificationConfig) : IConsumer<WhatsappConsumerReq>
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly WhatsappNotificationConfig _whatsappNotificationConfig = whatsappNotificationConfig.Value;

    public async Task Consume(ConsumeContext<WhatsappConsumerReq> context)
    {
        return;
        switch (context.Message.WhatsappMessageType)
        {
            case WhatsappMessageType.Text:
                await SendTextMessage(context.Message.Message, context.Message.PhoneNumber);
                break;
            case WhatsappMessageType.Image:
                await SendImageMessage(context.Message.Message, context.Message.PhoneNumber, context.Message.Url);
                break;
            case WhatsappMessageType.Document:
                await SendDocumentMessage(context.Message.PhoneNumber, context.Message.Url);
                break;
        }
    }

    private async Task SendTextMessage(string message, string phoneNumber)
    {
        var url = $"https://whatsbot.tech/api/send_sms?api_token={_whatsappNotificationConfig.ApiKey}&mobile=91{phoneNumber}&message={message}";
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            if (responseString.Contains("true") == false)
            {
                logger.LogError("Whatsapp message failed - {ResponseString}", responseString);
            }
        }
        else
        {
            logger.LogError("Whatsapp message failed - {ResponseStatus}", response.StatusCode.ToString());
        }
    }
    
    private async Task SendImageMessage(string message, string phoneNumber,string imageUrl)
    {
        imageUrl = Uri.EscapeDataString(imageUrl);
        var url = $"https://whatsbot.tech/api/send_img?api_token={_whatsappNotificationConfig.ApiKey}&mobile=91{phoneNumber}&img_caption={message}&img_url={imageUrl}";
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            if (responseString.Contains("true") == false)
            {
                logger.LogError("Whatsapp message failed - {ResponseString}", responseString);
            }
        }
        else
        {
            logger.LogError("Whatsapp message failed - {ResponseStatus}", response.StatusCode.ToString());
        }
    }
    
    private async Task SendDocumentMessage(string phoneNumber,string documentUrl)
    {
        var url = $"https://whatsbot.tech/api/send_doc?api_token={_whatsappNotificationConfig.ApiKey}&mobile=91{phoneNumber}&doc_url={documentUrl}";
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            if (responseString.Contains("true") == false)
            {
                logger.LogError("Whatsapp message failed - {ResponseString}", responseString);
            }
        }
        else
        {
            logger.LogError("Whatsapp message failed - {ResponseStatus}", response.StatusCode.ToString());
        }
    }
}