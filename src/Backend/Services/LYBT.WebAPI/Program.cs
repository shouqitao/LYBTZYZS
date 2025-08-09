/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// </summary>
using LYBT.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

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