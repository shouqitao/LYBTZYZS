using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LYBT.LocalWebAPI.Data;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Logging.Management;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth;
using LYBT.Module.Users;
using LYBT.Module.Patients;
using LYBT.Module.Herbs;
using LYBT.Module.Formulas;
using LYBT.Module.MedicalCases;
using LYBT.Module.Registration;
using LYBT.Module.Sync;
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
        // DbContext — 使用 AppDbContext（与远程 WebAPI 一致，含审计自动化）
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddControllers();

        builder.Services.AddSingleton<LoggingLevelManager>();

        // 注册模块 Service（与远程 WebAPI 使用相同的 Service/Repository 层）
        builder.Services.AddAuthModule(builder.Configuration);
        builder.Services.AddUsersModule(builder.Configuration);
        builder.Services.AddPatientsModule(builder.Configuration);
        builder.Services.AddHerbsModule(builder.Configuration);
        builder.Services.AddFormulaModule();
        builder.Services.AddMedicalCaseModule();
        builder.Services.AddRegistrationModule();
        builder.Services.AddSyncModule(builder.Configuration);

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
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
