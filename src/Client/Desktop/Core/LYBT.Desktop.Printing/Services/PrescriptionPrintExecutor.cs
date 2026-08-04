using System.Printing;
using System.Windows.Controls;
using System.Windows.Documents;
using LYBT.Desktop.Printing.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Printing.Services
{
    /// <summary>
    /// 处方打印执行器
    /// U3-5: 从 PrescriptionPrintService 拆分，负责打印机管理与打印执行
    /// </summary>
    public class PrescriptionPrintExecutor : IDisposable
    {
        private readonly ILogger<PrescriptionPrintExecutor> _logger;
        private readonly LocalPrintServer _printServer = new();
        private string? _defaultPrinterName;
        private bool _disposed;

        public PrescriptionPrintExecutor(ILogger<PrescriptionPrintExecutor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 释放打印服务器资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                _printServer.Dispose();
            }
        }

        /// <summary>
        /// 获取打印机队列集合（供预览窗口填充打印机列表）
        /// </summary>
        public PrintQueueCollection GetPrintQueues()
        {
            return _printServer.GetPrintQueues();
        }

        /// <summary>
        /// 获取可用打印机列表
        /// </summary>
        public string[] GetAvailablePrinters()
        {
            try
            {
                var printQueues = _printServer.GetPrintQueues();

                return printQueues
                    .Where(pq => pq != null && !string.IsNullOrEmpty(pq.Name))
                    .Select(pq => pq.Name)
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PRINT] GetAvailablePrinters failed");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 设置默认打印机
        /// </summary>
        public void SetDefaultPrinter(string printerName)
        {
            if (string.IsNullOrEmpty(printerName))
                throw new ArgumentException("打印机名称不能为空", nameof(printerName));

            _defaultPrinterName = printerName;
            _logger.LogInformation("[PRINT] SetDefaultPrinter: {PrinterName}", printerName);
        }

        /// <summary>
        /// 获取默认打印机
        /// </summary>
        public string? GetDefaultPrinter()
        {
            return _defaultPrinterName ?? LocalPrintServer.GetDefaultPrintQueue()?.Name;
        }

        /// <summary>
        /// 通过打印对话框执行打印
        /// </summary>
        public bool ExecutePrintWithDialog(FixedDocument document, PrintOptions options)
        {
            var printDialog = new PrintDialog();
            SetupPrinter(printDialog, options);

            if (printDialog.ShowDialog() != true)
                return false;

            for (int i = 0; i < options.Copies; i++)
            {
                printDialog.PrintDocument(document.DocumentPaginator, "处方打印");
            }

            return true;
        }

        /// <summary>
        /// 直接打印（不显示对话框）
        /// </summary>
        public bool ExecutePrintDirect(FixedDocument document, PrintOptions options)
        {
            try
            {
                var printQueue = GetPrintQueue(options.PrinterName);
                if (printQueue == null)
                {
                    _logger.LogError("[PRINT] No printer available");
                    return false;
                }

                var paginator = document.DocumentPaginator;

                for (int i = 0; i < options.Copies; i++)
                {
                    var writer = PrintQueue.CreateXpsDocumentWriter(printQueue);
                    writer.Write(paginator);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PRINT] ExecutePrintDirect failed");
                return false;
            }
        }

        /// <summary>
        /// 配置打印对话框的打印机
        /// </summary>
        public void SetupPrinter(PrintDialog printDialog, PrintOptions options)
        {
            var printerName = options.PrinterName ?? _defaultPrinterName;
            if (string.IsNullOrEmpty(printerName))
                return;

            try
            {
                var printQueue = GetPrintQueue(printerName);
                if (printQueue != null)
                {
                    printDialog.PrintQueue = printQueue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PRINT] SetupPrinter failed: {PrinterName}", printerName);
            }
        }

        /// <summary>
        /// 根据打印机名称获取打印队列，找不到时回退到系统默认
        /// </summary>
        public PrintQueue? GetPrintQueue(string? printerName)
        {
            try
            {
                if (!string.IsNullOrEmpty(printerName))
                {
                    return _printServer.GetPrintQueue(printerName);
                }

                return LocalPrintServer.GetDefaultPrintQueue();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PRINT] GetPrintQueue failed for printer: {PrinterName}, falling back to default", printerName);
                return LocalPrintServer.GetDefaultPrintQueue();
            }
        }
    }
}
