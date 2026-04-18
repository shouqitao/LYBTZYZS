using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Navigation.Controls
{
    /// <summary>
    /// NavigationSuggestionsPanel - Phase 2.1: Navigation Improvements
    /// 导航建议面板 - 显示智能导航建议
    ///
    /// Usage:
    /// In XAML:
    /// <nav:NavigationSuggestionsPanel x:Name="SuggestionsPanel"/>
    ///
    /// In ViewModel:
    /// SuggestionsPanel.DataContext = new NavigationSuggestionsPanelViewModel(_navigationService);
    /// </summary>
    public partial class NavigationSuggestionsPanel : UserControl
    {
        public NavigationSuggestionsPanel()
        {
            InitializeComponent();
        }
    }
}
