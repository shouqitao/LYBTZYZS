namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 更新看诊状态DTO
    /// </summary>
    public class UpdateStatusDto
    {
        /// <summary>
        /// 新状态
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 状态更新原因（可选）
        /// </summary>
        public string? Reason { get; set; }
    }
}