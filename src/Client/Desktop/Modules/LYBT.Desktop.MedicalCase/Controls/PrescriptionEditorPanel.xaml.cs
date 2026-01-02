using System.ComponentModel;
using System.Windows.Controls;
using LYBT.Desktop.Infrastructure.Controls.HerbList;
using LYBT.Desktop.Infrastructure.Models;
using LYBT.Desktop.MedicalCase.ViewModels;

namespace LYBT.Desktop.MedicalCase.Controls
{
    /// <summary>
    /// Epic #2210 Phase 4: 处方面板UserControl
    /// OpenSpec: simplify-workspace-event-architecture - 使用属性绑定替代事件
    /// 用于MedicalCaseWorkspaceView的右侧60%区域
    /// </summary>
    public partial class PrescriptionEditorPanel : UserControl
    {
        private PrescriptionPanelViewModel? _currentViewModel;

        public PrescriptionEditorPanel()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// DataContext变更时订阅/取消订阅ViewModel属性变更
        /// OpenSpec: simplify-workspace-event-architecture - 使用PropertyChanged替代事件
        /// </summary>
        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // 取消旧ViewModel的属性变更订阅
            if (_currentViewModel != null)
            {
                _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            // 订阅新ViewModel的属性变更
            _currentViewModel = e.NewValue as PrescriptionPanelViewModel;
            if (_currentViewModel != null)
            {
                _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        /// <summary>
        /// 处理ViewModel属性变更
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PrescriptionPanelViewModel.PendingAddHerbs):
                    HandlePendingAddHerbs();
                    break;
            }
        }

        /// <summary>
        /// 处理待添加药材（导入场景）
        /// </summary>
        private void HandlePendingAddHerbs()
        {
            if (_currentViewModel?.PendingAddHerbs == null || !_currentViewModel.PendingAddHerbs.Any())
                return;

            // 调用控件的AddHerbs方法（内部处理重复检测和剂量合并）
            HerbListCtrl.AddHerbs(_currentViewModel.PendingAddHerbs);

            // 清空待处理项
            _currentViewModel.ClearPendingAddHerbs();
        }

        /// <summary>
        /// 获取药材列表控件（供外部访问）
        /// </summary>
        public HerbListControl HerbListControl => HerbListCtrl;

        /// <summary>
        /// 获取当前药材列表
        /// </summary>
        public IReadOnlyList<HerbItemDto> HerbList => HerbListCtrl.HerbList;

        #region 兼容方法 - 已弃用

        /// <summary>
        /// 从DTO加载药材数据
        /// </summary>
        [Obsolete("使用HerbItemsToLoad属性绑定替代。")]
        public void LoadHerbItems(IEnumerable<HerbItemDto> items)
        {
            HerbListCtrl.LoadFromDto(items);
        }

        /// <summary>
        /// 批量添加药材
        /// </summary>
        [Obsolete("使用PendingAddHerbs属性替代。")]
        public void AddHerbs(IEnumerable<HerbItemDto> herbs)
        {
            HerbListCtrl.AddHerbs(herbs);
        }

        /// <summary>
        /// 清空药材列表
        /// </summary>
        [Obsolete("使用HerbItemsToLoad = null替代。")]
        public void ClearHerbs()
        {
            HerbListCtrl.Clear();
        }

        #endregion
    }
}
