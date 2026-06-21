namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 导入验证结果
    /// </summary>
    public class ImportValidationResult
    {
        /// <summary>
        /// 验证是否通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 有效数据行数
        /// </summary>
        public int ValidRowCount { get; set; }

        /// <summary>
        /// 无效数据行数
        /// </summary>
        public int InvalidRowCount { get; set; }
    }
}
