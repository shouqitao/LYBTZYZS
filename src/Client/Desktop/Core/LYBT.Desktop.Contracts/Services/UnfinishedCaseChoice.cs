namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 未完成医案对话框用户选择枚举
    /// OpenSpec: optimize-medicalcase-navigation - 统一四选项弹窗
    /// </summary>
    public enum UnfinishedCaseChoice
    {
        /// <summary>继续看诊 - 返回到当前未完成医案</summary>
        Continue,

        /// <summary>关闭并新建 - 关闭当前医案并创建新医案</summary>
        CloseAndCreate,

        /// <summary>仅关闭 - 关闭当前医案，不新建</summary>
        CloseOnly,

        /// <summary>取消 - 取消操作，保持当前状态</summary>
        Cancel
    }
}
