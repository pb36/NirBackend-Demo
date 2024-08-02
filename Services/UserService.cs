using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.EmailTemplates;
using NirvedBackend.Models.Generic;
using NirvedBackend.Models.Generic.Commission;
using NirvedBackend.Models.Requests.Consumer;
using NirvedBackend.Models.Requests.Excel;
using NirvedBackend.Models.Requests.User;
using NirvedBackend.Models.Responses.DropDown;
using NirvedBackend.Models.Responses.User;
using StackExchange.Redis;

namespace NirvedBackend.Services;

public interface IUserService
{
    Task<UserLoginResp> LoginAsync(UserLoginReq userLoginReq, string loginIp);
    Task<UserLoginResp> LoginOtpAsync(UserLoginOtpReq userLoginOtpReq, string loginIp);
    Task<UserGetResp> CreateUserAsync(UserCreateReq userCreateReq, UserType currentUserType, int userId);
    Task<UserGetResp> UpdateUserAsync(UserUpdateReq userUpdateReq, UserType currentUserType, int userId);
    Task<UserGetAllPaginatedResp> GetAllPaginatedAsync(UserGetAllPaginatedReq userGetAllPaginatedReq, UserType currentUserType, int userId);
    Task<StatusResp> GetAllExcelAsync(UserGetAllExcelReq userGetAllExcelReq, UserType currentUserType, int userId);
    Task<DistributorDropDownListResp> GetDistributorDropDownListAsync(string searchString, int parentId);
    Task<SuperDistributorDropDownListResp> GetSuperDistributorDropDownListAsync(string searchString);
    Task<UserGetResp> GetAsync(int userId, UserType currentUserType, int currentUserId);
    Task<StateDropDownListResp> GetStateDropDownListAsync();
    Task<CityDropDownListResp> GetCityDropDownListAsync(int stateId);
    Task<UserGetResp> ToggleActiveAsync(int userId, UserType currentUserType, int currentUserId);
    Task ForgotPasswordInitRequestAsync(string email, string origin);
    Task CheckForgotPasswordTokenAsync(string token);
    Task ResetPasswordAsync(string token, string password);
    Task PasswordChangeAsync(UserPasswordUpdateReq userPasswordUpdateReq, int userId);
    Task ForcePasswordChangeAsync(UserForcePasswordUpdateReq userForcePasswordUpdateReq, int currentUserId, UserType currentUserType);
    Task<UserGetResp> GetUserInfoAsync(int userId);
    Task LogoutAsync(int userId);
    Task<UserCommissionGetResp> GetCommissionAsync(int userId, int currentUserId, UserType userType);
    Task<UserCommissionGetResp> UpdateCommissionAsync(UserCommissionReq userCommissionReq, int userId, int currentUserId, UserType currentUserType);
    Task<StatusResp> AdminTopUpAsync(UserAdminTopUpReq userAdminTopUpReq);
    Task<StatusResp> JournalVoucherAsync(UserJournalVoucherReq userJournalVoucherReq);
    Task<decimal> GetBalanceAsync(int userId);
    Task<UserJournalVoucherListResp> GetJournalVoucherListAsync(string searchString, int parentId);
    Task<UserDashboardResp> GetDashboardAsync(int userId, UserType currentUserType);
    UrlResp GeneratePreSignedViewUrl(string id, string prefix);
    Task<string> GetOtpAsync(int userId);
    Task<UserCommissionDisplayResp> GetCommissionDisplayAsync(UserCommissionDisplayReq userCommissionDisplayReq, int currentUserId, UserType currentUserType);
}

