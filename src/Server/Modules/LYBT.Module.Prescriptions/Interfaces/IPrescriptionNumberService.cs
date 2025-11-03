namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方编号生成服务接口
    /// Issue #1551: 处方自动编号功能
    /// </summary>
    public interface IPrescriptionNumberService
    {
        /// <summary>
        /// 生成处方编号
        /// 格式：RX-YYYYMMDD-NNNN
        /// 例如：RX-20251021-0001
        /// </summary>
        /// <param name="date">指定日期（通常为当前日期）</param>
        /// <returns>生成的处方编号</returns>
        Task<string> GenerateNumberAsync(DateTime date);

        /// <summary>
        /// 验证处方编号格式是否有效
        /// </summary>
        /// <param name="prescriptionNumber">待验证的处方编号</param>
        /// <returns>是否有效</returns>
        bool ValidateNumberFormat(string prescriptionNumber);
    }
}
