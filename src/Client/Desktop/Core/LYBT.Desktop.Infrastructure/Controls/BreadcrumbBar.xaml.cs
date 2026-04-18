using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls;

/// <summary>
/// BreadcrumbBar - 面包屑导航栏控件
/// Phase 2.1: Navigation Improvements
///
/// 显示当前导航路径，支持点击快速导航回上级页面
/// </summary>
public partial class BreadcrumbBar : UserControl
{
    public BreadcrumbBar()
    {
        InitializeComponent();
        Breadcrumbs = new ObservableCollection<BreadcrumbItem>();
    }

    #region NavigationPath dependency property

    public static readonly DependencyProperty NavigationPathProperty =
        DependencyProperty.Register(
            nameof(NavigationPath),
            typeof(string),
            typeof(BreadcrumbBar),
            new PropertyMetadata(string.Empty, OnNavigationPathChanged));

    public string NavigationPath
    {
        get => (string)GetValue(NavigationPathProperty);
        set => SetValue(NavigationPathProperty, value);
    }

    private static void OnNavigationPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BreadcrumbBar bar && e.NewValue is string path)
        {
            bar.UpdateBreadcrumbs(path);
        }
    }

    #endregion

    #region NavigateCommand dependency property

    public static readonly DependencyProperty NavigateCommandProperty =
        DependencyProperty.Register(
            nameof(NavigateCommand),
            typeof(ICommand),
            typeof(BreadcrumbBar),
            new PropertyMetadata(null));

    public ICommand NavigateCommand
    {
        get => (ICommand)GetValue(NavigateCommandProperty);
        set => SetValue(NavigateCommandProperty, value);
    }

    #endregion

    #region Breadcrumbs collection

    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; }

    #endregion

    /// <summary>
    /// Update breadcrumbs based on navigation path (e.g., "Patient > Clinical > Medical Case")
    /// </summary>
    private void UpdateBreadcrumbs(string path)
    {
        Breadcrumbs.Clear();

        if (string.IsNullOrWhiteSpace(path))
            return;

        // Split path by '>'
        var parts = path.Split('>', StringSplitOptions.TrimEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            Breadcrumbs.Add(new BreadcrumbItem
            {
                Label = parts[i].Trim(),
                Level = i + 1,
                IsCurrent = i == parts.Length - 1,
                IsLast = i == parts.Length - 1,
                NavigateCommand = NavigateCommand
            });
        }
    }
}

/// <summary>
/// Single breadcrumb item data model
/// </summary>
public class BreadcrumbItem
{
    public string Label { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsLast { get; set; }
    public ICommand? NavigateCommand { get; set; }
}
