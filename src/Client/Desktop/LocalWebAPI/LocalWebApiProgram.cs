using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LYBT.LocalWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LYBT.LocalWebAPI;

public static class LocalWebApiProgram
{
    public static IHostApplicationBuilder CreateBuilder(string[]? args = null)
    {
        var builder = WebApplication.CreateSlimBuilder(args ?? []);
        return builder;
    }

    public static WebApplication CreateApplication(IHostApplicationBuilder builder, string dbPath)
    {
        // Register SQLite DbContext
        builder.Services.AddDbContext<LocalWebApiDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Register controllers
        builder.Services.AddControllers();

        // Register auth
        LocalJwtConfig.ConfigureServices(builder.Services);

        var app = builder.Build();

        // Middleware pipeline
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LocalWebApiDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await LocalWebApiSeedData.SeedAsync(dbContext);
    }

    // Runs the Web API host lifecycle: build, initialize database, then run.
    public static async Task RunAsync(string[]? args, string dbPath)
    {
        var builder = CreateBuilder(args);
        var app = CreateApplication(builder, dbPath);
        await InitializeDatabaseAsync(app);
        await app.RunAsync();
    }
}
