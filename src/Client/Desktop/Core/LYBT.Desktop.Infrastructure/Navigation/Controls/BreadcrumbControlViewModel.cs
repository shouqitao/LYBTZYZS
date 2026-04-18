using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Desktop.Infrastructure.Navigation;

namespace LYBT.Desktop.Infrastructure.Navigation.Controls
{
    /// <summary>
    /// Breadcrumb Control ViewModel - Phase 2.1: Navigation Improvements
    /// 面包屑导航控件 ViewModel
    /// </summary>
    public class BreadcrumbControlViewModel : BindableBase
    {
        private readonly IEnhancedNavigationService _navigationService;
        private ReadOnlyObservableCollection<BreadcrumbItem> _items;

        /// <summary>
        /// 构造函数
        /// </summary>
        public BreadcrumbControlViewModel(IEnhancedNavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Subscribe to breadcrumb changes
            _items = (ReadOnlyObservableCollection<BreadcrumbItem>)_navigationService.Breadcrumbs;

            // Subscribe to navigation events to update UI
            _navigationService.Navigated += OnNavigated;
        }

        #region Properties

        /// <summary>
        /// 面包屑项集合
        /// </summary>
        public ReadOnlyObservableCollection<BreadcrumbItem> Items => _items;

        /// <summary>
        /// 是否有任何面包屑项
        /// </summary>
        public bool HasItems => _items != null && _items.Count > 0;

        #endregion

        #region Commands

        /// <summary>
        /// 导航到指定面包屑
        /// </summary>
        public ICommand NavigateCommand => new DelegateCommand<BreadcrumbItem>(
            ExecuteNavigateCommand,
            CanExecuteNavigateCommand
        );

        private bool CanExecuteNavigateCommand(BreadcrumbItem? item)
        {
            return item != null && !item.IsActive && item.NavigateCommand != null;
        }

        private void ExecuteNavigateCommand(BreadcrumbItem? item)
        {
            if (item == null || item.IsActive)
                return;

            // Use the item's navigate command
            if (item.NavigateCommand is ICommand command && command.CanExecute(item))
            {
                command.Execute(item);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 导航完成时刷新 UI
        /// </summary>
        private void OnNavigated(object? sender, NavigatedEventArgs e)
        {
            // Refresh items collection
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(HasItems));
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            if (_navigationService != null)
            {
                _navigationService.Navigated -= OnNavigated;
            }
        }

        #endregion
    }

    /// <summary>
    /// Breadcrumb Control (View-only placeholder)
    /// 实际实现位于 BreadcrumbControl.xaml
    /// </summary>
    public partial class BreadcrumbControl
    {
        // View implementation in XAML
        // This class serves as a code-behind placeholder if needed
    }
}
