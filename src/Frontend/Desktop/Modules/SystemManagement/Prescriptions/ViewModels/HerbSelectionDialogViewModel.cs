using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LYBT.Desktop.Core.Models.Herbs;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;

namespace LYBT.Desktop.Admin.Prescriptions.ViewModels
{
    /// <summary>
    /// 药材选择对话框视图模型
    /// </summary>
    public class HerbSelectionDialogViewModel : BindableBase
    {
        #region 字段

        private string _title = "选择药材";
        private string _searchKeyword = string.Empty;
        private HerbInfo? _selectedHerb;
        private ObservableCollection<HerbInfo> _allHerbs;
        private ObservableCollection<HerbInfo> _filteredHerbs;
        private List<Guid> _excludedHerbIds;

        #endregion

        #region 属性

        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    FilterHerbs();
                }
            }
        }

        /// <summary>
        /// 选中的药材
        /// </summary>
        public HerbInfo? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                if (SetProperty(ref _selectedHerb, value))
                {
                    RaisePropertyChanged(nameof(HasSelectedHerb));
                }
            }
        }

        /// <summary>
        /// 所有可用药材
        /// </summary>
        public ObservableCollection<HerbInfo> AllHerbs
        {
            get => _allHerbs;
            set => SetProperty(ref _allHerbs, value);
        }

        /// <summary>
        /// 过滤后的药材列表
        /// </summary>
        public ObservableCollection<HerbInfo> FilteredHerbs
        {
            get => _filteredHerbs;
            set => SetProperty(ref _filteredHerbs, value);
        }

        /// <summary>
        /// 是否有选中的药材
        /// </summary>
        public bool HasSelectedHerb => SelectedHerb != null;

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }
        public DelegateCommand<HerbInfo> SelectHerbCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }
        
        /// <summary>
        /// 对话框结果
        /// </summary>
        public bool? DialogResult { get; set; }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化对话框数据
        /// </summary>
        public void Initialize(IEnumerable<HerbInfo> availableHerbs, List<Guid>? selectedHerbIds = null)
        {
            if (availableHerbs != null)
            {
                AllHerbs = new ObservableCollection<HerbInfo>(availableHerbs);
                FilteredHerbs = new ObservableCollection<HerbInfo>(availableHerbs);
            }

            if (selectedHerbIds != null)
            {
                _excludedHerbIds = selectedHerbIds;
                // 从列表中排除已选择的药材
                FilterHerbs();
            }
        }

        /// <summary>
        /// 获取选中的药材
        /// </summary>
        public HerbInfo? GetSelectedHerb()
        {
            return SelectedHerb;
        }

        #endregion

        #region 构造函数

        public HerbSelectionDialogViewModel()
        {
            _allHerbs = new ObservableCollection<HerbInfo>();
            _filteredHerbs = new ObservableCollection<HerbInfo>();
            _excludedHerbIds = new List<Guid>();

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            ClearSearchCommand = new DelegateCommand(ExecuteClearSearch);
            SelectHerbCommand = new DelegateCommand<HerbInfo>(ExecuteSelectHerb);
            ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm)
                .ObservesProperty(() => SelectedHerb);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        #endregion

        #region 命令实现

        private void ExecuteSearch()
        {
            FilterHerbs();
        }

        private void ExecuteClearSearch()
        {
            SearchKeyword = string.Empty;
            FilterHerbs();
        }

        private void ExecuteSelectHerb(HerbInfo? herb)
        {
            if (herb != null)
            {
                SelectedHerb = herb;
                ExecuteConfirm();
            }
        }

        private bool CanExecuteConfirm()
        {
            return SelectedHerb != null;
        }

        private void ExecuteConfirm()
        {
            DialogResult = true;
            // 关闭对话框的实际操作由View处理
        }

        private void ExecuteCancel()
        {
            DialogResult = false;
            SelectedHerb = null;
            // 关闭对话框的实际操作由View处理
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 过滤药材列表
        /// </summary>
        private void FilterHerbs()
        {
            if (AllHerbs == null || !AllHerbs.Any())
            {
                FilteredHerbs = new ObservableCollection<HerbInfo>();
                return;
            }

            var filtered = AllHerbs.AsEnumerable();

            // 排除已选择的药材
            if (_excludedHerbIds != null && _excludedHerbIds.Any())
            {
                filtered = filtered.Where(h => !_excludedHerbIds.Contains(h.Id));
            }

            // 按关键词过滤
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var keyword = SearchKeyword.Trim().ToLower();
                filtered = filtered.Where(h =>
                    (h.Name != null && h.Name.ToLower().Contains(keyword)) ||
                    (h.PinYinCode != null && h.PinYinCode.ToLower().Contains(keyword)) ||
                    (h.Effect != null && h.Effect.ToLower().Contains(keyword)));
            }

            FilteredHerbs = new ObservableCollection<HerbInfo>(filtered);

            // 如果过滤后只有一个结果，自动选中
            if (FilteredHerbs.Count == 1)
            {
                SelectedHerb = FilteredHerbs.First();
            }
        }

        #endregion
    }
}