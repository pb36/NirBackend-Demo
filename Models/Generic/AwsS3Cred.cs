namespace NirvedBackend.Models.Generic;

public class AwsS3Cred
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
    public string Region { get; set; }
    public string CloudFrontDomain { get; set; }
    public string KeyPairId { get; set; }
}