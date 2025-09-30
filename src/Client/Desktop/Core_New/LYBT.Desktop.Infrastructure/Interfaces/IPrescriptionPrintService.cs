namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 处方打印服务接口
    /// UltraThink标准打印系统
    /// </summary>
    public interface IPrescriptionPrintService
    {
        /// <summary>
        /// 打印处方
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>打印是否成功</returns>
        Task<bool> PrintPrescriptionAsync(int prescriptionId);

        /// <summary>
        /// 预览处方打印
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        Task PreviewPrintAsync(int prescriptionId);

        /// <summary>
        /// 配置打印设置
        /// </summary>
        void ConfigurePrintSettings();
    }
}