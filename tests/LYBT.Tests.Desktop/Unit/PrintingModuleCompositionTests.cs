using DryIoc;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.Printing;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Desktop.Printing.Services;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Shared.Models.Enums;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// 打印模块组合守卫（F-06 Item A）。
/// </summary>
/// <remarks>
/// <para><b>缺陷</b>：<see cref="PrintingModule"/> 在模块目录中注册为 <see cref="InitializationMode.OnDemand"/>，
/// 但它既不在任何 <c>RoleDefinition.RequiredModules</c> 中，也不在 <see cref="ModuleLazyLoader.ViewToModuleMap"/>
/// 中——即三条加载路径（角色预加载 / 启动装载 / 导航懒加载）都不会拉起它。
/// 于是 <see cref="IPrintService{TModel}"/> 从未注册，<see cref="PrescriptionPrintHandler"/>
/// 经可选参数退化为 null，MedicalCaseWorkspaceView 的「打印处方单」/「导出 PDF」按钮
/// 恒返回「打印服务未配置」。</para>
/// <para><b>本测试走真实组合路径</b>：真实 DryIoc 容器 + 真实 Shell 服务注册（<c>RegisterAllServices</c>）
/// + 真实 Prism 模块目录/模块管理器（模块类型的 <c>RegisterTypes</c> 真实执行）
/// + 真实角色定义（<c>RoleRegistry</c>）→ 真实 <see cref="ModuleLazyLoader"/> 加载角色模块，
/// 最后断言打印服务可解析、且容器解析出的打印处理器不再降级为「打印服务未配置」。</para>
/// </remarks>
public class PrintingModuleCompositionTests
{
    /// <summary>
    /// 角色登录后（<see cref="ModuleLazyLoader.PreloadModulesAsync"/>，由 ShellEventCoordinator 触发）
    /// 打印服务必须已注册——四个可进入 MedicalCaseWorkspace 的角色（ViewRoleAccess）全部覆盖。
    /// </summary>
    [Theory]
    [InlineData(UserRole.Doctor)]
    [InlineData(UserRole.Receptionist)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task PrintService_IsResolvable_AfterRoleModulesLoaded(UserRole role)
    {
        using var composition = Composition.Create();

        await composition.Loader.PreloadModulesAsync(role);

        AssertPrintPipelineComposed(composition, $"角色 {role} 预加载模块后");
    }

    /// <summary>
    /// 导航懒加载路径（<see cref="ModuleLazyLoader.EnsureModuleLoadedAsync"/>，
    /// 视图 → ClinicalModule）同样必须把打印服务带进容器——首次直接进入医案工作台（未走预加载）的场景。
    /// </summary>
    [Fact]
    public async Task PrintService_IsResolvable_AfterNavigatingToMedicalCaseWorkspace()
    {
        using var composition = Composition.Create();

        await composition.Loader.EnsureModuleLoadedAsync(ViewNames.MedicalCaseWorkspace);

        AssertPrintPipelineComposed(composition, "导航懒加载 ClinicalModule 后");
    }

    private static void AssertPrintPipelineComposed(Composition composition, string because)
    {
        composition.IsPrintServiceRegistered.Should().BeTrue(
            $"{because}，{nameof(IPrintService<PrescriptionPrintModel>)} 必须可从容器解析；"
            + $"否则打印处方单/导出 PDF 恒提示「打印服务未配置」（{nameof(PrintingModule)} 必须随打印入口宿主模块一起加载）");

        composition.Resolve<IPrintService<PrescriptionPrintModel>>()
            .Should().BeOfType<PrescriptionPrintService>(
                $"{because}，处方打印服务应由 {nameof(PrintingModule)} 注册的实现提供");
    }

    /// <summary>
    /// 组合后的打印处理器不得再降级：容器解析 <see cref="PrescriptionPrintHandler"/> 时
    /// 必须注入真实打印服务（未完成医案在空 <c>Current</c> 下返回「仅已完成医案可打印」，
    /// 而非「打印服务未配置」）。此处不触碰真实打印/预览窗口，避免 WPF 打印管线在测试宿主中执行。
    /// </summary>
    [Fact]
    public async Task PrintPreview_DoesNotDegrade_WhenHandlerResolvedFromComposedContainer()
    {
        using var composition = Composition.Create();
        await composition.Loader.PreloadModulesAsync(UserRole.Doctor);

        var handler = composition.Resolve<PrescriptionPrintHandler>();
        var result = await handler.PrintPreviewAsync(Guid.NewGuid(), null, null, null);

        result.IsSuccess.Should().BeFalse("未完成医案不可打印");
        result.ErrorMessage.Should().NotBe("打印服务未配置");
    }

    /// <summary>
    /// 真实组合装配：Shell DI（真实服务图）+ Prism 模块目录/模块管理器 + 真实模块加载器。
    /// </summary>
    private sealed class Composition : IDisposable
    {
        private readonly DryIocContainerExtension _containerExtension;

        private Composition(DryIocContainerExtension containerExtension, IModuleLazyLoader loader)
        {
            _containerExtension = containerExtension;
            Loader = loader;
        }

        public IModuleLazyLoader Loader { get; }

        public bool IsPrintServiceRegistered => _containerExtension.IsRegistered(typeof(IPrintService<PrescriptionPrintModel>));

        public T Resolve<T>() where T : notnull => _containerExtension.Resolve<T>();

        public static Composition Create()
        {
            var containerExtension = new DryIocContainerExtension();

            // Prism 容器自身的约定注册（与 PrismApplicationBase.RegisterRequiredTypes 对齐）
            containerExtension.RegisterInstance<IContainerExtension>(containerExtension);
            containerExtension.RegisterInstance<IContainerProvider>(containerExtension);
            containerExtension.RegisterSingleton<IModuleInitializer, ModuleInitializer>();
            containerExtension.RegisterSingleton<IModuleManager, ModuleManager>();
            containerExtension.RegisterInstance<IEventAggregator>(CreateEventAggregator());
            containerExtension.RegisterSingleton<IDialogService, DialogService>();

            // Shell 真实服务图（含 IRoleRegistry 四个真实角色定义、IModuleLoadingService、IModuleLazyLoader）
            containerExtension.RegisterAllServices();

            var catalog = BuildModuleCatalog();
            containerExtension.RegisterInstance<IModuleCatalog>(catalog);

            var loader = containerExtension.Resolve<IModuleLazyLoader>();
            return new Composition(containerExtension, loader);
        }

        /// <summary>
        /// Prism 的 <see cref="EventAggregator"/> 在构造时捕获 <see cref="SynchronizationContext"/>
        /// （<c>ThreadOption.UIThread</c> 订阅的前置条件；WPF 宿主在 UI 线程上构造）。
        /// 测试宿主线程默认没有 SynchronizationContext，会因 <c>SessionLifecycleManager</c> 的 UIThread 订阅
        /// 抛 <c>EventAggregatorNotConstructedOnUIThread</c> —— 与打印链路无关，故在此显式提供。
        /// </summary>
        private static IEventAggregator CreateEventAggregator()
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(previous ?? new SynchronizationContext());
            try
            {
                return new EventAggregator();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        /// <summary>
        /// 真实模块目录：调用生产 SSOT <see cref="DesktopModuleCatalog.Configure"/>（App.ConfigureModuleCatalog 同源），
        /// 因此本测试校验的是应用真实模块清单与依赖声明，而不是测试侧副本。
        /// </summary>
        private static IModuleCatalog BuildModuleCatalog()
        {
            var catalog = new ModuleCatalog();

            // 不调用 ModuleManager.Run()：WhenAvailable 模块（Authentication/Admin/Sysadmin）
            // 仅在成为角色模块依赖时按需加载，与断言目标无关。
            DesktopModuleCatalog.Configure(catalog);

            catalog.Initialize();
            return catalog;
        }

        public void Dispose() => ((IContainerExtension<IContainer>)_containerExtension).Instance.Dispose();
    }
}
