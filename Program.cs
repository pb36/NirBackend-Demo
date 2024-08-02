using System;
using System.Net;
using System.Text;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NirvedBackend.Consumers;
using NirvedBackend.Entities;
using NirvedBackend.Helpers;
using NirvedBackend.Models.Generic;
using NirvedBackend.Services;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseSentry();
var configuration = builder.Configuration;
builder.Services.AddHttpClient();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<WalletConsumer>();
    x.AddConsumer<EmailConsumer>();
    x.AddConsumer<WhatsappMessageConsumer>();
    x.AddConsumer<RechargeConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri("rabbitmq://127.0.0.1"), h =>
        {
            h.Username(configuration["RabbitMQ:UserName"]);
            h.Password(configuration["RabbitMQ:Password"]);
        });
        cfg.ReceiveEndpoint(RabbitQueues.WalletQueue.ToString(), ep =>
        {
            ep.PrefetchCount = 300;
            ep.UseConcurrencyLimit(1);
            ep.UseMessageRetry(r => r.None());
            ep.ConfigureConsumer<WalletConsumer>(context);
        });
        cfg.ReceiveEndpoint(RabbitQueues.EmailQueue.ToString(), ep =>
        {
            ep.PrefetchCount = 100;
            ep.UseConcurrencyLimit(3);
            ep.UseMessageRetry(r => r.Interval(3, 10));
            ep.ConfigureConsumer<EmailConsumer>(context);
        });
        cfg.ReceiveEndpoint(RabbitQueues.WhatsappMessageQueue.ToString(), ep =>
        {
            ep.PrefetchCount = 100;
            ep.UseConcurrencyLimit(3);
            ep.UseMessageRetry(r => r.Interval(3, 10));
            ep.ConfigureConsumer<WhatsappMessageConsumer>(context);
        });
        cfg.ReceiveEndpoint(RabbitQueues.RechargeQueue.ToString(), ep =>
        {
            ep.PrefetchCount = 100;
            ep.UseConcurrencyLimit(1);
            ep.UseMessageRetry(r => r.None());
            ep.ConfigureConsumer<RechargeConsumer>(context);
        });
    });
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddDbContextPool<NirvedContext>(options =>
{
    var connectionString = configuration.GetConnectionString("Nirved");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.CommandTimeout(120);
            mySqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
        });
});
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(configuration.GetConnectionString("RedisConnection") ?? "localhost"));
builder.Services.Configure<JwtAppSettings>(configuration.GetSection("JWT"));
builder.Services.Configure<AwsS3Cred>(configuration.GetSection("AwsS3Cred"));
builder.Services.Configure<AwsSesAppSettings>(configuration.GetSection("AwsSes"));
builder.Services.Configure<WhatsappNotificationConfig>(configuration.GetSection("WhatsappNotification"));
builder.Services.Configure<RechargeApiInfoSettings>(configuration.GetSection("RechargeAPIInfo"));
builder.Services.Configure<BillConfig>(configuration.GetSection("BillConfig"));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBillerService, BillerService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<IUserNoticeService, UserNoticeService>();
builder.Services.AddScoped<IBillPaymentService, BillPaymentService>();
builder.Services.AddScoped<ITopUpService, TopUpService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();

builder.Services.AddAWSService<IAmazonS3>(new AWSOptions
{
    Credentials = new BasicAWSCredentials(configuration["AwsS3Cred:AccessKey"], configuration["AwsS3Cred:SecretKey"]),
    Region = Amazon.RegionEndpoint.GetBySystemName(configuration["AwsS3Cred:Region"]),
});

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.OrderActionsBy((apiDesc) => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}_{apiDesc.RelativePath}");
    
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1.0.0",
        Title = "Nirved MultiServices API",
        Description = "Nirved MultiServices API",
    });

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddControllers(options =>
    {
        options.Filters.Add(typeof(ModelStateValidationActionFilterAttribute));
        options.Filters.Add(new ProducesAttribute("application/json"));
        options.Filters.Add(new ConsumesAttribute("application/json"));
    })
    .ConfigureApiBehaviorOptions(options => { options.SuppressModelStateInvalidFilter = true; }).AddNewtonsoftJson();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Admin.ToString(), policy => policy.RequireRole(((int)UserType.Admin).ToString()));
    options.AddPolicy(Policies.SuperDistributor.ToString(), policy => policy.RequireRole(((int)UserType.SuperDistributor).ToString()));
    options.AddPolicy(Policies.Distributor.ToString(), policy => policy.RequireRole(((int)UserType.Distributor).ToString()));
    options.AddPolicy(Policies.Retailer.ToString(), policy => policy.RequireRole(((int)UserType.Retailer).ToString()));
    options.AddPolicy(Policies.AdminOrSuperDistributor.ToString(), policy => policy.RequireRole(((int)UserType.Admin).ToString(), ((int)UserType.SuperDistributor).ToString()));
    options.AddPolicy(Policies.AdminOrDistributor.ToString(), policy => policy.RequireRole(((int)UserType.Admin).ToString(), ((int)UserType.Distributor).ToString()));
    options.AddPolicy(Policies.AdminOrRetailer.ToString(), policy => policy.RequireRole(((int)UserType.Admin).ToString(), ((int)UserType.Retailer).ToString()));
    options.AddPolicy(Policies.SuperDistributorOrDistributor.ToString(), policy => policy.RequireRole(((int)UserType.SuperDistributor).ToString(), ((int)UserType.Distributor).ToString()));
    options.AddPolicy(Policies.SuperDistributorOrRetailer.ToString(), policy => policy.RequireRole(((int)UserType.SuperDistributor).ToString(), ((int)UserType.Retailer).ToString()));
    options.AddPolicy(Policies.DistributorOrRetailer.ToString(), policy => policy.RequireRole(((int)UserType.Distributor).ToString(), ((int)UserType.Retailer).ToString()));
    options.AddPolicy(Policies.AdminOrSuperDistributorOrDistributor.ToString(), policy => policy.RequireRole(((int)UserType.Admin).ToString(), ((int)UserType.SuperDistributor).ToString(), ((int)UserType.Distributor).ToString()));
    options.AddPolicy(Policies.AdminOrSuperDistributorOrRetailer.ToString(), policy => policy.RequireRole(((int)UserType.Admin).ToString(), ((int)UserType.SuperDistributor).ToString(), ((int)UserType.Retailer).ToString()));
    options.AddPolicy(Policies.AdminOrDistributorOrRetailer.ToString(), policy => policy.RequireRole(((int)UserType.Admin).ToString(), ((int)UserType.Distributor).ToString(), ((int)UserType.Retailer).ToString()));
    options.AddPolicy(Policies.SuperDistributorOrDistributorOrRetailer.ToString(), policy => policy.RequireRole(((int)UserType.SuperDistributor).ToString(), ((int)UserType.Distributor).ToString(), ((int)UserType.Retailer).ToString()));
    options.AddPolicy(Policies.All.ToString(), policy => policy.RequireRole(((int)UserType.Admin).ToString(), ((int)UserType.SuperDistributor).ToString(), ((int)UserType.Distributor).ToString(), ((int)UserType.Retailer).ToString()));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ClockSkew = TimeSpan.FromSeconds(15),
        ValidateLifetime = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = configuration["JWT:Audience"],
        ValidIssuer = configuration["JWT:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", cors =>
    {
        cors.SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials().SetPreflightMaxAge(TimeSpan.FromHours(24));
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy<string, ExcelRateLimitPolicy>("ExcelRateLimitPolicy");
    options.AddPolicy<string, BillRateLimitPolicy>("BillRateLimitPolicy");
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();
