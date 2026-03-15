using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Infrastructure.Controls;

/// <summary>
/// MasterDetail控件非泛型基类
/// OpenSpec: refactor-frontend-srp-patterns Phase 3.3 - 提取控件共享逻辑
///
/// 职责：设置DataContext、Loaded时调用InitializeAsync
/// 使用非泛型基类以兼容WPF XAML（XAML不支持泛型根元素）
/// </summary>
public abstract class MasterDetailControlBase : UserControl
{
    /// <summary>
    /// 初始化ViewModel的异步支持
    /// 派生类应在构造函数中先调用InitializeComponent()，然后调用此方法
    /// </summary>
    protected void InitializeAsyncSupport()
    {
        // 设计模式下不执行初始化
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        // 注册Loaded事件用于异步初始化
        Loaded += async (sender, e) =>
        {
            // 只执行一次
            Loaded -= (RoutedEventHandler)((sender, e) => { });

            // 如果DataContext实现了IAsyncInitializable，调用InitializeAsync
            if (DataContext is IAsyncInitializable initializable)
            {
                await initializable.InitializeAsync();
            }
        };
    }
}
