using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Herbs.Controls.HerbList
{
    /// <summary>
    /// 药材列表变更事件参数
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public class HerbListChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更类型
        /// </summary>
        public HerbListChangeType ChangeType { get; }

        /// <summary>
        /// 受影响的药材项(如适用)
        /// </summary>
        public PrescriptionItemDto? AffectedItem { get; }

        /// <summary>
        /// 受影响项的索引(如适用)
        /// </summary>
        public int AffectedIndex { get; }

        /// <summary>
        /// 当前列表中的有效药材数量
        /// </summary>
        public int ItemCount { get; }

        public HerbListChangedEventArgs(
            HerbListChangeType changeType,
            int itemCount,
            PrescriptionItemDto? affectedItem = null,
            int affectedIndex = -1)
        {
            ChangeType = changeType;
            ItemCount = itemCount;
            AffectedItem = affectedItem;
            AffectedIndex = affectedIndex;
        }
    }

    /// <summary>
    /// 药材列表变更类型
    /// </summary>
    public enum HerbListChangeType
    {
        /// <summary>
        /// 添加药材项
        /// </summary>
        ItemAdded,

        /// <summary>
        /// 删除药材项
        /// </summary>
        ItemRemoved,

        /// <summary>
        /// 修改药材项
        /// </summary>
        ItemModified,

        /// <summary>
        /// 清空列表
        /// </summary>
        Cleared,

        /// <summary>
        /// 加载数据
        /// </summary>
        Loaded,

        /// <summary>
        /// 移动位置
        /// </summary>
        ItemMoved,

        /// <summary>
        /// 批量导入
        /// </summary>
        BatchImported
    }
}
