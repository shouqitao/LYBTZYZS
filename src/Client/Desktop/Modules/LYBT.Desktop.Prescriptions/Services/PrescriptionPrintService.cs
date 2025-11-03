using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using LYBT.Desktop.Prescriptions.Models;
using LYBT.Desktop.Services.Print;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 处方打印服务实现
    /// Issue #1380: [PRINT-3] 实现处方打印服务
    /// 使用FlowDocument + PrintDialog实现打印功能
    /// </summary>
    public class PrescriptionPrintService : IPrescriptionPrintService
    {
        private readonly ILogger<PrescriptionPrintService> _logger;
        private string? _defaultPrinterName;

        public PrescriptionPrintService(ILogger<PrescriptionPrintService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 打印处方
        /// Issue #1794: 优化方法长度（39→20行），提取打印流程
        /// </summary>
        public async Task<bool> PrintPrescriptionAsync(PrescriptionDto prescription)
        {
            if (prescription == null)
                throw new ArgumentNullException(nameof(prescription));

            try
            {
                _logger.LogInformation("开始打印处方 ID: {PrescriptionId}", prescription.Id);

                var document = await PreparePrintDocumentAsync(prescription);
                var success = ExecutePrint(document, prescription.Id);

                _logger.LogInformation(success
                    ? "处方打印成功 ID: {PrescriptionId}"
                    : "用户取消打印 ID: {PrescriptionId}", prescription.Id);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印处方失败 ID: {PrescriptionId}", prescription.Id);
                throw;
            }
        }

        /// <summary>
        /// 准备打印文档
        /// Issue #1794: 从PrintPrescriptionAsync提取
        /// </summary>
        private async Task<FlowDocument> PreparePrintDocumentAsync(PrescriptionDto prescription)
        {
            var printDto = await MapToPrintDtoAsync(prescription);
            return BuildFlowDocument(printDto);
        }

        /// <summary>
        /// 执行打印操作
        /// Issue #1794: 从PrintPrescriptionAsync提取
        /// </summary>
        private bool ExecutePrint(FlowDocument document, Guid prescriptionId)
        {
            var printDialog = new PrintDialog();
            SetupDefaultPrinter(printDialog);

            if (printDialog.ShowDialog() != true)
                return false;

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            printDialog.PrintDocument(paginator, $"处方_{prescriptionId}");
            return true;
        }

        /// <summary>
        /// 预览处方
        /// </summary>
        public async Task PreviewPrescriptionAsync(PrescriptionDto prescription)
        {
            if (prescription == null)
                throw new ArgumentNullException(nameof(prescription));

            try
            {
                _logger.LogInformation("开始预览处方 ID: {PrescriptionId}", prescription.Id);

                // 1. 构建打印数据模型
                var printDto = await MapToPrintDtoAsync(prescription);

                // 2. 使用Builder构建FlowDocument
                var document = BuildFlowDocument(printDto);

                // 3. 创建预览窗口
                var previewWindow = new Window
                {
                    Title = $"处方预览 - {prescription.Id}",
                    Width = 900,
                    Height = 1100,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                // 4. 使用FlowDocumentScrollViewer显示文档
                var viewer = new FlowDocumentScrollViewer
                {
                    Document = document,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };

                previewWindow.Content = viewer;
                previewWindow.ShowDialog();

                _logger.LogInformation("处方预览完成 ID: {PrescriptionId}", prescription.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预览处方失败 ID: {PrescriptionId}", prescription.Id);
                throw;
            }
        }

        /// <summary>
        /// 批量打印处方
        /// </summary>
        public async Task<int> BatchPrintAsync(PrescriptionDto[] prescriptions)
        {
            if (prescriptions == null || prescriptions.Length == 0)
                throw new ArgumentException("处方列表不能为空", nameof(prescriptions));

            _logger.LogInformation("开始批量打印 {Count} 个处方", prescriptions.Length);

            int successCount = 0;

            foreach (var prescription in prescriptions)
            {
                try
                {
                    if (await PrintPrescriptionAsync(prescription))
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量打印处方失败 ID: {PrescriptionId}", prescription.Id);
                }
            }

            _logger.LogInformation("批量打印完成，成功: {SuccessCount}/{TotalCount}",
                successCount, prescriptions.Length);

            return successCount;
        }

        /// <summary>
        /// 导出处方为PDF（MVP阶段：导出为XPS格式）
        /// </summary>
        public async Task<bool> ExportToPdfAsync(PrescriptionDto prescription, string filePath)
        {
            if (prescription == null)
                throw new ArgumentNullException(nameof(prescription));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            try
            {
                _logger.LogInformation("开始导出处方 ID: {PrescriptionId} 到 {FilePath}",
                    prescription.Id, filePath);

                // MVP阶段：导出为XPS格式（原生WPF支持，无需第三方库）
                // 未来版本可以考虑添加XPS到PDF的转换

                // 确保文件扩展名为.xps
                if (!filePath.EndsWith(".xps", StringComparison.OrdinalIgnoreCase))
                {
                    filePath = Path.ChangeExtension(filePath, ".xps");
                }

                // 1. 构建打印数据模型
                var printDto = await MapToPrintDtoAsync(prescription);

                // 2. 使用Builder构建FlowDocument
                var document = BuildFlowDocument(printDto);

                // 3. 创建XPS文档
                using (var package = Package.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var xpsDocument = new XpsDocument(package, CompressionOption.Maximum))
                    {
                        var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                        writer.Write(paginator);
                    }
                }

                _logger.LogInformation("处方导出成功 ID: {PrescriptionId}, 文件: {FilePath}",
                    prescription.Id, filePath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出处方失败 ID: {PrescriptionId}", prescription.Id);
                throw;
            }
        }

        /// <summary>
        /// 获取可用的打印机列表
        /// </summary>
        public string[] GetAvailablePrinters()
        {
            try
            {
                var printServer = new LocalPrintServer();
                var printQueues = printServer.GetPrintQueues();

                return printQueues
                    .Where(pq => pq != null && !string.IsNullOrEmpty(pq.Name))
                    .Select(pq => pq.Name)
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取打印机列表失败");
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
            _logger.LogInformation("设置默认打印机: {PrinterName}", printerName);
        }

        /// <summary>
        /// 获取当前默认打印机
        /// </summary>
        public string? GetDefaultPrinter()
        {
            return _defaultPrinterName ?? LocalPrintServer.GetDefaultPrintQueue()?.Name;
        }

        // ===== 私有辅助方法 =====

        /// <summary>
        /// 映射处方药品项到打印模型
        /// </summary>
        private List<PrescriptionItemPrintDto> MapPrescriptionItems(IList<PrescriptionItemDto> items)
        {
            return items.Select((item, index) => new PrescriptionItemPrintDto
            {
                SequenceNumber = index + 1,
                HerbName = item.HerbName,
                Quantity = item.Quantity,
                Unit = item.Unit
            }).ToList();
        }

        /// <summary>
        /// 设置默认打印机
        /// </summary>
        private void SetupDefaultPrinter(PrintDialog printDialog)
        {
            if (string.IsNullOrEmpty(_defaultPrinterName))
                return;

            try
            {
                var printQueue = FindPrintQueue(_defaultPrinterName);
                if (printQueue != null)
                {
                    printDialog.PrintQueue = printQueue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "设置默认打印机失败: {PrinterName}", _defaultPrinterName);
            }
        }

        /// <summary>
        /// 将PrescriptionDto映射到PrescriptionPrintDto
        /// Issue #1794: 优化方法长度（48→18行），提取逻辑块
        /// TODO: 需要依赖其他服务获取患者、医生、病例等信息
        /// </summary>
        private async Task<PrescriptionPrintDto> MapToPrintDtoAsync(PrescriptionDto prescription)
        {
            var printDto = new PrescriptionPrintDto();

            // TODO: 在PRINT-4集成时，注入IPatientService、IUserService、IMedicalCaseService
            PopulateClinicInfo(printDto);
            PopulatePatientInfo(printDto);
            PopulateDiagnosisInfo(printDto, prescription);
            PopulatePrescriptionDetails(printDto, prescription);
            PopulateDoctorInfo(printDto, prescription);

            return await Task.FromResult(printDto);
        }

        /// <summary>
        /// 填充诊所信息
        /// Issue #1794: 从MapToPrintDtoAsync提取
        /// </summary>
        private static void PopulateClinicInfo(PrescriptionPrintDto printDto)
        {
            // TODO: 从配置或系统设置获取
            printDto.ClinicName = "中医门诊";
            printDto.ClinicAddress = null;
            printDto.ClinicPhone = null;
        }

        /// <summary>
        /// 填充患者信息
        /// Issue #1794: 从MapToPrintDtoAsync提取
        /// </summary>
        private static void PopulatePatientInfo(PrescriptionPrintDto printDto)
        {
            // TODO: 从IPatientService获取
            printDto.PatientName = "患者姓名";
            printDto.Gender = "男";
            printDto.Age = 0;
            printDto.ConsultationDate = DateTime.Now;
        }

        /// <summary>
        /// 填充四诊信息
        /// Issue #1794: 从MapToPrintDtoAsync提取
        /// </summary>
        private static void PopulateDiagnosisInfo(PrescriptionPrintDto printDto, PrescriptionDto prescription)
        {
            // TODO: 从IMedicalCaseService获取
            printDto.Inspection = null;
            printDto.AuscultationOlfaction = null;
            printDto.Inquiry = null;
            printDto.Palpation = null;
            printDto.TCMDiagnosis = prescription.Indication;
            printDto.TreatmentPrinciple = null;
        }

        /// <summary>
        /// 填充处方详情
        /// Issue #1794: 从MapToPrintDtoAsync提取
        /// </summary>
        private void PopulatePrescriptionDetails(PrescriptionPrintDto printDto, PrescriptionDto prescription)
        {
            printDto.Items = MapPrescriptionItems(prescription.Items);
            printDto.DosageCount = prescription.DosageCount;
            printDto.Usage = prescription.Usage ?? "水煎服，日一剂，分早晚服";
            printDto.SingleDosePrice = prescription.SingleDosePrice;
            printDto.TotalPrice = prescription.TotalPrice;
        }

        /// <summary>
        /// 填充医生信息和可选信息
        /// Issue #1794: 从MapToPrintDtoAsync提取
        /// </summary>
        private static void PopulateDoctorInfo(PrescriptionPrintDto printDto, PrescriptionDto prescription)
        {
            // TODO: 从IUserService获取
            printDto.DoctorName = "医生姓名";
            printDto.PrescriptionDate = DateTime.Now;
            printDto.PrescriptionNumber = prescription.Id.ToString("N").Substring(0, 8).ToUpper();
            printDto.Advice = prescription.Advice;
            printDto.FormulaSource = prescription.FormulaSource;
        }

        /// <summary>
        /// 使用Builder构建FlowDocument
        /// </summary>
        private FlowDocument BuildFlowDocument(PrescriptionPrintDto printDto)
        {
            var builder = new PrescriptionFlowDocumentBuilder(printDto);

            return builder
                .AddHeader()
                .AddPatientInfo()
                .AddFourDiagnostics()
                .AddPrescriptionTable()
                .AddUsageInstructions()
                .AddPriceInfo()
                .AddSignature()
                .Build();
        }

        /// <summary>
        /// 查找打印队列
        /// </summary>
        private PrintQueue? FindPrintQueue(string printerName)
        {
            try
            {
                var printServer = new LocalPrintServer();
                return printServer.GetPrintQueue(printerName);
            }
            catch
            {
                return null;
            }
        }
    }
}