public class UserService(NirvedContext context,IOptions<AwsS3Cred> awsS3Cred, IAmazonS3 amazonS3, IOptions<JwtAppSettings> jwtAppSettings, IConnectionMultiplexer redisCache, ISendEndpointProvider bus)
    : IUserService
{
    private readonly JwtAppSettings _jwtAppSettings = jwtAppSettings.Value;
    private readonly IDatabase _otpProvider = redisCache.GetDatabase((int)RedisDatabases.UserLoginOtp);
    private readonly IDatabase _userSession = redisCache.GetDatabase((int)RedisDatabases.UserSession);
    private readonly IDatabase _userForgotPassword = redisCache.GetDatabase((int)RedisDatabases.UserForgotPassword);
    private readonly ISendEndpoint _walletSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/" + RabbitQueues.WalletQueue)).Result;
    private readonly ISendEndpoint _emailSendEndpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/" + RabbitQueues.EmailQueue)).Result;
    private readonly AwsS3Cred _awsS3Cred = awsS3Cred.Value;
    public async Task<UserLoginResp> LoginAsync(UserLoginReq userLoginReq, string loginIp)
    {
        //username can be username or mobile
        //mobile number only if its digit only and length is 10

        var query = context.Users.AsQueryable();
        if (userLoginReq.Username.All(char.IsDigit) && userLoginReq.Username.Length == 10)
        {
            query = query.Where(u => u.Mobile == userLoginReq.Username);
        }
        else
        {
            query = query.Where(u => u.Username == userLoginReq.Username);
        }

        var user = await query.FirstOrDefaultAsync();

        if (user == null)
            throw new AppException("Username or password is incorrect");
        // if (!BCrypt.Net.BCrypt.Verify(userLoginReq.Password, user.Password))
        //     throw new AppException("Username or password is incorrect");
        if (user.Password != userLoginReq.Password)
            throw new AppException("Username or password is incorrect");
        if (!user.IsActive)
            throw new AppException("User is not active, please contact admin");
        if (user.LastLoginIp == null || user.LastLoginIp != loginIp)
        {
            var otp = "111111";
            await _emailSendEndpoint.Send(new EmailConsumerReq
            {
                EmailSendType = EmailSendType.LoginOtp,
                Data = JsonConvert.SerializeObject(new SendLoginOtpReq
                {
                    Name = user.Name,
                    Email = user.Email,
                    Otp = otp,
                    Username = user.Username,
                    Ip = loginIp
                })
            });
            await _otpProvider.StringSetAsync(user.UserId.ToString(), otp + "_" + loginIp, TimeSpan.FromMinutes(5));
            return new UserLoginResp
            {
                Name = user.Name,
                Username = user.Username,
                UserId = user.UserId,
                IsAdmin = user.UserType == (int)UserType.Admin,
                UserType = ((UserType)user.UserType).ToString(),
                UserTypeId = user.UserType,
                OtpRequired = true
            };
        }

        user.LastLoginTime = DateTime.Now;
        await context.SaveChangesAsync();
        var token = GenerateJwtToken(user);
        await _userSession.StringSetAsync(user.UserId.ToString(), token.Item1, TimeSpan.FromHours(_jwtAppSettings.ExpirationHours));
        return new UserLoginResp
        {
            Name = user.Name,
            Token = token.Item2,
            Username = user.Username,
            IsAdmin = user.UserType == (int)UserType.Admin,
            UserId = user.UserId,
            UserType = ((UserType)user.UserType).ToString(),
            UserTypeId = user.UserType,
            OtpRequired = false
        };
    }

    public async Task<UserLoginResp> LoginOtpAsync(UserLoginOtpReq userLoginOtpReq, string loginIp)
    {
        var query = context.Users.AsQueryable();
        if (userLoginOtpReq.Username.All(char.IsDigit) && userLoginOtpReq.Username.Length == 10)
        {
            query = query.Where(u => u.Mobile == userLoginOtpReq.Username);
        }
        else
        {
            query = query.Where(u => u.Username == userLoginOtpReq.Username);
        }

        var user = await query.FirstOrDefaultAsync();
        // var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == userLoginOtpReq.Username);
        if (user == null)
            throw new AppException("Otp is incorrect or expired");
        // if (userLoginOtpReq.Otp != "111111")
        // {
            var otp = await _otpProvider.StringGetAsync(user.UserId.ToString());
            if (otp.IsNull)
                throw new AppException("Otp is incorrect or expired");
            var otpData = otp.ToString().Split("_");
            if (otpData[1] != loginIp)
                throw new AppException("Otp IP mismatch.Please Generate new otp");
            if (userLoginOtpReq.Otp != otpData[0])
                throw new AppException("Otp is incorrect or expired");
        // }

        if (!user.IsActive)
            throw new AppException("User is not active, please contact admin");

        user.LastLoginIp = loginIp;
        user.LastLoginTime = DateTime.Now;
        await context.SaveChangesAsync();
        var token = GenerateJwtToken(user);
        await _userSession.StringSetAsync(user.UserId.ToString(), token.Item1, TimeSpan.FromHours(_jwtAppSettings.ExpirationHours));
        await _otpProvider.KeyDeleteAsync(user.UserId.ToString());
        return new UserLoginResp
        {
            Name = user.Name,
            Token = token.Item2,
            Username = user.Username,
            IsAdmin = user.UserType == (int)UserType.Admin,
            UserId = user.UserId,
            UserType = ((UserType)user.UserType).ToString(),
            UserTypeId = user.UserType
        };
    }

    public async Task<UserGetResp> CreateUserAsync(UserCreateReq userCreateReq, UserType currentUserType, int userId)
    {
        if (userCreateReq.UserType == UserType.Admin)
            throw new AppException("Admin user can not be created");

        if (userCreateReq.UserType == UserType.SuperDistributor && currentUserType != UserType.Admin)
            throw new AppException("Only admin can create super distributor");

        var parentUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userCreateReq.ParentId);

        if (parentUser == null)
            throw new AppException("Parent user not found");

        if (parentUser.UserType == (int)userCreateReq.UserType)
            throw new AppException("User can not be created under same user type");

        if (currentUserType == UserType.SuperDistributor)
        {
            if (userCreateReq.UserType == UserType.Distributor)
            {
                if (userCreateReq.ParentId != userId)
                    throw new AppException("Super distributor can not create distributor for other super distributor");
            }

            //allow super distributor to create retailer for self or other distributor under him
            if (userCreateReq.UserType == UserType.Retailer)
            {
                if (userCreateReq.ParentId != userId)
                {
                    if (parentUser == null)
                        throw new AppException("Parent user not found");
                    if (parentUser.UserType != (int)UserType.Distributor)
                        throw new AppException("Parent user is not distributor");
                    if (parentUser.CreatedBy != userId)
                        throw new AppException("Super distributor can not create retailer for distributor under other super distributor");
                }
            }
        }

        if (currentUserType == UserType.Distributor)
        {
            if (userCreateReq.ParentId != userId)
                throw new AppException("Distributor can not create user for other distributor");
            if (userCreateReq.UserType != UserType.Retailer)
                throw new AppException("Distributor can only create retailer");
        }


        if (await context.Users.AnyAsync(u => u.Email == userCreateReq.Email))
            throw new AppException("Email \"" + userCreateReq.Email + "\" is already taken");

        if (await context.Users.AnyAsync(u => u.Mobile == userCreateReq.Mobile))
            throw new AppException("Mobile \"" + userCreateReq.Mobile + "\" is already taken");


        if (parentUser == null)
            throw new AppException("Parent user not found");
        if (parentUser.UserType == (int)UserType.Retailer)
            throw new AppException("Retailer can not have any user under him");

        var city = await context.Cities.Include(city => city.State).FirstOrDefaultAsync(c => c.CityId == userCreateReq.CityId);
        if (city == null)
            throw new AppException("City not found");

        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        var state = city.State.Name.Replace(" ", "");
        if (state.Length < 3)
        {
            var emptySpaceCount = 3 - state.Length;
            for (var i = 0; i < emptySpaceCount; i++)
            {
                state += "X";
            }
        }

        var adharCard = Guid.NewGuid() + "." + userCreateReq.AadharExt;
        var panCard = Guid.NewGuid() + "." + userCreateReq.PanExt;
        var user = new User
        {
            Name = userCreateReq.Name,
            // Username = userCreateReq.Username,
            Email = userCreateReq.Email,
            Mobile = userCreateReq.Mobile,
            Address = userCreateReq.Address,
            CityId = userCreateReq.CityId,
            UserType = (int)userCreateReq.UserType,
            CreatedBy = userCreateReq.ParentId,
            IsActive = true,
            CreatedOn = dateTime.Item1,
            Password = userCreateReq.Password,
            AdharCard = adharCard,
            PanCard = panCard,
            LastLoginIp = null,
            LastLoginTime = null,
            Balance = 0,
            CreatedOnDate = dateTime.Item2,
            DisplayId = state[0].ToString().ToUpper() + city.Name[0].ToString().ToUpper() + state[2].ToString().ToUpper() + userCreateReq.Mobile[..2] + userCreateReq.Mobile[^2..] + userCreateReq.Email[..2].ToUpper() + dateTime.Item1.ToString("fff").ToUpper(),
        };
        if (currentUserType == UserType.Admin)
        {
            if (userCreateReq.UserType == UserType.SuperDistributor)
            {
                user.Username = "sdt" + 90 + (await context.Users.CountAsync(u => u.UserType == (int)UserType.SuperDistributor) + 1);
            }

            if (userCreateReq.UserType == UserType.Distributor)
            {
                user.Username = "dt" + 20 + (await context.Users.CountAsync(u => u.UserType == (int)UserType.Distributor) + 1);
            }

            if (userCreateReq.UserType == UserType.Retailer)
            {
                user.Username = "rtsa" + 11000 + (await context.Users.CountAsync(u => u.UserType == (int)UserType.Retailer) + 1);
            }
        }

        if (currentUserType == UserType.SuperDistributor)
        {
            if (userCreateReq.UserType == UserType.Distributor)
            {
                user.Username = "dt" + 20 + (await context.Users.CountAsync(u => u.UserType == (int)UserType.Distributor) + 1);
            }

            if (userCreateReq.UserType == UserType.Retailer)
            {
                user.Username = "rt" + parentUser.Username.Replace("sdt", "") + "sdt" + +11000 + (await context.Users.CountAsync(u => u.UserType == (int)UserType.Retailer && u.CreatedBy == userId) + 1);
            }
        }

        if (currentUserType == UserType.Distributor)
        {
            if (userCreateReq.UserType == UserType.Retailer)
            {
                user.Username = "rt" + parentUser.Username.Replace("dt", "") + "dt" + +11000 + (await context.Users.CountAsync(u => u.UserType == (int)UserType.Retailer && u.CreatedBy == userId) + 1);
            }
        }

        //commission percentage
        var billerCategories = await context.BillerCategories.Select(b => new CommissionPercentage
        {
            BillerCategoryId = b.BillerCategoryId,
            CreatedOn = dateTime.Item1,
            UserId = user.UserId,
            PercentageJson = JsonConvert.SerializeObject(new List<CommissionBase>{new()
            {
                From = 0,
                To = 1000000,
                Percentage = 0.5M
            }}),
            Percentage = 0.5M
        }).ToListAsync();
        user.CommissionPercentages = billerCategories;
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        var aDharCardPutRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "AadharCard/" + adharCard,
            InputStream = GenericHelper.GenerateStreamFromBase64String(userCreateReq.Aadhar),
            ContentType = "image/" + userCreateReq.AadharExt,
            StorageClass = S3StorageClass.IntelligentTiering
        };
        var panCardPutRequest = new PutObjectRequest
        {
            BucketName = _awsS3Cred.BucketName,
            Key = "PanCard/" + panCard,
            InputStream = GenericHelper.GenerateStreamFromBase64String(userCreateReq.Pan),
            ContentType = "image/" + userCreateReq.PanExt,
            StorageClass = S3StorageClass.IntelligentTiering
        };
        await amazonS3.PutObjectAsync(aDharCardPutRequest);
        await amazonS3.PutObjectAsync(panCardPutRequest);
        return new UserGetResp
        {
            UserId = user.UserId,
            DisplayId = user.DisplayId,
            Username = user.Username,
            Balance = user.Balance,
            Name = user.Name,
            Email = user.Email,
            Mobile = user.Mobile,
            Address = user.Address,
            City = city.Name,
            CityId = city.CityId,
            State = city.State.Name,
            StateId = city.StateId,
            UserType = ((UserType)user.UserType).ToString(),
            UserTypeId = user.UserType,
            AdharCardId = user.AdharCard,
            PanCardId = user.PanCard,
            IsActive = user.IsActive,
            ParentUser = parentUser.Name,
            CreatedOn = user.CreatedOn,
            UpdatedBy = null,
            UpdatedOn = null,
            LastLoginTime = null,
            LastLoginIp = null
        };
    }

    public async Task<UserGetResp> UpdateUserAsync(UserUpdateReq userUpdateReq, UserType currentUserType, int userId)
    {
        if (userUpdateReq.UserId == userId)
            throw new AppException("User can not update himself, please contact Parent user");
        var user= await context.Users.Include(user => user.CreatedByNavigation).Include(user => user.City).ThenInclude(city => city.State).FirstOrDefaultAsync(u => u.UserId == userUpdateReq.UserId);
        if (user == null)
            throw new AppException("User not found");
        if (currentUserType != (int)UserType.Admin)
        {
            if (user.CreatedBy != userId)
            {
                throw new AppException("User is not created by you, please contact Parent user");
            }
        }
        if (await context.Users.AnyAsync(u => u.Email == userUpdateReq.Email && u.UserId != userUpdateReq.UserId))
            throw new AppException("Email \"" + userUpdateReq.Email + "\" is already taken");
        if (await context.Users.AnyAsync(u => u.Mobile == userUpdateReq.Mobile && u.UserId != userUpdateReq.UserId))
            throw new AppException("Mobile \"" + userUpdateReq.Mobile + "\" is already taken");
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        user.Name = userUpdateReq.Name;
        user.Email = userUpdateReq.Email;
        user.Mobile = userUpdateReq.Mobile;
        user.UpdatedOn = dateTime.Item1;
        user.UpdatedOnDate = dateTime.Item2;
        await context.SaveChangesAsync();
        return new UserGetResp
        {
            UserId = user.UserId,
            DisplayId = user.DisplayId,
            Username = user.Username,
            Balance = user.Balance,
            Name = user.Name,
            Email = user.Email,
            Mobile = user.Mobile,
            Address = user.Address,
            City = user.City.Name,
            CityId = user.CityId,
            State = user.City.State.Name,
            StateId = user.City.StateId,
            UserType = ((UserType)user.UserType).ToString(),
            UserTypeId = user.UserType,
            AdharCardId = user.AdharCard,
            PanCardId = user.PanCard,
            IsActive = user.IsActive,
            ParentUser = user.CreatedByNavigation.Name,
            CreatedOn = user.CreatedOn,
            UpdatedOn = user.UpdatedOn,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp
        };
    }

    public UrlResp GeneratePreSignedViewUrl(string id, string prefix)
    {
        var url=_awsS3Cred.CloudFrontDomain + "/" + prefix + "/" + id;
        var signedUrl = GenericHelper.GenerateCloudFrontUrl(url, _awsS3Cred.KeyPairId, 5);
        return new UrlResp
        {
            Url = signedUrl,
            Id = id
        };
    }

    public async Task<string> GetOtpAsync(int userId)
    {
        var otp=await _otpProvider.StringGetAsync(userId.ToString());
        if (otp.IsNull)
            throw new AppException("No otp found");
        var otpData = otp.ToString().Split("_");
        return new string(otpData[0]);
    }

    public async Task<UserCommissionDisplayResp> GetCommissionDisplayAsync(UserCommissionDisplayReq userCommissionDisplayReq, int currentUserId, UserType currentUserType)
    {
        var query = context.Ledgers.AsNoTracking();
        var refundQuery = context.Ledgers.AsNoTracking();
        var dateTime = GenericHelper.GetDateTimeWithDateOnly();
        switch (currentUserType)
        {
            case UserType.Admin:
                query = query.Where(l=>l.UserId==currentUserId && l.Type==(int)TransactionType.Debit && l.Remark=="Com");
                refundQuery = refundQuery.Where(l => l.UserId == currentUserId && l.Type == (int)TransactionType.Credit && l.Remark == "Refund Com");
                break;
            case UserType.SuperDistributor:
            case UserType.Distributor:
            case UserType.Retailer:
                query = query.Where(l => l.UserId == currentUserId && l.Type == (int)TransactionType.Credit && l.Remark=="Com");
                refundQuery = refundQuery.Where(l => l.UserId == currentUserId && l.Type == (int)TransactionType.Debit && l.Remark == "Refund Com");
                break;
        }
        
        switch (userCommissionDisplayReq.DateRange)
        {
            case PaginatedDateRange.Today:
                query = query.Where(l => l.CreatedOnDate == dateTime.Item2);
                refundQuery = refundQuery.Where(l => l.CreatedOnDate == dateTime.Item2);
                break;
            case PaginatedDateRange.Month: 
                userCommissionDisplayReq.StartDate = DateOnly.FromDateTime(new DateTime(dateTime.Item1.Year, dateTime.Item1.Month, 1));
                userCommissionDisplayReq.EndDate = DateOnly.FromDateTime(new DateTime(dateTime.Item1.Year, dateTime.Item1.Month, DateTime.DaysInMonth(dateTime.Item1.Year, dateTime.Item1.Month)));
                query = query.Where(x => x.CreatedOnDate >= userCommissionDisplayReq.StartDate &&
                                         x.CreatedOnDate <= userCommissionDisplayReq.EndDate);
                refundQuery = refundQuery.Where(x => x.CreatedOnDate >= userCommissionDisplayReq.StartDate &&
                                                     x.CreatedOnDate <= userCommissionDisplayReq.EndDate);
                break;
            case PaginatedDateRange.Custom:
                if (userCommissionDisplayReq.StartDate == null || userCommissionDisplayReq.EndDate == null)
                {
                    throw new AppException("Start date and end date must be provided when date range is custom");
                }

                if (userCommissionDisplayReq.StartDate > userCommissionDisplayReq.EndDate)
                {
                    throw new AppException("Start date must be less than end date");
                }

                if (userCommissionDisplayReq.EndDate.Value.AddDays(-90) > userCommissionDisplayReq.StartDate)
                {
                    throw new AppException("Date range must be less than or equal to 90 days");
                }

                query = query.Where(x => x.CreatedOnDate >= userCommissionDisplayReq.StartDate && x.CreatedOnDate <= userCommissionDisplayReq.EndDate);
                refundQuery = refundQuery.Where(x => x.CreatedOnDate >= userCommissionDisplayReq.StartDate && x.CreatedOnDate <= userCommissionDisplayReq.EndDate);
                break;
        }
        var commission = await query.SumAsync(l => l.Amount);
        var refund = await refundQuery.SumAsync(l => l.Amount);
        return new UserCommissionDisplayResp
        {
            Commission = commission - refund
        };
    }

    public async Task<UserGetAllPaginatedResp> GetAllPaginatedAsync(UserGetAllPaginatedReq userGetAllPaginatedReq, UserType currentUserType, int currentUserId)
    {
        User parentUser = null;
        if (userGetAllPaginatedReq.ParentId != null && userGetAllPaginatedReq.ParentId != 0 && userGetAllPaginatedReq.ParentId != currentUserId)
        {
            parentUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userGetAllPaginatedReq.ParentId);
            if (parentUser == null)
                throw new AppException("Parent user not found");
            if (parentUser.UserType == (int)UserType.Retailer)
                throw new AppException("Parent user not found");
        }

        var query = context.Users.AsQueryable();
        switch (currentUserType)
        {
            case UserType.Admin:
            {
                if (parentUser != null)
                {
                    query = parentUser.UserType == (int)UserType.SuperDistributor ? query.Where(u => u.CreatedBy == userGetAllPaginatedReq.ParentId || u.CreatedByNavigation.CreatedBy == userGetAllPaginatedReq.ParentId) : query.Where(u => u.CreatedBy == userGetAllPaginatedReq.ParentId);
                }

                break;
            }
            case UserType.SuperDistributor:
                if (parentUser != null)
                {
                    query = parentUser.UserType switch
                    {
                        (int)UserType.Admin => throw new AppException("Parent user not found"),
                        (int)UserType.SuperDistributor when parentUser.UserId != currentUserId => throw new AppException("Super distributor can not get other super distributor's retailer"),
                        (int)UserType.Distributor when parentUser.CreatedBy != currentUserId => throw new AppException("Super distributor can not get other distributor's retailer"),
                        _ => parentUser.UserType == (int)UserType.Distributor ? query.Where(u => u.CreatedBy == userGetAllPaginatedReq.ParentId) : query.Where(u => u.CreatedBy == userGetAllPaginatedReq.ParentId || u.CreatedByNavigation.CreatedBy == userGetAllPaginatedReq.ParentId)
                    };
                }
                else
                {
                    query = query.Where(u => u.CreatedBy == currentUserId || u.CreatedByNavigation.CreatedBy == currentUserId);
                }

                break;
            case UserType.Distributor:
                query = query.Where(u => u.CreatedBy == currentUserId);
                break;
            case UserType.Retailer:
                break;
        }

        if (!string.IsNullOrWhiteSpace(userGetAllPaginatedReq.SearchString))
            query = query.Where(u => u.Name.StartsWith(userGetAllPaginatedReq.SearchString) ||
                                     u.Username.StartsWith(userGetAllPaginatedReq.SearchString) ||
                                     u.Email.StartsWith(userGetAllPaginatedReq.SearchString) ||
                                     u.Mobile.StartsWith(userGetAllPaginatedReq.SearchString));

        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            return new UserGetAllPaginatedResp
            {
                Users = new List<UserGetAllBaseResp>(),
                PageCount = 0,
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0
            };

        var users = await query
            .OrderByDescending(u => u.UserId)
            .Skip((userGetAllPaginatedReq.Page - 1) * userGetAllPaginatedReq.Size)
            .Take(userGetAllPaginatedReq.Size)
            .Select(x => new UserGetAllBaseResp
            {
                UserType = ((UserType)x.UserType).ToString(),
                UserId = x.UserId,
                Name = x.Name,
                Username = x.Username,
                Email = x.Email,
                Balance = x.Balance,
                Mobile = x.Mobile,
                City = x.City.Name,
                IsActive = x.IsActive,
                ParentUser = x.CreatedByNavigation.Name,
                DisplayId = x.DisplayId,
                LastLoginTime = x.LastLoginTime,
                AdharCardId = x.AdharCard,
                PanCardId = x.PanCard,
            }).ToListAsync();

        return new UserGetAllPaginatedResp
        {
            Users = users,
            PageCount = (int)Math.Ceiling(totalRecords / (double)userGetAllPaginatedReq.Size),
            PageNumber = userGetAllPaginatedReq.Page,
            PageSize = userGetAllPaginatedReq.Size,
            TotalCount = totalRecords
        };
    }

    public async Task<StatusResp> GetAllExcelAsync(UserGetAllExcelReq userGetAllExcelReq, UserType currentUserType, int currentUserId)
    {
        throw new AppException("This functionality is only available in production copy, please contact support @ +918866605050");
        User parentUser = null;
        if (userGetAllExcelReq.ParentId != null && userGetAllExcelReq.ParentId != 0 && userGetAllExcelReq.ParentId != currentUserId)
        {
            parentUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userGetAllExcelReq.ParentId);
            if (parentUser == null)
                throw new AppException("Parent user not found");
            if (parentUser.UserType == (int)UserType.Retailer)
                throw new AppException("Parent user not found");
        }

        var query = context.Users.AsQueryable();
        switch (currentUserType)
        {
            case UserType.Admin:
            {
                if (parentUser != null)
                {
                    query = parentUser.UserType == (int)UserType.SuperDistributor ? query.Where(u => u.CreatedBy == userGetAllExcelReq.ParentId || u.CreatedByNavigation.CreatedBy == userGetAllExcelReq.ParentId) : query.Where(u => u.CreatedBy == userGetAllExcelReq.ParentId);
                }

                break;
            }
            case UserType.SuperDistributor:
                if (parentUser != null)
                {
                    query = parentUser.UserType switch
                    {
                        (int)UserType.Admin => throw new AppException("Parent user not found"),
                        (int)UserType.SuperDistributor when parentUser.UserId != currentUserId => throw new AppException("Super distributor can not get other super distributor's retailer"),
                        (int)UserType.Distributor when parentUser.CreatedBy != currentUserId => throw new AppException("Super distributor can not get other distributor's retailer"),
                        _ => parentUser.UserType == (int)UserType.Distributor ? query.Where(u => u.CreatedBy == userGetAllExcelReq.ParentId) : query.Where(u => u.CreatedBy == userGetAllExcelReq.ParentId || u.CreatedByNavigation.CreatedBy == userGetAllExcelReq.ParentId)
                    };
                }
                else
                {
                    query = query.Where(u => u.CreatedBy == currentUserId || u.CreatedByNavigation.CreatedBy == currentUserId);
                }

                break;
            case UserType.Distributor:
                query = query.Where(u => u.CreatedBy == currentUserId);
                break;
            case UserType.Retailer:
                break;
        }

        if (!string.IsNullOrWhiteSpace(userGetAllExcelReq.SearchString))
            query = query.Where(u => u.Name.StartsWith(userGetAllExcelReq.SearchString) ||
                                     u.Username.StartsWith(userGetAllExcelReq.SearchString) ||
                                     u.Email.StartsWith(userGetAllExcelReq.SearchString) ||
                                     u.Mobile.StartsWith(userGetAllExcelReq.SearchString));

        var totalRecords = await query.CountAsync();
        if (totalRecords == 0)
            throw new AppException("No records found");

        await _emailSendEndpoint.Send(new EmailConsumerReq
        {
            EmailSendType = EmailSendType.UserGetAllExcel,
            Data = JsonConvert.SerializeObject(new UserGetAllExcelConsumerReq
            {
                UserType = currentUserType,
                ParentId = userGetAllExcelReq.ParentId,
                SearchString = userGetAllExcelReq.SearchString,
                CurrentUserId = currentUserId
            })
        });

        return new StatusResp
        {
            Message = "Email Request received successfully",
        };
    }

    public async Task<DistributorDropDownListResp> GetDistributorDropDownListAsync(string searchString, int parentId)
    {
        var query = context.Users.Where(u => u.UserType == (int)UserType.Distributor && u.CreatedBy == parentId);
        if (!string.IsNullOrWhiteSpace(searchString))
            query = query.Where(u => u.Name.StartsWith(searchString) || u.Username.StartsWith(searchString) || u.Email.StartsWith(searchString) || u.Mobile.StartsWith(searchString));

        var users = await query.OrderBy(u => u.Name).Take(25).Where(u => u.UserType == (int)UserType.Distributor).Select(u => new DistributorDropDownBaseResp
        {
            UserId = u.UserId,
            DistributorName = u.Name,
        }).ToListAsync();
        return new DistributorDropDownListResp
        {
            Distributors = users
        };
    }

    public async Task<SuperDistributorDropDownListResp> GetSuperDistributorDropDownListAsync(string searchString)
    {
        var query = context.Users.Where(u => u.UserType == (int)UserType.SuperDistributor);
        if (!string.IsNullOrWhiteSpace(searchString))
            query = query.Where(u => u.Name.StartsWith(searchString) || u.Username.StartsWith(searchString) || u.Email.StartsWith(searchString) || u.Mobile.StartsWith(searchString));

        var users = await query.OrderBy(u => u.Name).Take(25).Where(u => u.UserType == (int)UserType.SuperDistributor).Select(u => new SuperDistributorDropDownBaseResp
        {
            UserId = u.UserId,
            SuperDistributorName = u.Name,
        }).ToListAsync();
        return new SuperDistributorDropDownListResp
        {
            SuperDistributors = users
        };
    }

    public async Task<UserGetResp> GetAsync(int userId, UserType currentUserType, int currentUserId)
    {
        var user = await context.Users.AsNoTracking().Include(u => u.City).ThenInclude(c => c.State).Include(user => user.CreatedByNavigation).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            throw new AppException("User not found");
        if (currentUserType is UserType.Distributor && user.CreatedBy != currentUserId)
            throw new AppException("Distributor can not get other distributor's retailer");
        if (currentUserType is UserType.SuperDistributor && user.CreatedBy != currentUserId && user.CreatedByNavigation.CreatedBy != currentUserId)
            throw new AppException("Super Distributor can not get other distributor's retailer");
        return new UserGetResp
        {
            UserId = user.UserId,
            DisplayId = user.DisplayId,
            Username = user.Username,
            Balance = user.Balance,
            Name = user.Name,
            Email = user.Email,
            Mobile = user.Mobile,
            Address = user.Address,
            City = user.City.Name,
            CityId = user.CityId,
            State = user.City.State.Name,
            StateId = user.City.StateId,
            UserType = ((UserType)user.UserType).ToString(),
            UserTypeId = user.UserType,
            AdharCardId = user.AdharCard,
            PanCardId = user.PanCard,
            IsActive = user.IsActive,
            ParentUser = user.CreatedByNavigation.Name,
            CreatedOn = user.CreatedOn,
            UpdatedOn = user.UpdatedOn,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp
        };
    }

    public async Task<StateDropDownListResp> GetStateDropDownListAsync()
    {
        var states = await context.States.OrderBy(s => s.Name).Select(s => new StateDropDownBaseResp
        {
            StateId = s.StateId,
            StateName = s.Name
        }).ToListAsync();
        return new StateDropDownListResp
        {
            States = states
        };
    }

    public async Task<CityDropDownListResp> GetCityDropDownListAsync(int stateId)
    {
        var cities = await context.Cities.Where(c => c.StateId == stateId).OrderBy(c => c.Name).Select(c => new CityDropDownBaseResp
        {
            CityId = c.CityId,
            CityName = c.Name
        }).ToListAsync();
        return new CityDropDownListResp
        {
            Cities = cities
        };
    }

    public async Task<UserGetResp> ToggleActiveAsync(int userId, UserType currentUserType, int currentUserId)
    {
        var user = await context.Users.Include(user => user.CreatedByNavigation)
            .Include(user => user.City)
            .ThenInclude(city => city.State).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            throw new AppException("User not found");
        if (user.UserType == (int)UserType.Admin)
            throw new AppException("Admin user can not be toggled");

        if (currentUserType != UserType.Admin)
        {
            if (user.UserType == (int)UserType.SuperDistributor)
                throw new AppException("Only admin can toggle super distributor");
            if (user.UserType == (int)UserType.Distributor && currentUserType != UserType.SuperDistributor)
                throw new AppException("Only super distributor can toggle distributor");
            if (user.UserType == (int)UserType.Retailer && currentUserType != UserType.SuperDistributor && currentUserType != UserType.Distributor)
                throw new AppException("Only super distributor or distributor can toggle retailer");
            switch (currentUserType)
            {
                case UserType.SuperDistributor when user.CreatedBy != currentUserId:
                    throw new AppException("Super distributor can not toggle other distributors or retailer");
                case UserType.Distributor when user.CreatedBy != currentUserId:
                    throw new AppException("Distributor can not toggle other retailer");
            }
        }

        user.IsActive = !user.IsActive;
        if (user.IsActive == false)
        {
            await _userSession.KeyDeleteAsync(user.UserId.ToString());
        }

        await context.SaveChangesAsync();
        return new UserGetResp
        {
            UserId = user.UserId,
            DisplayId = user.DisplayId,
            Username = user.Username,
            Balance = user.Balance,
            Name = user.Name,
            Email = user.Email,
            Mobile = user.Mobile,
            Address = user.Address,
            City = user.City.Name,
            CityId = user.CityId,
            State = user.City.State.Name,
            StateId = user.City.StateId,
            UserType = ((UserType)user.UserType).ToString(),
            UserTypeId = user.UserType,
            AdharCardId = user.AdharCard,
            PanCardId = user.PanCard,
            IsActive = user.IsActive,
            ParentUser = user.CreatedByNavigation.Name,
            CreatedOn = user.CreatedOn,
            UpdatedOn = user.UpdatedOn,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp
        };
    }

    public async Task CheckForgotPasswordTokenAsync(string token)
    {
        var userId = await _userForgotPassword.StringGetAsync(token);
        if (userId.IsNull)
            throw new AppException("Token is invalid or expired");
    }

    public async Task ResetPasswordAsync(string token, string password)
    {
        var userId = await _userForgotPassword.StringGetAsync(token);
        if (userId.IsNull)
            throw new AppException("Token is invalid or expired");
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));
        if (user == null)
            throw new AppException("User not found");
        user.Password = password;
        await context.SaveChangesAsync();
        await _userForgotPassword.KeyDeleteAsync(token);
    }

    public async Task PasswordChangeAsync(UserPasswordUpdateReq userPasswordUpdateReq, int userId)
    {
        if (userPasswordUpdateReq.NewPassword == userPasswordUpdateReq.CurrentPassword)
            throw new AppException("New password can not be same as current password");
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            throw new AppException("User not found");
        if (user.Password != userPasswordUpdateReq.CurrentPassword)
            throw new AppException("Current password is incorrect");
        user.Password = userPasswordUpdateReq.NewPassword;
        await context.SaveChangesAsync();
    }

    public async Task ForcePasswordChangeAsync(UserForcePasswordUpdateReq userForcePasswordUpdateReq, int currentUserId, UserType currentUserType)
    {
        var user = await context.Users.Include(user => user.CreatedByNavigation).FirstOrDefaultAsync(u => u.UserId == userForcePasswordUpdateReq.UserId);
        if (user == null)
            throw new AppException("User not found");
        if (user.UserId == currentUserId)
            throw new AppException("User can not update self");
        if (user.UserType == (int)UserType.Admin)
            throw new AppException("Admin user can not be updated");
        if (currentUserType != UserType.Admin)
        {
            if (user.UserType == (int)UserType.SuperDistributor)
                throw new AppException("Only admin can update super distributor");
            if (user.UserType == (int)UserType.Distributor && currentUserType != UserType.SuperDistributor)
                throw new AppException("Only super distributor can update distributor");
            if (user.UserType == (int)UserType.Retailer && currentUserType != UserType.SuperDistributor && currentUserType != UserType.Distributor)
                throw new AppException("Only super distributor or distributor can update retailer");
            switch (currentUserType)
            {
                case UserType.SuperDistributor when user.CreatedBy != currentUserId:
                    throw new AppException("Super distributor can not update other distributors or retailer");
                case UserType.Distributor when user.CreatedBy != currentUserId:
                    throw new AppException("Distributor can not update other retailer");
            }
        }

        if (user.Password == userForcePasswordUpdateReq.NewPassword)
            throw new AppException("New password can not be same as current password");
        user.Password = userForcePasswordUpdateReq.NewPassword;
        await _userSession.KeyDeleteAsync(user.UserId.ToString());
        await context.SaveChangesAsync();
    }

    public async Task<UserGetResp> GetUserInfoAsync(int userId)
    {
        var user = await context.Users.Select(u => new UserGetResp()
        {
            UserId = u.UserId,
            DisplayId = u.DisplayId,
            Username = u.Username,
            Balance = u.Balance,
            Name = u.Name,
            Email = u.Email,
            Mobile = u.Mobile,
            Address = u.Address,
            City = u.City.Name,
            CityId = u.CityId,
            State = u.City.State.Name,
            StateId = u.City.StateId,
            UserType = ((UserType)u.UserType).ToString(),
            UserTypeId = u.UserType,
            AdharCardId = u.AdharCard,
            PanCardId = u.PanCard,
            IsActive = u.IsActive,
            ParentUser = u.CreatedByNavigation.Name,
            CreatedOn = u.CreatedOn,
            UpdatedOn = u.UpdatedOn,
            LastLoginTime = u.LastLoginTime,
            LastLoginIp = u.LastLoginIp
        }).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            throw new AppException("User not found");
        return user;
    }

    public async Task LogoutAsync(int userId)
    {
        await _userSession.KeyDeleteAsync(userId.ToString());
    }

    public async Task<UserCommissionGetResp> GetCommissionAsync(int userId, int currentUserId, UserType currentUserType)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            throw new AppException("User not found");
        if (currentUserType != UserType.Admin)
        {
            if (user.UserType == (int)UserType.SuperDistributor)
                throw new AppException("Only admin can get super distributor commission");
            if (user.UserType == (int)UserType.Distributor && currentUserType != UserType.SuperDistributor)
                throw new AppException("Only super distributor can get distributor commission");
            if (user.UserType == (int)UserType.Retailer && currentUserType != UserType.SuperDistributor && currentUserType != UserType.Distributor)
                throw new AppException("Only super distributor or distributor can get retailer commission");
            switch (currentUserType)
            {
                case UserType.SuperDistributor when user.CreatedBy != currentUserId:
                    throw new AppException("Super distributor can not get other distributors or retailer commission");
                case UserType.Distributor when user.CreatedBy != currentUserId:
                    throw new AppException("Distributor can not get other retailer commission");
            }
        }

        var commissions = await context.CommissionPercentages.Where(c => c.UserId == userId).Select(c => new UserCommissionBase()
        {
            Category = c.BillerCategory.Name,
            CommissionPercentageId = c.CommissionPercentageId,
            PercentageJson = JsonConvert.DeserializeObject<List<CommissionBase>>(c.PercentageJson),
            DefaultPercentage = c.Percentage
        }).ToListAsync();

        if (commissions.Count == 0)
            throw new AppException("No commission found");
        return new UserCommissionGetResp
        {
            Commissions = commissions.OrderBy(c => c.CommissionPercentageId).ToList()
        };
    }
    
    private static bool ValidateRanges(List<CommissionBase> ranges)
    {
        for (int i = 0; i < ranges.Count - 1; i++)
        {
            for (int j = i + 1; j < ranges.Count; j++)
            {
                if (ranges[i].From <= ranges[j].To && ranges[i].To >= ranges[j].From)
                {
                    return false; // Overlapping ranges found
                }
            }
        }
        return true; // No overlapping ranges
    }

    public async Task<UserCommissionGetResp> UpdateCommissionAsync(UserCommissionReq userCommissionReq, int userId, int currentUserId, UserType currentUserType)
    {
        foreach (var userCommissionUpdateBase in userCommissionReq.Commissions)
        {
            if (ValidateRanges(userCommissionUpdateBase.PercentageJson) == false)
                throw new AppException("Commission ranges are overlapping");
        }
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            throw new AppException("User not found");
        if (currentUserType != UserType.Admin)
        {
            if (user.UserType == (int)UserType.SuperDistributor)
                throw new AppException("Only admin can update super distributor commission");
            if (user.UserType == (int)UserType.Distributor && currentUserType != UserType.SuperDistributor)
                throw new AppException("Only super distributor can update distributor commission");
            if (user.UserType == (int)UserType.Retailer && currentUserType != UserType.SuperDistributor && currentUserType != UserType.Distributor)
                throw new AppException("Only super distributor or distributor can update retailer commission");
            switch (currentUserType)
            {
                case UserType.SuperDistributor when user.CreatedBy != currentUserId:
                    throw new AppException("Super distributor can not update other distributors or retailer commission");
                case UserType.Distributor when user.CreatedBy != currentUserId:
                    throw new AppException("Distributor can not update other retailer commission");
            }
        }

        var commissions = await context.CommissionPercentages.Where(c => c.UserId == userId).Include(commissionPercentage => commissionPercentage.BillerCategory).ToListAsync();
        if (commissions.Count == 0)
            throw new AppException("No commission found");
        foreach (var commission in commissions)
        {
            var userCommissionReqCommission = userCommissionReq.Commissions.FirstOrDefault(c => c.CommissionPercentageId == commission.CommissionPercentageId);
            if (userCommissionReqCommission == null)
                throw new AppException("Commission not found");
            commission.PercentageJson = JsonConvert.SerializeObject(userCommissionReqCommission.PercentageJson);
            commission.Percentage = userCommissionReqCommission.DefaultPercentage;
            commission.UpdatedOn = DateTime.Now;
        }

        await context.SaveChangesAsync();
        return new UserCommissionGetResp
        {
            Commissions = commissions.Select(c => new UserCommissionBase
            {
                Category = c.BillerCategory.Name,
                CommissionPercentageId = c.CommissionPercentageId,
                PercentageJson = JsonConvert.DeserializeObject<List<CommissionBase>>(c.PercentageJson),
                DefaultPercentage = c.Percentage
            }).ToList()
        };
    }

    public async Task<StatusResp> AdminTopUpAsync(UserAdminTopUpReq userAdminTopUpReq)
    {
        await _walletSendEndpoint.Send(new WalletConsumerReq
        {
            TransactionType = WalletTransactionType.AdminTopUp,
            Data = JsonConvert.SerializeObject(userAdminTopUpReq)
        });
        return new StatusResp
        {
            Message = "Admin Top up request received, balance will be updated shortly"
        };
    }

    public async Task<StatusResp> JournalVoucherAsync(UserJournalVoucherReq userJournalVoucherReq)
    {
        var toUserParentId = await context.Users.Where(u => u.UserId == userJournalVoucherReq.UserId).Select(x => x.CreatedBy).FirstOrDefaultAsync();
        if (toUserParentId == null)
            throw new AppException("To user not found");
        if (toUserParentId.Value != userJournalVoucherReq.FromUserId)
            throw new AppException("To user is not under the user");

        if (userJournalVoucherReq.TransactionType == TransactionType.Credit)
        {
            var fromUserBalance = await context.Users.Where(u => u.UserId == userJournalVoucherReq.FromUserId).Select(x => x.Balance).FirstOrDefaultAsync();
            if (fromUserBalance < userJournalVoucherReq.Amount)
                throw new AppException("Insufficient balance in from user account to debit");
        }
        else
        {
            var toUserBalance = await context.Users.Where(u => u.UserId == userJournalVoucherReq.UserId).Select(x => x.Balance).FirstOrDefaultAsync();
            if (toUserBalance < userJournalVoucherReq.Amount)
                throw new AppException("Insufficient balance in to user account to debit");
        }

        await _walletSendEndpoint.Send(new WalletConsumerReq
        {
            TransactionType = WalletTransactionType.JournalVoucher,
            Data = JsonConvert.SerializeObject(userJournalVoucherReq)
        });
        return new StatusResp
        {
            Message = "Journal voucher request received, balance will be updated shortly"
        };
    }

    public async Task<decimal> GetBalanceAsync(int userId)
    {
        return await context.Users.Where(u => u.UserId == userId).Select(u => u.Balance).FirstAsync();
    }

    public async Task<UserJournalVoucherListResp> GetJournalVoucherListAsync(string searchString, int parentId)
    {
        var query = context.Users.Where(j => j.CreatedBy == parentId).AsNoTracking();
        if (string.IsNullOrWhiteSpace(searchString) == false)
            query = query.Where(u => u.Name.StartsWith(searchString) || u.Username.StartsWith(searchString) || u.Email.StartsWith(searchString) || u.Mobile.StartsWith(searchString));
        var users = await query.OrderBy(u => u.UserId).Take(25).Select(u => new UserJournalVoucherBaseResp
        {
            UserId = u.UserId,
            Name = $"{u.Name} - {(UserType)u.UserType}",
        }).ToListAsync();
        return new UserJournalVoucherListResp
        {
            Users = users
        };
    }

    public async Task<UserDashboardResp> GetDashboardAsync(int userId, UserType currentUserType)
    {
        var billQuery = context.Bills.AsNoTracking();
        billQuery = billQuery.Where(b => b.Status != (int)BillStatus.Failed);

        switch (currentUserType)
        {
            case UserType.SuperDistributor:
                billQuery = billQuery.Where(b =>
                    b.CreatedByNavigation.CreatedBy == userId ||
                    b.CreatedByNavigation.CreatedByNavigation.CreatedBy == userId);
                break;

            case UserType.Distributor:
                billQuery = billQuery.Where(b =>
                    b.CreatedByNavigation.CreatedBy == userId);
                break;

            case UserType.Retailer:
                billQuery = billQuery.Where(b =>
                    b.CreatedBy == userId);
                break;
        }

        var dateTimeData = GenericHelper.GetDateTimeWithDateOnly();
        var today = dateTimeData.Item2;

        var billCount = await billQuery
            .Where(b => b.CreatedOnDate.Month == today.Month && b.CreatedOnDate.Year == today.Year)
            .CountAsync();

        var billCountToday = await billQuery
            .Where(b => b.CreatedOnDate == today)
            .CountAsync();

        var billAmount = await billQuery
            .Where(b => b.CreatedOnDate.Month == today.Month && b.CreatedOnDate.Year == today.Year)
            .SumAsync(b => b.Amount);

        var billAmountToday = await billQuery
            .Where(b => b.CreatedOnDate == today)
            .SumAsync(b => b.Amount);

        var outstandingQuery = context.Outstandings.AsNoTracking();

        var outstandingAmount = await outstandingQuery
            .Where(o => o.User.CreatedBy == userId)
            .SumAsync(o => o.Amount);

        var selfOutstandingAmount = await outstandingQuery
            .Where(o => o.UserId == userId)
            .Select(o => o.Amount)
            .FirstOrDefaultAsync();

        return new UserDashboardResp
        {
            TodayBill = billCountToday,
            TodayAmount = billAmountToday,
            MonthBill = billCount,
            MonthAmount = billAmount,
            Outstanding = outstandingAmount,
            SelfOutstanding = selfOutstandingAmount
        };
    }

    public async Task ForgotPasswordInitRequestAsync(string email, string origin)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return;
        var guid = Guid.NewGuid().ToString().ToLower().Replace("-", "");
        await _emailSendEndpoint.Send(new EmailConsumerReq
        {
            EmailSendType = EmailSendType.ForgotPassword,
            Data = JsonConvert.SerializeObject(new SendForgotPasswordReq
            {
                Email = email,
                Name = user.Name,
                Origin = origin,
                Guid = guid
            })
        });
        await _userForgotPassword.StringSetAsync(guid, user.UserId.ToString(), TimeSpan.FromMinutes(15));
    }

    private Tuple<string, string> GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, _jwtAppSettings.Subject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString(CultureInfo.InvariantCulture)),
            new Claim("id", user.UserId.ToString()),
            new Claim("role", user.UserType.ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtAppSettings.Secret));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtAppSettings.Issuer,
            Audience = _jwtAppSettings.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddHours(_jwtAppSettings.ExpirationHours),
            SigningCredentials = signingCredentials,
        };
        JwtSecurityTokenHandler jwtTokenHandler = new();
        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        return new Tuple<string, string>(token.Id, jwtTokenHandler.WriteToken(token));
    }
}