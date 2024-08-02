using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Amazon.CloudFront;

namespace NirvedBackend.Helpers;

public static class GenericHelper
{
    public static string GetOtp()
    {
        return Random.Shared.Next(111111, 999999).ToString();
    }
    
    public static Tuple<DateTime,DateOnly> GetDateTimeWithDateOnly()
    {
        var dateTime = DateTime.Now;
        var dateOnly = DateOnly.FromDateTime(dateTime);
        return new Tuple<DateTime, DateOnly>(dateTime, dateOnly);
    }
    
    //GenerateStreamFromBase64String
    public static Stream GenerateStreamFromBase64String(string base64String)
    {
        var bytes = Convert.FromBase64String(base64String);
        return new MemoryStream(bytes);
    }
    
    public static string GenerateQueryString(string url,Dictionary<string,string> queryDictionary)
    {
        var queryString = new StringBuilder();
        queryString.Append(url);
        queryString.Append('?');
        foreach (var (key, value) in queryDictionary)
        {
            queryString.Append($"{key}={value}&");
        }
        return queryString.ToString().TrimEnd('&');
    }
    
    public static bool IsValidHmac(string signature, string data, string secret)
    {
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        var hashString = Convert.ToBase64String(hash);
        return hashString == signature;
    }
    
    public static string GenerateSignature(string data, string secret)
    {
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
    
    public static string GenerateCloudFrontUrl(string url, string keyPairId,int expiryInMinutes)
    {
        using var key = new StreamReader(@"private_key.pem");
        var signedUrl=AmazonCloudFrontUrlSigner.GetCannedSignedURL(
            url,
            key,
            keyPairId,
            DateTime.Now.AddMinutes(expiryInMinutes)
        );
        return signedUrl;
    }
}