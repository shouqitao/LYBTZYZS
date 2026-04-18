using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Desktop.Infrastructure.Navigation;

namespace LYBT.Desktop.Infrastructure.Navigation.Controls
{
    /// <summary>
    /// Navigation History Panel ViewModel - Phase 2.1: Navigation Improvements
    /// 导航历史面板 ViewModel
    /// </summary>
    public class NavigationHistoryPanelViewModel : BindableBase
    {
        private readonly IEnhancedNavigationService _navigationService;
        private ReadOnlyObservableCollection<NavigationEntry> _history;
        private NavigationEntry? _selectedEntry;

        /// <summary>
        /// 构造函数
        /// </summary>
        public NavigationHistoryPanelViewModel(IEnhancedNavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Subscribe to history changes
            _history = (ReadOnlyObservableCollection<NavigationEntry>)_navigationService.History;

            // Subscribe to navigation events
            _navigationService.Navigated += OnNavigated;

            // Setup commands
            ClearHistoryCommand = new DelegateCommand(ExecuteClearHistory, CanExecuteClearHistory);
            NavigateToEntryCommand = new DelegateCommand<NavigationEntry>(ExecuteNavigateToEntry, CanExecuteNavigateToEntry);
        }

        #region Properties

        /// <summary>
        /// 导航历史集合
        /// </summary>
        public ReadOnlyObservableCollection<NavigationEntry> History => _history;

        /// <summary>
        /// 是否有任何历史记录
        /// </summary>
        public bool HasHistory => _history != null && _history.Count > 0;

        /// <summary>
        /// 历史记录数量
        /// </summary>
        public int HistoryCount => _history?.Count ?? 0;

        /// <summary>
        /// 当前选中的历史条目
        /// </summary>
        public NavigationEntry? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (SetProperty(ref _selectedEntry, value))
                {
                    if (NavigateToEntryCommand is DelegateCommand<NavigationEntry> command)
                    {
                        command.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 清除历史命令
        /// </summary>
        public ICommand ClearHistoryCommand { get; }

        private bool CanExecuteClearHistory()
        {
            return HasHistory;
        }

        private void ExecuteClearHistory()
        {
            _navigationService.ClearHistory();
            RaisePropertyChanged(nameof(HasHistory));
            RaisePropertyChanged(nameof(HistoryCount));
        }

        /// <summary>
        /// 导航到历史条目命令
        /// </summary>
        public ICommand NavigateToEntryCommand { get; }

        private bool CanExecuteNavigateToEntry(NavigationEntry? entry)
        {
            return entry != null;
        }

        private void ExecuteNavigateToEntry(NavigationEntry? entry)
        {
            if (entry == null)
                return;

            // Navigate to the entry's URI
            var _ = _navigationService.NavigateAsync(entry.Uri, entry.Parameters);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 格式化时间戳
        /// </summary>
        public string FormatTimestamp(DateTime timestamp)
        {
            var now = DateTime.UtcNow;
            var diff = now - timestamp;

            if (diff.TotalMinutes < 1)
                return "刚刚";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} 分钟前";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} 小时前";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} 天前";

            return timestamp.ToString("MM-dd HH:mm");
        }

        /// <summary>
        /// 获取导航图标
        /// </summary>
        public string GetNavigationIcon(string uri)
        {
            // Return icon based on URI pattern
            if (uri.Contains("MedicalCase"))
                return "📋"; // Medical case icon
            if (uri.Contains("Patient"))
                return "👤"; // Patient icon
            if (uri.Contains("Prescription"))
                return "💊"; // Prescription icon
            if (uri.Contains("Home"))
                return "🏠"; // Home icon

            return "📄"; // Default icon
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 导航完成时刷新 UI
        /// </summary>
        private void OnNavigated(object? sender, NavigatedEventArgs e)
        {
            // Refresh collections
            RaisePropertyChanged(nameof(History));
            RaisePropertyChanged(nameof(HasHistory));
            RaisePropertyChanged(nameof(HistoryCount));

            // Refresh commands
            if (ClearHistoryCommand is DelegateCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
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
    /// NavigationHistoryPanel (View-only placeholder)
    /// 实际实现位于 NavigationHistoryPanel.xaml
    /// </summary>
    public partial class NavigationHistoryPanel
    {
        // View implementation in XAML
    }
}
