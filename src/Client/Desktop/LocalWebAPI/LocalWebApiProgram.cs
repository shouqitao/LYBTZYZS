using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LYBT.LocalWebAPI.Data;
using LYBT.LocalWebAPI.Auth;
using Microsoft.EntityFrameworkCore;

namespace LYBT.LocalWebAPI;

public static class LocalWebApiProgram
{
    public static WebApplicationBuilder CreateBuilder(string[]? args = null)
    {
        var builder = WebApplication.CreateSlimBuilder(args ?? []);
        return builder;
    }

    public static WebApplication CreateApplication(WebApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContext<LocalWebApiDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddControllers();

        LocalJwtConfig.ConfigureServices(builder.Services);

        var app = builder.Build();

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

    public static async Task RunAsync(string[]? args, string connectionString)
    {
        var builder = CreateBuilder(args);
        var app = CreateApplication(builder, connectionString);
        await InitializeDatabaseAsync(app);
        await app.RunAsync();
    }
}
