namespace LYBT.Desktop.MedicalCase.Models
{
    /// <summary>
    /// 工作区模式枚举
    /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-001
    /// 定义MedicalCaseWorkspaceView的来源模块
    /// </summary>
    public enum WorkspaceMode
    {
        /// <summary>
        /// 临床看诊模式 - 从PatientSelectionView进入
        /// 返回目标: PatientSelectionView
        /// 标题显示: "看诊中 | 患者：XXX"
        /// </summary>
        Clinical = 0,

        /// <summary>
        /// 管理编辑模式 - 从MedicalCaseManagementView进入
        /// 返回目标: MedicalCaseManagementView
        /// 标题显示: "编辑医案 | 患者：XXX"
        /// </summary>
        Management = 1,

        /// <summary>
        /// 前台挂号模式 - 前台选择患者挂号
        /// OpenSpec: refactor-clinical-workflow
        /// </summary>
        Reception = 2
    }
}
