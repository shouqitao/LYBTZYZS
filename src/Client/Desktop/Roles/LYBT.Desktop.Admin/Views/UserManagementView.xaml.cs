using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Admin.ViewModels;
using LYBT.Shared.Models.Enums;
using Prism.Regions;

namespace LYBT.Desktop.Admin.Views
{
    /// <summary>
    /// 用户管理视图
    ///
    /// 薄包装View，复用业务模块的UserMasterDetailControl
    /// 参数消费在 UserManagementViewModel.OnNavigatedTo；本 View 仅将 VM 结果转发给 Control
    /// </summary>
    public partial class UserManagementView : UserControl, INavigationAware
    {
        public UserManagementView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (_, _) => ApplyFilterFromDataContext();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UserManagementViewModel oldVm)
                oldVm.DefaultRoleFilterApplied -= ApplyRoleFilter;
            if (e.NewValue is UserManagementViewModel newVm)
            {
                newVm.DefaultRoleFilterApplied += ApplyRoleFilter;
                ApplyFilterFromDataContext();
            }
        }

        private void ApplyFilterFromDataContext()
        {
            if (DataContext is UserManagementViewModel vm && vm.DefaultRoleFilter.HasValue)
                ApplyRoleFilter(vm.DefaultRoleFilter.Value);
        }

        private void ApplyRoleFilter(UserRole role)
            => UserMasterDetailControl.SetDefaultRoleFilter(role);

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Prism 顺序：先 View.OnNavigatedTo，再 DataContext(INavigationAware).OnNavigatedTo
            // 参数消费在 VM；此处仅在 DataContext 已就绪且 VM 已有值时应用
            ApplyFilterFromDataContext();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
