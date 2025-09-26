namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 处方验证结果
    /// </summary>
    public class PrescriptionValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// 验证错误信息列表
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> WarningMessages { get; set; } = new List<string>();
    }
}