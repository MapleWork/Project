using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalFinanceApi.Data;
using PersonalFinanceApi.Services;
using PersonalFinanceApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 添加基本服務
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 配置 Swagger 文件生成，包含 JWT 認證支援
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Personal Finance API",
        Version = "v1",
        Description = "個人記帳系統 API",
        Contact = new OpenApiContact
        {
            Name = "Personal Finance Team",
            Email = "support@personalfinance.com",
            Url = new Uri("https://github.com/MapleWork/Project/tree/main/personal-finance-api")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // 添加 JWT 認證配置到 Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // 包含 XML 註釋 (需要再專案檔中啟用 XML 文件生成)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 配置資料庫連線
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));

    // 在開發環境中啟用敏感資料紀錄
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// 配置 JWT 認證
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // 開發環境可設為 false
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // 取消預設的 5 分鐘時間偏差容忍
    };

    // JWT 認證失敗時的事件處理
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

// 配置 CORS 政策
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:3001",
            "https://localhost:3000",
            "https://localhost:3001"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });

    // 開發環境的寬鬆政策
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("Development", policy =>
        {
            policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
    }
});

// 註冊應用程式服務
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// 配置日誌記錄
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

// 建立應用程式
var app = builder.Build();

// 配置 HTTP 請求處理管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Personal Finance API v1");
        c.RoutePrefix = string.Empty; // 將 Swagger UI 設為跟路徑
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
    });
}

// 中介軟體順序很重要
app.UseHttpsRedirection();

// 使用適當的 CORS 政策
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("AllowReactApp");
}

// 認證和授權中介軟體
app.UseAuthentication();
app.UseAuthorization();

// 全域例外處理中介軟體
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        // 設定回應狀態碼和內容類型
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var ex = error.Error;
            
            // 取得 Logger
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            
            // 記錄詳細錯誤資訊
            logger.LogError(ex, "Unhandled exception occurred. RequestId: {RequestId}, Path: {Path}", 
                context.TraceIdentifier, context.Request.Path);

            // 根據例外類型設定適當的狀態碼
            var statusCode = ex switch
            {
                ArgumentException => 400,
                UnauthorizedAccessException => 401,
                NotImplementedException => 501,
                TimeoutException => 408,
                _ => 500
            };

            context.Response.StatusCode = statusCode;

            // 建立錯誤回應
            var response = new ErrorResponse
            {
                Message = app.Environment.IsDevelopment() 
                    ? ex.Message 
                    : GetUserFriendlyMessage(ex),
                StackTrace = app.Environment.IsDevelopment() ? ex.StackTrace : null,
                ExceptionType = app.Environment.IsDevelopment() ? ex.GetType().Name : null,
                RequestId = context.TraceIdentifier,
                Path = context.Request.Path,
                StatusCode = statusCode
            };

            // 序列化回應
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = app.Environment.IsDevelopment(),
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }
    });
});

// 取得使用者友善的錯誤訊息
static string GetUserFriendlyMessage(Exception ex)
{
    return ex switch
    {
        ArgumentException => "提供的資料格式不正確",
        UnauthorizedAccessException => "您沒有權限執行此操作",
        TimeoutException => "請求處理超時，請稍後再試",
        NotImplementedException => "此功能尚未實作",
        _ => "系統發生錯誤，請稍後再試"
    };
}

// 映射控制器路由
app.MapControllers();

// 確保資料庫已建立
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        // 如果需要自動遷移資料庫 (僅建議在開發環境使用)
        if (app.Environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
        }

        // 檢查資料庫連線
        await context.Database.CanConnectAsync();
        app.Logger.LogInformation("資料庫連線成功");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "資料庫連線失敗");
    }
}

// 啟動應用程式
app.Logger.LogInformation("Personal Finance API 正在啟動...");
app.Logger.LogInformation($"環境: {app.Environment.EnvironmentName}");
app.Logger.LogInformation($"Swagger UI 可以在以下位置訪問: {(app.Environment.IsDevelopment() ? "https://localhost:5001" : "")}");

app.Run();