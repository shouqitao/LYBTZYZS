using System.Windows;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Formula.ViewModels.Handlers;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Herbs.ViewModels.Handlers;
using LYBT.Desktop.Infrastructure.Converters;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Desktop.LocalData.Initialization;
using LYBT.Desktop.LocalData.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Patients.ViewModels.Handlers;
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

namespace LYBT.Tests.Desktop.Infrastructure;

/// <summary>
/// Desktop Testing Trophy Fixture.
/// SQLite InMemory + 真实 Repository/DataSource/Service + 最小 WPF 边界 Mock.
///
/// Mock 白名单 (扩展自设计文档 4 接口，附理由):
///
/// [WPF Shell 边界] 无法在无 Shell 环境中使用真实实现:
///   - IRegionManager: Prism 区域导航，无 Shell 窗口
///   - IDialogService: Prism 对话框，需要运行中 Shell
///   - IDialogManager: TaskCompletionSource 死锁防护
///   - INavigationCoordinator: Shell 级导航
///   - ISessionManager: 依赖登录状态机
///   - IUserNotificationService: UI 通知
///   - ICommonDialogService: UI 对话框
///   - IRoleRegistry: 角色过滤
///
/// [硬件] 测试环境无硬件:
///   - ICardReaderService, IPatientCardReaderIntegration
///
/// [远程服务] Desktop 测试不连接服务端:
///   - IDesktopCacheManager, IHerbSearchProvider
///
/// [文件 I/O] 避免文件系统副作用:
///   - IPatientImportExportHandler, IHerbImportExportHandler
///
/// 真实替换 (从旧 Fixture 改进):
///   - IEventAggregator → 真实 Prism EventAggregator (已在 AuthenticationStateMachineTests 中验证)
///   - ICurrentUserProvider → NSubstitute stub (固定测试 UserId)
/// </summary>
public class DesktopFixture : IDisposable
{
    private readonly List<SqliteConnection> _connections = new();
    private bool _disposed;
    private static bool _wpfInitialized;
    private static readonly object _wpfLock = new();

    /// <summary>默认测试用户ID</summary>
    public static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// 创建配置完整的 ServiceProvider.
    /// 包含: ViewModel + Repository + DataSource + LocalDbContext(SQLite InMemory).
    /// </summary>
    public IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // 1. 基础设施: Logging
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // 2. SQLite InMemory 数据库 (每次调用独立连接)
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

        // 3. ICurrentUserProvider (审计字段 stub)
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.CurrentUserId.Returns(TestUserId);
        services.AddSingleton(currentUserProvider);

        // 4. LocalDbContext (Scoped)
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

        // 6. 真实 Repository (Scoped: 与 DataSource 同生命周期)
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IHerbRepository, HerbRepository>();
        services.AddScoped<IFormulaRepository, FormulaRepository>();
        services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // 7. 真实 Prism EventAggregator (替代旧 Fixture 中的 mock)
        services.AddSingleton<IEventAggregator, EventAggregator>();

        // 8. WPF Shell 边界 Mock
        services.AddSingleton(Substitute.For<IRegionManager>());
        services.AddSingleton(Substitute.For<IDialogService>());
        services.AddSingleton(Substitute.For<ISessionManager>());
        services.AddSingleton(Substitute.For<IUserNotificationService>());
        services.AddSingleton(Substitute.For<ICommonDialogService>());
        services.AddSingleton(Substitute.For<IRoleRegistry>());
        services.AddSingleton(Substitute.For<INavigationCoordinator>());

        // 9. 真实 ViewModelServices (聚合)
        services.AddSingleton<IViewModelServices, ViewModelServices>();

        // 10. IDialogManager: Mock 防死锁
        // 真实 DialogManager.ShowDialog() → Prism IDialogService.ShowDialog() (mock 不触发回调)
        // → TaskCompletionSource 永不完成 → 测试死锁
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

        // 11. ViewModel 基础设施服务 (真实)
        services.AddSingleton<IAsyncExecutor, AsyncExecutor>();
        services.AddTransient<ILoadingStateManager, LoadingStateManager>();
        services.AddTransient<IPaginationService, PaginationService>();
        services.AddTransient<ISearchService, SearchService>();
        services.AddTransient<IErrorHandler, ErrorHandler>();

