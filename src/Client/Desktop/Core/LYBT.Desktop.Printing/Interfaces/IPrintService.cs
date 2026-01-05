namespace LYBT.Desktop.Printing.Interfaces
{
    /// <summary>
    /// 泛型打印服务接口
    /// OpenSpec: create-printing-module
    /// 提供类型安全的打印、预览、导出操作
    /// </summary>
    /// <typeparam name="TModel">打印数据模型类型</typeparam>
    public interface IPrintService<TModel> where TModel : class
    {
        /// <summary>
        /// 打印文档
        /// </summary>
        /// <param name="model">打印数据模型</param>
        /// <param name="options">打印选项（可选）</param>
        /// <returns>是否打印成功</returns>
        Task<bool> PrintAsync(TModel model, PrintOptions? options = null);

        /// <summary>
        /// 预览文档
        /// </summary>
        /// <param name="model">打印数据模型</param>
        /// <param name="options">打印选项（可选）</param>
        Task PreviewAsync(TModel model, PrintOptions? options = null);

        /// <summary>
        /// 导出文档
        /// </summary>
        /// <param name="model">打印数据模型</param>
        /// <param name="filePath">保存路径</param>
        /// <param name="format">导出格式</param>
        /// <returns>是否导出成功</returns>
        Task<bool> ExportAsync(TModel model, string filePath, ExportFormat format = ExportFormat.Xps);

        /// <summary>
        /// 批量打印
        /// </summary>
        /// <param name="models">打印数据模型列表</param>
        /// <param name="options">打印选项（可选）</param>
        /// <returns>成功打印的数量</returns>
        Task<int> BatchPrintAsync(TModel[] models, PrintOptions? options = null);

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
        /// 打印机名称（为空使用系统默认）
        /// </summary>
        public string? PrinterName { get; set; }

        /// <summary>
        /// 份数（默认1份）
        /// </summary>
        public int Copies { get; set; } = 1;

        /// <summary>
        /// 纸张大小（默认A5）
        /// </summary>
        public PaperSize PaperSize { get; set; } = PaperSize.A5;

        /// <summary>
        /// 打印方向（默认纵向）
        /// </summary>
        public PrintOrientation Orientation { get; set; } = PrintOrientation.Portrait;

        /// <summary>
        /// 是否双面打印（默认否）
        /// </summary>
        public bool DuplexPrinting { get; set; } = false;

        /// <summary>
        /// 是否显示打印对话框（默认是）
        /// </summary>
        public bool ShowDialog { get; set; } = true;
    }

    /// <summary>
    /// 纸张大小
    /// </summary>
    public enum PaperSize
    {
        /// <summary>
        /// A4 (210 x 297 mm)
        /// </summary>
        A4,

        /// <summary>
        /// A5 (148 x 210 mm) - 处方笺默认
        /// </summary>
        A5,

        /// <summary>
        /// Letter (8.5 x 11 in)
        /// </summary>
        Letter,

        /// <summary>
        /// Legal (8.5 x 14 in)
        /// </summary>
        Legal
    }

    /// <summary>
    /// 打印方向
    /// </summary>
    public enum PrintOrientation
    {
        /// <summary>
        /// 纵向
        /// </summary>
        Portrait,

        /// <summary>
        /// 横向
        /// </summary>
        Landscape
    }

    /// <summary>
    /// 导出格式
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// XPS格式（WPF原生支持）
        /// </summary>
        Xps,

        /// <summary>
        /// PDF格式（MVP阶段暂不支持，预留扩展）
        /// </summary>
        Pdf
    }
}
