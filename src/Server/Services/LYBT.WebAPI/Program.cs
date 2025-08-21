/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// UltraThink v2.0 Security: 加载.env文件和环境变量替换支持
/// </summary>
using LYBT.Infrastructure.Configuration;
using LYBT.WebAPI.Extensions;

// =========== UltraThink安全配置增强 - 提前加载环境变量 ===========
// 在应用构建器创建之前手动加载.env文件（在项目根目录）
var currentDir = Directory.GetCurrentDirectory();
Console.WriteLine($"📍 当前目录: {currentDir}");

// 从WebAPI项目目录向上查找项目根目录（包含.sln文件的目录）
var searchDir = new DirectoryInfo(currentDir);
string? projectRoot = null;

while (searchDir != null)
{
    if (searchDir.GetFiles("*.sln").Any())
    {
        projectRoot = searchDir.FullName;
        break;
    }
    searchDir = searchDir.Parent;
}

if (string.IsNullOrEmpty(projectRoot))
{
    projectRoot = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".."));
}

Console.WriteLine($"🏠 项目根目录: {projectRoot}");
var envPath = Path.Combine(projectRoot, ".env");
Console.WriteLine($"🔍 检查.env文件: {envPath}");
if (File.Exists(envPath))
{
    Console.WriteLine("✅ .env文件存在，开始加载环境变量");
    var envVars = File.ReadAllLines(envPath)
        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
        .Select(line => line.Split('=', 2))
        .Where(parts => parts.Length == 2)
        .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Trim('"'));
    
    Console.WriteLine($"📋 加载 {envVars.Count} 个环境变量");
    foreach (var kvp in envVars)
    {
        Environment.SetEnvironmentVariable(kvp.Key, kvp.Value, EnvironmentVariableTarget.Process);
        // 只显示关键变量的部分信息（保护敏感信息）
        if (new[] { "JWT_SECRET", "ADMIN_DEFAULT_PASSWORD", "USER_DEFAULT_PASSWORD" }.Contains(kvp.Key))
        {
            var maskedValue = kvp.Value.Length > 8 ? $"{kvp.Value.Substring(0, 4)}***{kvp.Value.Substring(kvp.Value.Length - 4)}" : "****";
            Console.WriteLine($"  🔑 {kvp.Key} = {maskedValue} (长度: {kvp.Value.Length})");
        }
    }
}
else
{
    Console.WriteLine("❌ .env文件不存在");
}

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