        // 12. 泛型服务 (真实)
        services.AddTransient(typeof(ISelectionService<>), typeof(SelectionService<>));
        services.AddTransient(typeof(IDetailEditorService<>), typeof(DetailEditorService<>));
        services.AddTransient(typeof(IListViewServices<>), typeof(ListViewServices<>));
        services.AddTransient(typeof(IMasterDetailServices<,>), typeof(MasterDetailServices<,>));

        // 13. 模块 Service (真实)
        services.AddScoped<PatientService>();
        services.AddScoped<IFormulaService, FormulaService>();
        services.AddScoped<UserService>();
        services.AddScoped<IUserPasswordHandler, UserPasswordHandler>();
        services.AddScoped<IUserStatusHandler, UserStatusHandler>();
        services.AddScoped<IUserImportExportHandler, UserImportExportHandler>();
        services.AddScoped<IPatientStatusHandler, PatientStatusHandler>();
        services.AddScoped<IHerbStatusHandler, HerbStatusHandler>();
        services.AddScoped<IFormulaStatusHandler, FormulaStatusHandler>();

        // 14. 本地认证
        services.AddScoped<ILocalAuthService, LocalAuthService>();
        services.AddScoped<DatabaseInitializer>();

        // 15. 硬件/远程/I/O Mock
        services.AddSingleton(Substitute.For<ICardReaderService>());
        services.AddSingleton(Substitute.For<IPatientCardReaderIntegration>());
        services.AddSingleton(Substitute.For<IDesktopCacheManager>());
        services.AddSingleton(Substitute.For<IHerbSearchProvider>());
        services.AddSingleton(Substitute.For<IPatientImportExportHandler>());
        services.AddSingleton(Substitute.For<IHerbImportExportHandler>());

        // 16. ViewModel (真实)
        services.AddTransient<PatientMasterDetailViewModel>();
        services.AddTransient<HerbMasterDetailViewModel>();
        services.AddTransient<FormulaMasterDetailViewModel>();
        services.AddTransient<MedicalCaseMasterDetailViewModel>();
        services.AddTransient<UserMasterDetailViewModel>();

        ServiceProvider = services.BuildServiceProvider();
        return ServiceProvider;
    }

    /// <summary>
    /// 从 DI 容器解析服务
    /// </summary>
    public T Resolve<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

    /// <summary>
    /// 创建新的 DI Scope (用于隔离 Scoped 服务)
    /// </summary>
    public IServiceScope CreateScope() => ServiceProvider.CreateScope();

    /// <summary>
    /// 获取 LocalDbContext 用于直接数据验证
    /// </summary>
    public LocalDbContext GetDbContext() => ServiceProvider.GetRequiredService<LocalDbContext>();

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
    /// 初始化 WPF Application 资源 (STA 线程安全, 幂等)
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
                var whiteBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                var grayBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                var blueBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

                app.Resources = new ResourceDictionary
                {
                    ["BaseDataGridStyle"] = new Style(typeof(System.Windows.Controls.DataGrid)),
                    ["BaseDataGridCell"] = new Style(typeof(System.Windows.Controls.DataGridCell)),
                    ["BaseDataGridRow"] = new Style(typeof(System.Windows.Controls.DataGridRow)),
                    ["BaseDataGridColumnHeader"] = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)),
                    ["ToolBarContainer"] = new Style(typeof(System.Windows.Controls.Border)),
                    ["SearchTextBox"] = new Style(typeof(System.Windows.Controls.TextBox)),
                    ["SecondaryButton"] = new Style(typeof(System.Windows.Controls.Button)),
                    ["FilterComboBox"] = new Style(typeof(System.Windows.Controls.ComboBox)),
                    ["PaginationControlButton"] = new Style(typeof(System.Windows.Controls.Button)),
                    ["BackgroundBrush"] = whiteBrush,
                    ["PrimaryBrush"] = blueBrush,
                    ["BorderBrush"] = grayBrush,
                    ["NeutralBrush"] = grayBrush,
                    ["NeutralLightBrush"] = grayBrush,
                    ["RegionBrush"] = whiteBrush,
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
                    ["PageSizeOptions"] = new int[] { 10, 20, 50, 100 },
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
            catch { /* cleanup best-effort */ }
        }
        _connections.Clear();

        GC.SuppressFinalize(this);
    }
}
