using Prism.Mvvm;

namespace LYBT.UI.WPF.ViewModels.Navigation {
    /// <summary>
    /// 管理后台主视图模型
    /// </summary>
    class AdminViewModel : BindableBase {
        private int _selectedTabIndex;

        /// <summary>
        /// 当前选中的标签页索引
        /// </summary>
        public int SelectedTabIndex {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }
    }
}

