using System.Windows;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Infrastructure.Converters;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Repositories;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Desktop.Users.ViewModels.Handlers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.IntegrationTests.EndToEnd.Fixtures;

/// <summary>
/// Desktop E2E 集成测试 Fixture
/// 构建最小化 DI 容器: ViewModel + Repository + DataSource + LocalDbContext(SQLite InMemory)
/// Mock: Prism 基础设施、导航、对话框、认证
/// </summary>
public class DesktopE2ETestFixture : IDisposable
{
    private readonly List<SqliteConnection> _connections = new();
    private bool _disposed;
    private static bool _wpfInitialized;
    private static readonly object _wpfLock = new();

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// 创建并返回配置完整的 ServiceProvider
    /// </summary>
    public IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // 1. 基础设施: Logging
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // 2. SQLite InMemory 数据库
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        services.AddSingleton(_ =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseSqlite(connection);
            optionsBuilder.EnableSensitiveDataLogging();
            return optionsBuilder.Options;
        });

        // 3. Mock ICurrentUserProvider (审计字段)
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        var testUserId = Guid.NewGuid();
        currentUserProvider.CurrentUserId.Returns(testUserId);
        services.AddSingleton(currentUserProvider);

        // 4. LocalDbContext (Scoped - 每个 scope 创建新实例)
        services.AddScoped(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<LocalDbContext>>();
            var userProvider = sp.GetRequiredService<ICurrentUserProvider>();
            var ctx = new LocalDbContext(options, userProvider);
            ctx.Database.EnsureCreated();
            return ctx;
        });

        // 5. 真实 Local DataSource (全部模块)
        services.AddScoped<IPatientDataSource, LocalPatientDataSource>();
        services.AddScoped<IHerbDataSource, LocalHerbDataSource>();
        services.AddScoped<IFormulaDataSource, LocalFormulaDataSource>();
        services.AddScoped<IMedicalCaseDataSource, LocalMedicalCaseDataSource>();
        services.AddScoped<IUserDataSource, LocalUserDataSource>();

        // 6. 真实 Repository (API 参数可选，Local 模式不需要)
        services.AddSingleton<IPatientRepository, PatientRepository>();
        services.AddSingleton<IHerbRepository, HerbRepository>();
        services.AddSingleton<IFormulaRepository, FormulaRepository>();
        services.AddSingleton<IMedicalCaseRepository, MedicalCaseRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();

        // 7. Mock Prism 基础设施
        services.AddSingleton(Substitute.For<IRegionManager>());
        services.AddSingleton(Substitute.For<IEventAggregator>());
        services.AddSingleton(Substitute.For<IDialogService>());

        // 8. Mock ViewModel 聚合服务的依赖项
        services.AddSingleton(Substitute.For<ISessionManager>());
        services.AddSingleton(Substitute.For<IUserNotificationService>());
        services.AddSingleton(Substitute.For<ICommonDialogService>());
        services.AddSingleton(Substitute.For<IRoleRegistry>());

        // 9. 真实 ViewModelServices (聚合)
        services.AddSingleton<IViewModelServices, ViewModelServices>();

        // 10. Mock NavigationCoordinator (依赖 Shell 层)
        services.AddSingleton(Substitute.For<INavigationCoordinator>());

        // 11. ViewModel 基础设施服务
        services.AddSingleton<IAsyncExecutor, AsyncExecutor>();

        // Mock IDialogManager: 真实 DialogManager 依赖 Prism IDialogService.ShowDialog()
        // NSubstitute mock 的 ShowDialog() 不触发回调 → TaskCompletionSource 永不完成 → 测试死锁
        // 解决: Mock IDialogManager，确认对话框默认返回 true
        var mockDialogManager = Substitute.For<IDialogManager>();
        mockDialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        mockDialogManager.ShowSuccessAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        mockDialogManager.ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        mockDialogManager.ShowWarningAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        mockDialogManager.ShowInfoAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        services.AddSingleton(mockDialogManager);
        services.AddTransient<ILoadingStateManager, LoadingStateManager>();
        services.AddTransient<IPaginationService, PaginationService>();
        services.AddTransient<ISearchService, SearchService>();
        services.AddTransient<IErrorHandler, ErrorHandler>();

        // 12. 泛型服务
        services.AddTransient(typeof(ISelectionService<>), typeof(SelectionService<>));
        services.AddTransient(typeof(IDetailEditorService<>), typeof(DetailEditorService<>));
        services.AddTransient(typeof(IListViewServices<>), typeof(ListViewServices<>));
        services.AddTransient(typeof(IMasterDetailServices<,>), typeof(MasterDetailServices<,>));

        // 13. 模块特有的 Service
        // Patient
        services.AddScoped<PatientService>();
        services.AddSingleton(Substitute.For<ICardReaderService>());
        services.AddSingleton(Substitute.For<IPatientCardReaderIntegration>());

        // Formula
        services.AddScoped<IFormulaService, FormulaService>();

        // User
        services.AddScoped<UserService>();
        services.AddScoped<IUserPasswordHandler, UserPasswordHandler>();
        services.AddScoped<IUserStatusHandler, UserStatusHandler>();
        services.AddScoped<IUserImportExportHandler, UserImportExportHandler>();

        // 14. ViewModel
        services.AddTransient<PatientMasterDetailViewModel>();
        services.AddTransient<HerbMasterDetailViewModel>();
        services.AddTransient<FormulaMasterDetailViewModel>();
        services.AddTransient<MedicalCaseMasterDetailViewModel>();
        services.AddTransient<UserMasterDetailViewModel>();

        ServiceProvider = services.BuildServiceProvider();
        return ServiceProvider;
    }

    /// <summary>
    /// 获取 LocalDbContext 用于直接数据验证
    /// </summary>
    public LocalDbContext GetDbContext()
    {
        return ServiceProvider.GetRequiredService<LocalDbContext>();
    }

    /// <summary>
    /// 预置种子数据
    /// </summary>
    public async Task SeedDataAsync(Func<LocalDbContext, Task> seedAction)
    {
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        await seedAction(db);
    }

    /// <summary>
    /// 初始化 WPF Application 资源 (STA 线程安全)
    /// </summary>
    public static void InitializeWpf()
    {
        lock (_wpfLock)
        {
            if (_wpfInitialized) return;

            if (Application.Current == null)
            {
                _ = new Application();
            }

            var app = Application.Current;
            if (app != null)
            {
                app.Resources = new ResourceDictionary
                {
                    ["BaseDataGridStyle"] = new Style(typeof(System.Windows.Controls.DataGrid)),
                    ["ToolBarContainer"] = new Style(typeof(System.Windows.Controls.Border)),
                    ["SearchTextBox"] = new Style(typeof(System.Windows.Controls.TextBox)),
                    ["SecondaryButton"] = new Style(typeof(System.Windows.Controls.Button)),
                    ["FilterComboBox"] = new Style(typeof(System.Windows.Controls.ComboBox)),
                    ["PaginationControlButton"] = new Style(typeof(System.Windows.Controls.Button)),
                    ["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                    ["PrimaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue),
                    ["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                    ["NeutralBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                    ["NeutralLightBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                    ["EmptyStateBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray),
                    ["EmptyStateForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                    ["FontSizeDisplay"] = 14.0,
                    ["FontSizeBody"] = 13.0,
                    ["FontSizeLabel"] = 12.0,
                    ["FontSizeTitle"] = 16.0,
                    ["FontSizeSmall"] = 11.0,
                    ["SpacingSmall"] = new System.Windows.Thickness(4),
                    ["SpacingMedium"] = new System.Windows.Thickness(8),
                    ["SpacingLarge"] = new System.Windows.Thickness(16),
                    ["CornerRadius"] = new System.Windows.CornerRadius(4),
                    ["StandardPadding"] = new System.Windows.Thickness(8),
                    ["StandardMargin"] = new System.Windows.Thickness(4),
                    ["InverseNullToVisibilityConverter"] = new InverseNullToVisibilityConverter(),
                    ["NullToVisibilityConverter"] = new NullToVisibilityConverter(),
                    ["PaginationCurrentPage"] = new Style(typeof(System.Windows.Controls.Border)),
                    ["PaginationPageNumber"] = new Style(typeof(System.Windows.Controls.TextBlock)),
                };
            }

            _wpfInitialized = true;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        foreach (var conn in _connections)
        {
            try { conn.Close(); conn.Dispose(); }
            catch { /* ignore */ }
        }
        _connections.Clear();
    }
}
