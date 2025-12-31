using System.Windows.Controls;
using LYBT.Desktop.Infrastructure.Controls.HerbList;
using LYBT.Desktop.Infrastructure.Models;
using LYBT.Desktop.MedicalCase.ViewModels;

namespace LYBT.Desktop.MedicalCase.Controls
{
    /// <summary>
    /// Epic #2210 Phase 4: 处方面板UserControl
    /// OpenSpec: herb-editor-control-refactoring - 使用HerbListControl
    /// 用于MedicalCaseWorkspaceView的右侧60%区域
    /// </summary>
    public partial class PrescriptionEditorPanel : UserControl
    {
        public PrescriptionEditorPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取药材列表控件（供ViewModel访问）
        /// </summary>
        public HerbListControl HerbListControl => HerbListCtrl;

        /// <summary>
        /// 获取当前药材列表
        /// </summary>
        public IReadOnlyList<HerbItemDto> HerbList => HerbListCtrl.HerbList;

        /// <summary>
        /// 从DTO加载药材数据
        /// </summary>
        public void LoadHerbItems(IEnumerable<HerbItemDto> items)
        {
            HerbListCtrl.LoadFromDto(items);
        }

        /// <summary>
        /// 批量添加药材
        /// </summary>
        public void AddHerbs(IEnumerable<HerbItemDto> herbs)
        {
            HerbListCtrl.AddHerbs(herbs);
        }

        /// <summary>
        /// 清空药材列表
        /// </summary>
        public void ClearHerbs()
        {
            HerbListCtrl.Clear();
        }

        /// <summary>
        /// 处理药材列表变更事件
        /// </summary>
        private void OnHerbListChanged(object? sender, HerbListChangedEventArgs e)
        {
            // 通知ViewModel更新计算
            if (DataContext is PrescriptionPanelViewModel vm)
            {
                // 先同步药材列表数据
                vm.SetCurrentHerbList(HerbListCtrl.HerbList);
                // 再触发事件处理
                vm.OnHerbListChanged(e);
            }
        }
    }
}
