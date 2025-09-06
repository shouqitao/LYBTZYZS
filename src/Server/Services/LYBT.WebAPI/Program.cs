/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// UltraThink v2.0 Security: 加载.env文件和环境变量替换支持
/// </summary>
using LYBT.WebAPI.Extensions;

// using LYBT.WebAPI.Services; // Removed - enterprise services beyond constraint scope

// =========== UltraThink安全配置增强 - 提前加载环境变量 ===========
// EnvironmentVariableLoader.LoadEnvironmentVariables(); // Removed - enterprise feature

var builder = WebApplication.CreateBuilder(args);

// =========== 额外的环境变量支持 ===========
// 添加环境变量配置源（确保环境变量优先级高于appsettings.json）
builder.Configuration.AddEnvironmentVariables();

// =========== UltraThink统一服务注册 ===========
builder.Services.RegisterAllApplicationServices(builder.Configuration, builder.Environment);

// =========== 构建应用 ===========
var app = builder.Build();

// =========== UltraThink统一初始化 ===========
await app.InitializeAllApplicationServices();

// =========== UltraThink统一中间件配置 ===========
app.ConfigureAllMiddleware();

// =========== 显示数据库状态 ===========
await app.DisplayDatabaseStatusAsync();

// =========== UltraThink优雅关闭配置 ===========
await app.ConfigureGracefulShutdown();
