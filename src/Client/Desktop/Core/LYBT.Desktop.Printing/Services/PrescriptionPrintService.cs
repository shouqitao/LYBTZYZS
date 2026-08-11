using System.IO;
using System.IO.Packaging;
using System.Windows.Xps.Packaging;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Printing.Services
{
    /// <summary>
    /// 处方打印服务实现
    /// 使用FixedDocument + PrintDialog实现打印功能
    /// U3-5: 页面构建/执行/预览逻辑已拆分至独立构建器类
    /// </summary>
    public class PrescriptionPrintService : IPrintService<PrescriptionPrintModel>
    {
        private readonly ILogger<PrescriptionPrintService> _logger;
        private readonly PrescriptionDocumentBuilder _documentBuilder;
        private readonly PrescriptionPrintExecutor _executor;
        private readonly PrescriptionPreviewWindowBuilder _previewWindowBuilder;

        public PrescriptionPrintService(
            ILogger<PrescriptionPrintService> logger,
            PrescriptionDocumentBuilder documentBuilder,
            PrescriptionPrintExecutor executor,
            PrescriptionPreviewWindowBuilder previewWindowBuilder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _previewWindowBuilder = previewWindowBuilder ?? throw new ArgumentNullException(nameof(previewWindowBuilder));
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        public async Task<bool> PrintAsync(PrescriptionPrintModel model, PrintOptions? options = null)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // CODE-24: 防御性检查 -- 空处方不应到达打印层
            if (model.Items == null || model.Items.Count == 0)
                throw new InvalidOperationException("处方无药材信息，无法打印");

            try
            {
                _logger.LogInformation("[PRINT] PrintAsync started");

                options ??= new PrintOptions();
                var pageSize = PrescriptionDocumentBuilder.GetPageSize(options.PaperSize);
                var document = _documentBuilder.BuildFixedDocument(model, pageSize);

                bool success;
                if (options.ShowDialog)
                {
                    success = _executor.ExecutePrintWithDialog(document, options);
                }
                else
                {
                    success = _executor.ExecutePrintDirect(document, options);
                }

                if (success)
                {
                    _logger.LogInformation("[PRINT] PrintAsync completed successfully");
                }
                else
                {
                    _logger.LogDebug("[PRINT] PrintAsync cancelled by user");
                }

                return await Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PRINT] PrintAsync failed");
                throw;
            }
        }

        /// <summary>
        /// 预览处方
        /// </summary>
        public async Task PreviewAsync(PrescriptionPrintModel model, PrintOptions? options = null, Action? onPrintCompleted = null)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // CODE-24: 防御性检查 -- 空处方不应到达打印层
            if (model.Items == null || model.Items.Count == 0)
                throw new InvalidOperationException("处方无药材信息，无法预览");

            try
            {
                _logger.LogDebug("[PRINT] PreviewAsync started");

                options ??= new PrintOptions();
                var pageSize = PrescriptionDocumentBuilder.GetPageSize(options.PaperSize);
                var document = _documentBuilder.BuildFixedDocument(model, pageSize);

                _previewWindowBuilder.ShowPreviewWindow(document, model, options, onPrintCompleted);

                _logger.LogDebug("[PRINT] PreviewAsync completed");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PRINT] PreviewAsync failed");
                throw;
            }
        }

        /// <summary>
        /// 导出处方为XPS
        /// </summary>
        public async Task<bool> ExportAsync(PrescriptionPrintModel model, string filePath, ExportFormat format = ExportFormat.Xps)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            try
            {
                // D1: PDF 导出 (QuestPDF)
                if (format == ExportFormat.Pdf)
                {
                    if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        filePath = Path.ChangeExtension(filePath, ".pdf");
                    }

                    _logger.LogInformation("[PRINT] PDF ExportAsync started - FilePath={FilePath}", filePath);
                    PrescriptionPdfExporter.Export(model, filePath);
                    _logger.LogInformation("[PRINT] PDF ExportAsync completed - FilePath={FilePath}", filePath);
                    return await Task.FromResult(true);
                }

                // XPS 导出 (原有逻辑)
                if (!filePath.EndsWith(".xps", StringComparison.OrdinalIgnoreCase))
                {
                    filePath = Path.ChangeExtension(filePath, ".xps");
                }

                _logger.LogInformation("[PRINT] XPS ExportAsync started - FilePath={FilePath}", filePath);

                var document = _documentBuilder.BuildFixedDocument(model, PrescriptionDocumentBuilder.A5PageSize);

                using (var package = Package.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var xpsDocument = new XpsDocument(package, CompressionOption.Maximum))
                    {
                        var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                        writer.Write(document.DocumentPaginator);
                    }
                }

                _logger.LogInformation("[PRINT] XPS ExportAsync completed - FilePath={FilePath}", filePath);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PRINT] ExportAsync failed - Format={Format}", format);
                throw;
            }
        }

        /// <summary>
        /// 获取可用打印机列表
        /// </summary>
        public string[] GetAvailablePrinters()
        {
            return _executor.GetAvailablePrinters();
        }

        /// <summary>
        /// 设置默认打印机
        /// </summary>
        public void SetDefaultPrinter(string printerName)
        {
            _executor.SetDefaultPrinter(printerName);
        }

        /// <summary>
        /// 获取默认打印机
        /// </summary>
        public string? GetDefaultPrinter()
        {
            return _executor.GetDefaultPrinter();
        }
    }
}
