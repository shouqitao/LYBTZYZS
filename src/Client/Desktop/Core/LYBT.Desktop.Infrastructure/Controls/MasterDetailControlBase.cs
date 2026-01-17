using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Contracts.Services;
using Prism.Ioc;

namespace LYBT.Desktop.Infrastructure.Controls;

/// <summary>
/// MasterDetail控件非泛型基类
/// OpenSpec: refactor-frontend-srp-patterns Phase 3.3 - 提取控件共享逻辑
///
/// 职责：DI解析ViewModel、设置DataContext、Loaded时调用InitializeAsync
/// 使用非泛型基类以兼容WPF XAML（XAML不支持泛型根元素）
/// </summary>
public abstract class MasterDetailControlBase : UserControl
{
    /// <summary>
    /// ViewModel实例（基类存储为object，派生类通过泛型方法获取强类型）
    /// </summary>
    protected object? ViewModelInstance { get; private set; }

    /// <summary>
    /// 初始化ViewModel
    /// 派生类构造函数必须先调用InitializeComponent()，然后调用此方法
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel类型，必须实现IAsyncInitializable</typeparam>
    protected void InitializeViewModel<TViewModel>()
        where TViewModel : class, IAsyncInitializable
    {
        // 设计模式下不执行DI和初始化
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        // 从DI容器解析ViewModel
        var container = ContainerLocator.Container;
        var viewModel = container.Resolve<TViewModel>();
        ViewModelInstance = viewModel;
        DataContext = viewModel;

        // 注册Loaded事件用于异步初始化
        Loaded += async (sender, e) =>
        {
            // 只执行一次
            Loaded -= (RoutedEventHandler)((sender, e) => { });
            await viewModel.InitializeAsync();
        };
    }
}
