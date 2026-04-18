using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Desktop.Infrastructure.Navigation;

namespace LYBT.Desktop.Infrastructure.Navigation.Controls
{
    /// <summary>
    /// Navigation Suggestions Panel ViewModel - Phase 2.1: Navigation Improvements
    /// 导航建议面板 ViewModel
    /// </summary>
    public class NavigationSuggestionsPanelViewModel : BindableBase
    {
        private readonly IEnhancedNavigationService _navigationService;
        private ObservableCollection<NavigationSuggestion> _suggestions;
        private NavigationSuggestion? _selectedSuggestion;

        /// <summary>
        /// 构造函数
        /// </summary>
        public NavigationSuggestionsPanelViewModel(IEnhancedNavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            _suggestions = new ObservableCollection<NavigationSuggestion>();
            IsLoading = false;

            // Setup commands
            RefreshCommand = new DelegateCommand(ExecuteRefresh, CanExecuteRefresh);
            NavigateToSuggestionCommand = new DelegateCommand<NavigationSuggestion>(ExecuteNavigateToSuggestion, CanExecuteNavigateToSuggestion);

            // Load initial suggestions
            LoadSuggestions();

            // Subscribe to navigation events to refresh suggestions
            _navigationService.Navigated += OnNavigated;
        }

        #region Properties

        /// <summary>
        /// 导航建议集合
        /// </summary>
        public ObservableCollection<NavigationSuggestion> Suggestions => _suggestions;

        /// <summary>
        /// 是否有任何建议
        /// </summary>
        public bool HasSuggestions => _suggestions != null && _suggestions.Count > 0;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// 当前选中的建议
        /// </summary>
        public NavigationSuggestion? SelectedSuggestion
        {
            get => _selectedSuggestion;
            set
            {
                if (SetProperty(ref _selectedSuggestion, value))
                {
                    if (NavigateToSuggestionCommand is DelegateCommand<NavigationSuggestion> command)
                    {
                        command.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 刷新建议命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        private bool CanExecuteRefresh()
        {
            return !IsLoading;
        }

        private void ExecuteRefresh()
        {
            LoadSuggestions();
        }

        /// <summary>
        /// 导航到建议命令
        /// </summary>
        public ICommand NavigateToSuggestionCommand { get; }

        private bool CanExecuteNavigateToSuggestion(NavigationSuggestion? suggestion)
        {
            return suggestion != null;
        }

        private void ExecuteNavigateToSuggestion(NavigationSuggestion? suggestion)
        {
            if (suggestion == null)
                return;

            // Navigate to the suggested URI
            var _ = _navigationService.NavigateAsync(suggestion.Uri);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 加载导航建议
        /// </summary>
        private async void LoadSuggestions()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            RaisePropertyChanged(nameof(IsLoading));

            try
            {
                // Simulate async operation (in real implementation, might query analytics)
                await Task.Delay(100);

                // Get suggestions from navigation service
                var newSuggestions = _navigationService.GetSuggestions(5);

                _suggestions.Clear();
                foreach (var suggestion in newSuggestions)
                {
                    _suggestions.Add(suggestion);
                }

                RaisePropertyChanged(nameof(Suggestions));
                RaisePropertyChanged(nameof(HasSuggestions));
            }
            finally
            {
                IsLoading = false;
                RaisePropertyChanged(nameof(IsLoading));
            }
        }

        /// <summary>
        /// 获取建议类型显示文本
        /// </summary>
        public string GetSuggestionTypeText(SuggestionType type)
        {
            return type switch
            {
                SuggestionType.Contextual => "上下文",
                SuggestionType.Frequent => "常用",
                SuggestionType.TimeBased => "时间",
                SuggestionType.Recent => "最近",
                SuggestionType.Pinned => "固定",
                _ => "建议"
            };
        }

        /// <summary>
        /// 获取建议类型颜色
        /// </summary>
        public string GetSuggestionTypeColor(SuggestionType type)
        {
            return type switch
            {
                SuggestionType.Contextual => "#2196F3", // Blue
                SuggestionType.Frequent => "#4CAF50", // Green
                SuggestionType.TimeBased => "#FF9800", // Orange
                SuggestionType.Recent => "#9C27B0", // Purple
                SuggestionType.Pinned => "#F44336", // Red
                _ => "#757575" // Gray
            };
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 导航完成时刷新建议
        /// </summary>
        private void OnNavigated(object? sender, NavigatedEventArgs e)
        {
            // Refresh suggestions after navigation
            LoadSuggestions();
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
    /// NavigationSuggestionsPanel (View-only placeholder)
    /// 实际实现位于 NavigationSuggestionsPanel.xaml
    /// </summary>
    public partial class NavigationSuggestionsPanel
    {
        // View implementation in XAML
    }
}
