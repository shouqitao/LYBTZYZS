using LYBT.Desktop.Sync.ViewModels;
using LYBT.Desktop.Sync.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Sync;

/// <summary>
/// 数据同步模块
/// OpenSpec: implement-data-sync
/// 提供基础数据（Herb、Patient、Formula）的双向同步功能
/// </summary>
[Module(ModuleName = nameof(SyncModule))]
[ModuleDependency("AuthenticationModule")]
public class SyncModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册 ViewModel
        containerRegistry.Register<SyncViewModel>();
        containerRegistry.Register<SyncConflictDialogViewModel>();

        // 注册导航视图
        containerRegistry.RegisterForNavigation<SyncView, SyncViewModel>();

        // 注册对话框
        containerRegistry.RegisterDialog<SyncConflictDialog, SyncConflictDialogViewModel>();
    }
}
