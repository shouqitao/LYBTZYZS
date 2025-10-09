using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Presentation.Print
{
    /// <summary>
    /// 处方打印服务接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供基本的处方打印功能
    /// </summary>
    public interface IPrescriptionPrintService
    {
        /// <summary>
        /// 打印处方
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <returns>是否打印成功</returns>
        Task<bool> PrintPrescriptionAsync(PrescriptionDto prescription);

        /// <summary>
        /// 预览处方
        /// </summary>
        /// <param name="prescription">处方信息</param>
        Task PreviewPrescriptionAsync(PrescriptionDto prescription);

        /// <summary>
        /// 批量打印处方
        /// </summary>
        /// <param name="prescriptions">处方列表</param>
        /// <returns>成功打印的数量</returns>
        Task<int> BatchPrintAsync(PrescriptionDto[] prescriptions);

        /// <summary>
        /// 导出处方为PDF
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>是否导出成功</returns>
        Task<bool> ExportToPdfAsync(PrescriptionDto prescription, string filePath);

        /// <summary>
        /// 获取可用的打印机列表
        /// </summary>
        /// <returns>打印机名称列表</returns>
        string[] GetAvailablePrinters();

        /// <summary>
        /// 设置默认打印机
        /// </summary>
        /// <param name="printerName">打印机名称</param>
        void SetDefaultPrinter(string printerName);

        /// <summary>
        /// 获取当前默认打印机
        /// </summary>
        string? GetDefaultPrinter();
    }

    /// <summary>
    /// 打印选项
    /// </summary>
    public class PrintOptions
    {
        /// <summary>
        /// 打印机名称
        /// </summary>
        public string? PrinterName { get; set; }

        /// <summary>
        /// 份数
        /// </summary>
        public int Copies { get; set; } = 1;

        /// <summary>
        /// 是否双面打印
        /// </summary>
        public bool DuplexPrinting { get; set; } = false;

        /// <summary>
        /// 纸张大小
        /// </summary>
        public PaperSize PaperSize { get; set; } = PaperSize.A4;

        /// <summary>
        /// 方向
        /// </summary>
        public PrintOrientation Orientation { get; set; } = PrintOrientation.Portrait;
    }

    /// <summary>
    /// 纸张大小
    /// </summary>
    public enum PaperSize
    {
        A4,
        A5,
        Letter,
        Legal
    }

    /// <summary>
    /// 打印方向
    /// </summary>
    public enum PrintOrientation
    {
        Portrait,
        Landscape
    }
}
