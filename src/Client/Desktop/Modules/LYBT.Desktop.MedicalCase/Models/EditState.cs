namespace LYBT.Desktop.MedicalCase.Models
{
    /// <summary>
    /// 编辑状态枚举
    /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-002
    /// 定义MedicalCaseWorkspaceView的编辑状态
    /// </summary>
    public enum EditState
    {
        /// <summary>
        /// 编辑中 - 所有表单字段可编辑
        /// 底部操作栏显示: [暂存医案] [打印处方笺] [完成看诊]
        /// </summary>
        Editing = 0,

        /// <summary>
        /// 只读 - 所有表单字段只读
        /// 底部操作栏显示: [修改医案](有权限时) [打印处方笺]
        /// </summary>
        ReadOnly = 1
    }
}
