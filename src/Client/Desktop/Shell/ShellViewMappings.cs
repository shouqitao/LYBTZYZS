namespace LYBT.Desktop.Shell;

/// <summary>
/// Shell 视图 → ViewModel 显式映射表（Prism 显式映射优于约定）。
/// <para>存在原因：Shell 控件的约定名与实际 VM 类名不一致（如 <c>HeaderControl</c> 的约定名
/// <c>HeaderControlViewModel</c> 并不存在，实际为 <c>HeaderViewModel</c>），缺失映射时 Prism 会**静默**
/// 跳过赋值并让控件继承宿主 DataContext，导致绑定大面积失效且无任何异常/日志。</para>
/// <para>唯一真相源：<c>App.ConfigureViewModelLocator</c> 与守卫测试 <c>ShellViewViewModelBindingTests</c>
/// 共用本表——新增 Shell 控件只需在此追加一行，守卫测试自动覆盖。</para>
/// </summary>
public static class ShellViewMappings
{
    /// <summary>视图 → ViewModel 映射（视图类型以 <see cref="Type"/> 表达，便于测试反射校验）</summary>
    public static IReadOnlyList<(Type View, Type ViewModel)> Mappings { get; } = new[]
    {
        (typeof(Views.MainWindow), typeof(ViewModels.MainWindowViewModel)),
        (typeof(Controls.AccountSettingsControl), typeof(ViewModels.AccountSettingsViewModel)),
        (typeof(Views.HeaderControl), typeof(ViewModels.HeaderViewModel)),
        (typeof(Views.SideNavControl), typeof(ViewModels.SideNavViewModel)),
        (typeof(Views.FooterControl), typeof(ViewModels.FooterViewModel)),
    };

    /// <summary>向 Prism <c>ViewModelLocationProvider</c> 注册本表全部映射</summary>
    public static void Register()
    {
        foreach (var (view, viewModel) in Mappings)
            Prism.Mvvm.ViewModelLocationProvider.Register(view.ToString()!, viewModel);
    }
}
