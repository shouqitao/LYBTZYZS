using LYBT.Infrastructure.Authentication;
using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Options;
using LYBT.Module.Users;
using LYBT.WebAPI.Services;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 统一服务注册管理 - UltraThink统一注入管理系统
/// 将所有服务注册逻辑统一到一个地方，提高可维护性和可读性
/// </summary>
public static class UnifiedServiceRegistration
{
    /// <summary>
    /// 注册所有应用服务（统一入口）
    /// </summary>
    public static IServiceCollection RegisterAllApplicationServices(
        this IServiceCollection services, 
        IConfiguration configuration, 
        IWebHostEnvironment environment)
    {
        // 1. 基础设施服务
        services.RegisterInfrastructureServices(configuration);
        
        // 2. 认证和安全服务
        services.RegisterAuthenticationServices(configuration);
        
        // 3. 业务模块服务
        services.RegisterBusinessModules();
        
        // 4. API和文档服务
        services.RegisterApiServices();
        
        // 5. 控制器和JSON配置
        services.RegisterControllerServices();
        
        // 6. 跨域策略
        services.AddSecureCorsPolicy(configuration, environment);
        
        return services;
    }

    /// <summary>
    /// 注册基础设施服务
    /// </summary>
    private static IServiceCollection RegisterInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 统一数据库上下文
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                });
                options.EnableSensitiveDataLogging(false);
                options.EnableServiceProviderCaching();
            });
        }

        // 缓存服务
        services.AddMemoryCache();
        services.Configure<CacheOptions>(configuration.GetSection("CacheOptions"));
        services.AddSingleton<ICacheService, CacheService>();

        // 安全配置服务
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.AddScoped<IPasswordValidationService, PasswordValidationService>();
        services.AddScoped<ISecurityConfigurationValidator, SecurityConfigurationValidator>();

        // 监控和健康检查服务
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddSingleton<ISystemMetricsCollector, SystemMetricsCollector>();

        // 统一服务
        services.AddScoped<IUnifiedLogService, UnifiedLogService>();
        
        // 数据库初始化服务
        services.AddScoped<LYBT.Infrastructure.Database.DatabaseInitializationService>();

        return services;
    }

    /// <summary>
    /// 注册认证和安全服务
    /// </summary>
    private static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // JWT认证配置
        var jwtSection = configuration.GetSection("JwtOptions");
        services.Configure<JwtOptions>(jwtSection);
        var jwtOptions = jwtSection.Get<JwtOptions>();
        
        if (jwtOptions != null)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
                };
            });
        }

        // 认证相关服务
        services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        
        // 配置选项
        services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));

        return services;
    }

    /// <summary>
    /// 注册业务模块服务
    /// </summary>
    private static IServiceCollection RegisterBusinessModules(this IServiceCollection services)
    {
        // 注册Users模块服务
        services.AddUsersModuleServices();
        
        // 注册所有LYBT业务模块服务
        services.AddAllModules();
        
        // 注册认证模块服务
        services.AddScoped<LYBT.Module.Auth.Interfaces.IAuthRepository, LYBT.Module.Auth.Repositories.AuthRepository>();
        services.AddScoped<LYBT.Module.Auth.Services.SysAdminHandler>();
        services.AddScoped<LYBT.Module.Auth.Interfaces.IAuthService, LYBT.Module.Auth.Services.AuthService>();

        // 注册验方模块服务
        services.AddScoped<LYBT.Module.Formula.Interfaces.IFormulaService, LYBT.Module.Formula.Services.FormulaService>();

        return services;
    }

    /// <summary>
    /// 注册API和文档服务
    /// </summary>
    private static IServiceCollection RegisterApiServices(this IServiceCollection services)
    {
        // API版本控制
        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                new Asp.Versioning.UrlSegmentApiVersionReader(),
                new Asp.Versioning.QueryStringApiVersionReader("version"),
                new Asp.Versioning.HeaderApiVersionReader("X-Version"));
        }).AddApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        // ProblemDetails 和异常处理服务
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Swagger文档
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "凌隐宝堂中医诊所诊疗系统 API",
                Version = "v1",
                Description = "凌隐宝堂中医诊所诊疗系统API文档"
            });

            // 解决Schema ID冲突问题 - 生成真正唯一的Schema ID
            c.CustomSchemaIds(type =>
            {
                if (type.IsGenericType)
                {
                    var genericDef = type.GetGenericTypeDefinition();
                    var genericTypeName = genericDef.FullName?.Split('`')[0]?.Replace(".", "") ?? genericDef.Name.Split('`')[0];

                    var genericArgs = type.GetGenericArguments()
                        .Select(arg => GetTypeSignature(arg))
                        .ToArray();

                    return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
                }

                return type.FullName?.Replace(".", "").Replace("+", "") ?? type.Name;
            });
        });

        // AutoMapper配置
        services.AddAutoMapperConfiguration();

        return services;

        // 本地辅助方法：生成类型签名
        static string GetTypeSignature(Type type)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var genericTypeName = genericDef.Name.Split('`')[0];
                var genericArgs = type.GetGenericArguments()
                    .Select(arg => GetTypeSignature(arg))
                    .ToArray();
                return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
            }
            return type.Name.Replace("[]", "Array");
        }
    }

    /// <summary>
    /// 注册控制器和JSON服务
    /// </summary>
    private static IServiceCollection RegisterControllerServices(this IServiceCollection services)
    {
        // 确保UTF-8编码支持
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        });

        return services;
    }
}