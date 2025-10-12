using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.{{ModuleName}};

/// <summary>
/// {{ModuleName}} 模块
/// </summary>
[Module(ModuleName = nameof({{ModuleName}}Module))]
// [ModuleDependency("DependencyModule")] // 如果有依赖模块，取消注释并填写
public class {{ModuleName}}Module : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑（可选）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
        // - Repository (数据访问层) 由各业务模块自行注册

        // 注册 Repository（单例）
        containerRegistry.RegisterSingleton<I{{Entity}}Repository, {{Entity}}Repository>();

        // 注册 ViewModel（瞬时）
        containerRegistry.Register<{{Entity}}ManagementViewModel>();
        containerRegistry.Register<{{Entity}}DetailViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.{{Entity}}ManagementView>();
        containerRegistry.RegisterForNavigation<Views.{{Entity}}DetailView>();

        // 注册对话框（可选）
        // containerRegistry.RegisterDialog<Views.{{Entity}}EditorDialog, ViewModels.{{Entity}}EditorDialogViewModel>();
    }
}
