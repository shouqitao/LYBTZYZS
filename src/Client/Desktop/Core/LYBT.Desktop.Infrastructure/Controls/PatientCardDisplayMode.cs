namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 患者信息卡片显示模式
    /// OpenSpec: refactor-medicalcase-workspace
    /// </summary>
    public enum PatientCardDisplayMode
    {
        /// <summary>
        /// 完整模式 - 显示所有患者信息
        /// </summary>
        Full,

        /// <summary>
        /// 紧凑模式 - 仅显示姓名、性别、年龄
        /// </summary>
        Compact,

        /// <summary>
        /// 最小模式 - 仅显示姓名
        /// </summary>
        Minimal
    }
}
