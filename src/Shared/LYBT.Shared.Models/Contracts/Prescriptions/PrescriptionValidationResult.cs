namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 处方验证结果 - 简化版，只保留基本验证
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

        // 删除警告信息，简化验证逻辑
    }
}
