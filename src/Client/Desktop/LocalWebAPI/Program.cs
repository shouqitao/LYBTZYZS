namespace LYBT.LocalWebAPI;

/// <summary>
/// LocalWebAPI 独立宿主入口（桌面进程内由 EmbeddedLocalWebApiService 直接调用
/// LocalWebApiProgram 启动，本 Main 仅用于独立运行/调试场景）。
/// 命名空间化避免与 LYBT.WebAPI 全局命名空间的 Program
/// （Issue #1077 为 WebApplicationFactory 兼容特意置于全局命名空间）产生类型冲突。
/// </summary>
internal static class LocalWebApiHost
{
    public static async Task Main(string[] args)
    {
        var builder = LocalWebApiProgram.CreateBuilder(args);

        builder.WebHost.UseUrls("http://127.0.0.1:5290");

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=LYBTDB_Local;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true";

        var app = LocalWebApiProgram.CreateApplication(builder, connectionString);
        await LocalWebApiProgram.InitializeDatabaseAsync(app);
        await app.RunAsync();
    }
}