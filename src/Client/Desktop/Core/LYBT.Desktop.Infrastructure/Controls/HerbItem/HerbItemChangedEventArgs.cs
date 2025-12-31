using LYBT.Desktop.Infrastructure.Models;

namespace LYBT.Desktop.Infrastructure.Controls.HerbItem
{
    /// <summary>
    /// 药材项变更事件参数
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public class HerbItemChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更类型
        /// </summary>
        public HerbItemChangeType ChangeType { get; }

        /// <summary>
        /// 变更后的药材数据
        /// </summary>
        public HerbItemDto Item { get; }

        /// <summary>
        /// 项目在列表中的索引(如适用)
        /// </summary>
        public int Index { get; }

        public HerbItemChangedEventArgs(HerbItemChangeType changeType, HerbItemDto item, int index = -1)
        {
            ChangeType = changeType;
            Item = item;
            Index = index;
        }
    }

    /// <summary>
    /// 药材项变更类型
    /// </summary>
    public enum HerbItemChangeType
    {
        /// <summary>
        /// 药材选择变更
        /// </summary>
        HerbSelected,

        /// <summary>
        /// 剂量变更
        /// </summary>
        DosageChanged,

        /// <summary>
        /// 煎法变更
        /// </summary>
        DecocteMethodChanged,

        /// <summary>
        /// 数据已清空
        /// </summary>
        Cleared
    }
}
