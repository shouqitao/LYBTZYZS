using System.Collections.ObjectModel;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Shared.Components
{
    /// <summary>
    /// 可编辑药材项接口 - 用于支持药材选择和编辑的UI控件
    /// 继承IHerbItem基础属性，添加编辑所需的药材选择功能
    /// OpenSpec: optimize-desktop-core - 使用轻量级HerbListDto
    /// </summary>
    public interface IHerbItemEditable : IHerbItem
    {
        /// <summary>
        /// 所有药材列表引用 - 由父ViewModel注入，用于药材选择
        /// </summary>
        ObservableCollection<HerbListDto>? AllHerbs { get; set; }

        /// <summary>
        /// 过滤后的药材列表 - 基于拼音码和名称的智能过滤结果
        /// </summary>
        ObservableCollection<HerbListDto> FilteredHerbs { get; }

        /// <summary>
        /// 选中的药材 - 设置后自动填充HerbId、HerbName、Unit等属性
        /// </summary>
        HerbListDto? SelectedHerb { get; set; }
    }
}
