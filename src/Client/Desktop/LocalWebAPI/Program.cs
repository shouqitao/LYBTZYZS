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

        // 独立宿主专用配置（appsettings.localwebapi.json）。
        // 刻意不命名为 appsettings.json：桌面进程内嵌 LocalWebAPI 与 Shell 共用同一发布目录，
        // 两份 appsettings.json 会导致发布期 NETSDK1152（同相对路径冲突）并在构建期不確定地互相覆盖
        // ——内嵌场景应使用 Shell 的 appsettings.json（含完整 Jwt 节）。本文件仅供本独立宿主（调试）使用。
        builder.Configuration.AddJsonFile("appsettings.localwebapi.json", optional: true, reloadOnChange: false);

        builder.WebHost.UseUrls("http://127.0.0.1:5290");

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=LYBTDB_Local;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true";

        var app = LocalWebApiProgram.CreateApplication(builder, connectionString);
        await LocalWebApiProgram.InitializeDatabaseAsync(app);
        await app.RunAsync();
    }
}