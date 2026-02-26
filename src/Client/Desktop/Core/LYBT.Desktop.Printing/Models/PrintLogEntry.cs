namespace LYBT.Desktop.Printing.Models
{
    /// <summary>
    /// 打印日志条目 - 由打印服务在打印成功/失败时发出
    /// T4-S5-01: 支持打印日志记录回调
    /// </summary>
    public class PrintLogEntry
    {
        /// <summary>是否打印成功</summary>
        public bool IsSuccess { get; init; }

        /// <summary>打印机名称</summary>
        public string? PrinterName { get; init; }

        /// <summary>错误信息（失败时填写）</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>打印时间</summary>
        public DateTime PrintedAt { get; init; } = DateTime.Now;

        /// <summary>创建成功日志</summary>
        public static PrintLogEntry Succeeded(string? printerName = null)
            => new() { IsSuccess = true, PrinterName = printerName };

        /// <summary>创建失败日志</summary>
        public static PrintLogEntry Failed(string errorMessage, string? printerName = null)
            => new() { IsSuccess = false, ErrorMessage = errorMessage, PrinterName = printerName };
    }
}
