namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 统一处方药材DTO（用于ComboBox绑定）
    /// Epic #1701: PrescriptionView + PrescriptionEditorDialog合并
    /// </summary>
    public class UnifiedHerbDto
    {
        /// <summary>
        /// 药材名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 拼音码
        /// </summary>
        public string PinyinCode { get; set; } = string.Empty;
    }
}
