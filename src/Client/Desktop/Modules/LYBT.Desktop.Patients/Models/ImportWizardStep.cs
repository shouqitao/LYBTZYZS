namespace LYBT.Desktop.Patients.Models
{

    /// <summary>
    /// 导入向导步骤枚举
    /// </summary>
    public enum ImportWizardStep
    {

        /// <summary>
        /// 步骤1：模板下载
        /// </summary>
        TemplateDownload = 1,

        /// <summary>
        /// 步骤2：文件选择
        /// </summary>
        FileSelection = 2,

        /// <summary>
        /// 步骤3：数据预览
        /// </summary>
        DataPreview = 3,

        /// <summary>
        /// 步骤4：导入执行
        /// </summary>
        ImportExecution = 4
    }

    /// <summary>
    /// 导入进度信息
    /// </summary>
    public class ImportProgressInfo
    {

        /// <summary>
        /// 进度百分比 (0-100)
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// 当前处理的项目描述
        /// </summary>
        public string CurrentItem { get; set; } = string.Empty;

        /// <summary>
        /// 已处理数量
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    // Issue #1781 Task 8 Phase 1: ImportValidationResult已移至LYBT.Desktop.Contracts.Models
}
