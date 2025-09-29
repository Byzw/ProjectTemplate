using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using GoodSleepEIP;
using GoodSleepEIP.Swagger;
using System.Reflection;
using Quartz;
using Quartz.Simpl;
using AutoMapper;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FormOptions>(o =>
{
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartBodyLengthLimit = int.MaxValue;
    o.MemoryBufferThreshold = int.MaxValue;
});

// 註冊 Dapper/資料庫應用 服務
builder.Services.AddDapperHelper<SqlConnection>(builder.Configuration.GetSection(nameof(DapperHelperOptions)));   // only change: MSSQL: <SqlConnection>, MySQL: <MySqlConnection>, OracleConnection: <OracleConnection>

// 註冊 Email 服務
builder.Services.AddSingleton<IEmailService, EmailService>();

// 註冊"系統參數"服務
builder.Services.AddTransient<IParameterService, ParameterService>();

// 註冊"檔案上下傳"服務
builder.Services.AddTransient<IFileService, FileService>();

// 註冊"序號管制"服務
builder.Services.AddTransient<SequenceService>();

// 註冊"購物金紀錄"服務
// builder.Services.AddTransient<CustomerCreditService>();

// 註冊"系統日誌-DB"服務
builder.Services.AddTransient<ILogService, LogService>();

// 註冊 PermissionService/DepartmentService 權限服務
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddSingleton<DepartmentService>();

// 註冊 Token 服務
builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero, // 不允許時間偏差
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
    };

    // 添加事件處理器
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // 跳過默認的處理
            context.HandleResponse();

            // 檢查是否是 token 過期
            if (context.AuthenticateFailure is SecurityTokenExpiredException)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Token has expired" });
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid token" });
            }
        }
    };
});

// 註冊 通知 服務
builder.Services.AddSingleton<NotificationService>();

// 註冊 Line 服務
builder.Services.AddSingleton<LineService>();

// 註冊 T8Esb 服務
// builder.Services.AddT8EsbClient(builder.Configuration);

// 註冊 第三方平台設定 服務
builder.Services.AddSingleton<IThirdPartyConfigService, ThirdPartyConfigService>();

// 註冊 ECPay 發票服務
// builder.Services.AddEcpayInvoiceService(builder.Configuration);

// 註冊物流服務
// builder.Services.AddLogisticsServices(builder.Configuration);

// 註冊 TaskService 任務服務 與其相關服務 (所有 ITaskProcessor 都要在此註冊)
builder.Services.AddSingleton<TaskService>();       // 讓 TaskController 能解析
builder.Services.AddHostedService<TaskService>();   // 背景執行任務
builder.Services.AddSingleton<ITaskProcessor, DataCleanupTaskProcessor>();      // TaskType => "DataCleanup"
builder.Services.AddSingleton<ITaskProcessor, DatabaseBackupTaskProcessor>();   // TaskType => "DatabaseBackup";
builder.Services.AddSingleton<ITaskProcessor, ReportTaskProcessor>();   // TaskType => "Report";

// 註冊 Quartz 排程服務
builder.Services.AddQuartz(options =>
{
    options.UseJobFactory<MicrosoftDependencyInjectionJobFactory>(); // Quartz.DependencyInjection

    // 要依序註冊不同的 JobKey、TriggerKey，否則會被後面的覆蓋
    var jobKey1 = new JobKey(nameof(DataCleanupJob));
    options.AddJob<DataCleanupJob>(jobKey1).AddTrigger(trigger => trigger.ForJob(jobKey1).WithIdentity("trigger1").WithCronSchedule(DataCleanupJob.CronSchedule));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);   // 註冊 Quartz 讓它成為 .NET BackgroundService

builder.Services.AddMemoryCache();  // 改為無狀態架構，不使用 Session，改使用記憶體快取

// Swagger 服務
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc(builder.Configuration["WebAPI:SwaggerVersion"], new OpenApiInfo { Title = builder.Configuration["WebAPI:SwaggerTitle"], Version = builder.Configuration["WebAPI:SwaggerVersion"] });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    // 使用 DocumentFilter 來包含 xxxx.Models 下的所有模型(所有模型一律在後端開立，前端自動產生，以便控管)
    option.DocumentFilter<AdditionalSchemasDocumentFilter>(Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && type.Namespace != null && type.Namespace.Contains(".Models")).ToList());
});

// 配置 AutoMapper，自動掃描所有繼承自 Profile 的類別
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持原始名稱，讓資料庫->後端->前端，都不做命名原則的轉換(snake、camel、pascal)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeConverter()); // 處理 DateTime? 的 null 值和空字串轉換
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull; // 序列化時忽略 null 值
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;  // 允許非 ASCII 字元直接輸出，而不是進行編碼
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
if (Convert.ToBoolean(builder.Configuration["WebAPI:SwaggerOpen"]))
{
    app.UseMigrationsEndPoint();
    if (Convert.ToBoolean(builder.Configuration["WebAPI:SwaggerAuthAllowAnonymous"])) app.MapControllers().AllowAnonymous();    //kenghua: 測試時不驗證

    // 可以考慮開發環境才給 Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHsts();
app.UseDefaultFiles();
app.UseStaticFiles();   // 靜態資源的讀取路徑，此為預設，爲發佈後的wwwroot資料夾

app.UseRouting();
//app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();

