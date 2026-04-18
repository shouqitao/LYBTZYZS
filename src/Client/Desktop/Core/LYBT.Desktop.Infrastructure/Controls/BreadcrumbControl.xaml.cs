using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Infrastructure.Converters;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Controls;

/// <summary>
/// 面包屑导航控件
/// 导航架构改进方案 — Phase 1: 面包屑导航 + 后退/前进按钮
/// </summary>
public partial class BreadcrumbControl : UserControl
{
    public BreadcrumbControl() => InitializeComponent();

    #region Breadcrumbs - 面包屑列表

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs
    {
        get => (IReadOnlyList<BreadcrumbItem>)GetValue(BreadcrumbsProperty);
        set => SetValue(BreadcrumbsProperty, value);
    }

    public static readonly DependencyProperty BreadcrumbsProperty =
        DependencyProperty.Register(nameof(Breadcrumbs), typeof(IReadOnlyList<BreadcrumbItem>),
            typeof(BreadcrumbControl), new PropertyMetadata(Array.Empty<BreadcrumbItem>()));

    #endregion

    #region NavigateBackCommand - 后退命令

    public ICommand? NavigateBackCommand
    {
        get => (ICommand?)GetValue(NavigateBackCommandProperty);
        set => SetValue(NavigateBackCommandProperty, value);
    }

    public static readonly DependencyProperty NavigateBackCommandProperty =
        DependencyProperty.Register(nameof(NavigateBackCommand), typeof(ICommand),
            typeof(BreadcrumbControl), new PropertyMetadata(null));

    #endregion

    #region NavigateForwardCommand - 前进命令

    public ICommand? NavigateForwardCommand
    {
        get => (ICommand?)GetValue(NavigateForwardCommandProperty);
        set => SetValue(NavigateForwardCommandProperty, value);
    }

    public static readonly DependencyProperty NavigateForwardCommandProperty =
        DependencyProperty.Register(nameof(NavigateForwardCommand), typeof(ICommand),
            typeof(BreadcrumbControl), new PropertyMetadata(null));

    #endregion

    #region NavigateToBreadcrumbCommand - 跳转到面包屑项命令

    public ICommand? NavigateToBreadcrumbCommand
    {
        get => (ICommand?)GetValue(NavigateToBreadcrumbCommandProperty);
        set => SetValue(NavigateToBreadcrumbCommandProperty, value);
    }

    public static readonly DependencyProperty NavigateToBreadcrumbCommandProperty =
        DependencyProperty.Register(nameof(NavigateToBreadcrumbCommand), typeof(ICommand),
            typeof(BreadcrumbControl), new PropertyMetadata(null));

    #endregion

    /// <summary>
    /// 面包屑项点击事件 — 跳转到对应视图
    /// </summary>
    private void OnBreadcrumbItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is BreadcrumbItem item && !item.IsCurrent)
        {
            NavigateToBreadcrumbCommand?.Execute(item);
        }
    }
}
