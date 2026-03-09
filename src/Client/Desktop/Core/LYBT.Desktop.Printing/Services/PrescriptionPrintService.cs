using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Xps.Packaging;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Desktop.Printing.Templates;
using Microsoft.Extensions.Logging;

// T4-S5-01: PrintLogRequested event for print failure/success logging

namespace LYBT.Desktop.Printing.Services
{
    /// <summary>
    /// 处方打印服务实现
    /// OpenSpec: create-printing-module
    /// 使用FixedDocument + PrintDialog实现打印功能
    /// </summary>
    public class PrescriptionPrintService : IPrintService<PrescriptionPrintModel>
    {
        private readonly ILogger<PrescriptionPrintService> _logger;
        private string? _defaultPrinterName;

        /// <summary>
        /// 打印日志事件 - 在打印成功或失败时触发
        /// T4-S5-01: 调用方可订阅此事件以记录打印日志
        /// </summary>
        public event Action<PrintLogEntry>? PrintLogRequested;

        // 纸张尺寸定义（像素，96 DPI）
        private static readonly Size A5PageSize = new(559, 794);  // 148mm x 210mm
        private static readonly Size A4PageSize = new(794, 1123); // 210mm x 297mm

        public PrescriptionPrintService(ILogger<PrescriptionPrintService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                var pageSize = GetPageSize(options.PaperSize);
                var document = BuildFixedDocument(model, pageSize);

                bool success;
                if (options.ShowDialog)
                {
                    success = ExecutePrintWithDialog(document, options);
                }
                else
                {
                    success = ExecutePrintDirect(document, options);
                }

                if (success)
                {
                    _logger.LogInformation("[PRINT] PrintAsync completed successfully");
                    // T4-S5-01: 打印成功日志
                    PrintLogRequested?.Invoke(PrintLogEntry.Succeeded(options.PrinterName ?? _defaultPrinterName));
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
                // T4-S5-01: 打印失败日志
                PrintLogRequested?.Invoke(PrintLogEntry.Failed(ex.Message, options?.PrinterName ?? _defaultPrinterName));
                throw;
            }
        }

        /// <summary>
        /// 预览处方
        /// </summary>
        public async Task PreviewAsync(PrescriptionPrintModel model, PrintOptions? options = null)
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
                var pageSize = GetPageSize(options.PaperSize);
                var document = BuildFixedDocument(model, pageSize);

                ShowPreviewWindow(document, model, options);

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

                var document = BuildFixedDocument(model, A5PageSize);

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
        /// 批量打印
        /// </summary>
        public async Task<int> BatchPrintAsync(PrescriptionPrintModel[] models, PrintOptions? options = null)
        {
            if (models == null || models.Length == 0)
                throw new ArgumentException("打印列表不能为空", nameof(models));

            _logger.LogInformation("[PRINT] BatchPrintAsync started - Count={Count}", models.Length);

            int successCount = 0;
            options ??= new PrintOptions { ShowDialog = false };

            foreach (var model in models)
            {
                try
                {
                    if (await PrintAsync(model, options))
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PRINT] BatchPrintAsync item failed");
                }
            }

            _logger.LogInformation("[PRINT] BatchPrintAsync completed - Success={Success} Total={Total}",
                successCount, models.Length);

            return successCount;
        }

        /// <summary>
        /// 获取可用打印机列表
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

        #region Private Methods

        private static Size GetPageSize(Interfaces.PaperSize paperSize)
        {
            return paperSize switch
            {
                Interfaces.PaperSize.A4 => A4PageSize,
                Interfaces.PaperSize.A5 => A5PageSize,
                _ => A5PageSize
            };
        }

        // T4-S5-09: 分页阈值和容量常量
        private const int A5FirstPageHerbLimit = 12;      // A5首页最多显示12味药材
        private const int A4FirstPageHerbLimit = 20;      // A4首页最多显示20味药材
        private const int ContinuationPageHerbLimit = 20; // 续页最多显示20味药材（头部更简洁）

        /// <summary>
        /// 判断是否为A4纸张尺寸
        /// </summary>
        private static bool IsA4(Size pageSize) => pageSize.Width >= A4PageSize.Width;

        /// <summary>
        /// 根据纸张大小获取首页药材限制
        /// </summary>
        private static int GetFirstPageHerbLimit(Size pageSize) =>
            IsA4(pageSize) ? A4FirstPageHerbLimit : A5FirstPageHerbLimit;

        /// <summary>
        /// 构建FixedDocument，支持多页
        /// T4-S5-09: 当药材超过首页限制时自动分页 (A5=12味, A4=20味)
        /// </summary>
        private FixedDocument BuildFixedDocument(PrescriptionPrintModel model, Size pageSize)
        {
            var document = new FixedDocument();
            document.DocumentPaginator.PageSize = pageSize;

            var itemCount = model.Items?.Count ?? 0;
            var firstPageLimit = GetFirstPageHerbLimit(pageSize);

            if (itemCount <= firstPageLimit)
            {
                // 单页模式
                var pageContent = new PageContent();
                var fixedPage = CreateFixedPage(model, pageSize);
                ((IAddChild)pageContent).AddChild(fixedPage);
                document.Pages.Add(pageContent);
            }
            else
            {
                // 多页模式 T4-S5-09
                _logger.LogInformation("[PRINT] Multi-page mode: {ItemCount} herbs, threshold={Threshold}",
                    itemCount, firstPageLimit);
                BuildMultiPageDocument(document, model, pageSize);
            }

            return document;
        }

        /// <summary>
        /// 构建多页文档
        /// T4-S5-09: 首页显示前N味药材（使用完整模板），后续页使用续页模板
        /// </summary>
        private void BuildMultiPageDocument(FixedDocument document, PrescriptionPrintModel model, Size pageSize)
        {
            var allItems = model.Items?.ToList() ?? new List<PrescriptionItemPrintModel>();
            var totalItems = allItems.Count;
            var offset = 0;
            var firstPageLimit = GetFirstPageHerbLimit(pageSize);

            // 第1页：使用完整模板，限制药材数量
            var firstPageItems = allItems.Take(firstPageLimit).ToList();
            var firstPageModel = CloneModelWithItems(model, firstPageItems);
            var firstPage = CreateFixedPage(firstPageModel, pageSize);
            var firstPageContent = new PageContent();
            ((IAddChild)firstPageContent).AddChild(firstPage);
            document.Pages.Add(firstPageContent);
            offset += firstPageLimit;

            // 后续页：使用续页模板
            while (offset < totalItems)
            {
                var remainingCount = totalItems - offset;
                var pageItems = allItems.Skip(offset).Take(ContinuationPageHerbLimit).ToList();
                var isLastPage = (offset + pageItems.Count) >= totalItems;

                var continuationModel = CloneModelWithItems(model, pageItems);
                var continuationPage = CreateContinuationFixedPage(continuationModel, pageSize, isLastPage);
                var continuationPageContent = new PageContent();
                ((IAddChild)continuationPageContent).AddChild(continuationPage);
                document.Pages.Add(continuationPageContent);

                offset += pageItems.Count;
            }

            _logger.LogInformation("[PRINT] Multi-page document built: {PageCount} pages for {ItemCount} herbs",
                document.Pages.Count, totalItems);
        }

        /// <summary>
        /// 克隆打印模型但替换药材列表
        /// T4-S5-09
        /// </summary>
        private static PrescriptionPrintModel CloneModelWithItems(
            PrescriptionPrintModel source,
            List<PrescriptionItemPrintModel> items)
        {
            return new PrescriptionPrintModel
            {
                // 诊所信息
                ClinicName = source.ClinicName,
                ClinicAddress = source.ClinicAddress,
                ClinicPhone = source.ClinicPhone,
                Department = source.Department,

                // 患者信息
                PatientName = source.PatientName,
                Gender = source.Gender,
                Age = source.Age,
                ConsultationDate = source.ConsultationDate,
                OutpatientNumber = source.OutpatientNumber,
                PatientPhone = source.PatientPhone,
                PatientAddress = source.PatientAddress,

                // 诊断信息
                TcmDiagnosis = source.TcmDiagnosis,
                Symptoms = source.Symptoms,
                PresentIllness = source.PresentIllness,
                InspectionDiagnosis = source.InspectionDiagnosis,
                AuscultationDiagnosis = source.AuscultationDiagnosis,
                TongueDiagnosis = source.TongueDiagnosis,
                PulseDiagnosis = source.PulseDiagnosis,

                // 处方内容（替换药材列表）
                Items = items,
                DosageCount = source.DosageCount,
                Usage = source.Usage,
                Advice = source.Advice,
                FormulaSource = source.FormulaSource,

                // 费用信息
                ConsultationFee = source.ConsultationFee,
                MedicineFee = source.MedicineFee,
                TreatmentFee = source.TreatmentFee,
                SingleDosePrice = source.SingleDosePrice,
                Discount = source.Discount,
                TotalPrice = source.TotalPrice,

                // 签名
                DoctorName = source.DoctorName,
                PrescriptionDate = source.PrescriptionDate,
                Reviewer = source.Reviewer,
                Dispenser = source.Dispenser,
                PrescriptionNumber = source.PrescriptionNumber,
            };
        }

        /// <summary>
        /// 创建首页 FixedPage（根据纸张尺寸选择A4或A5模板）
        /// </summary>
        private FixedPage CreateFixedPage(PrescriptionPrintModel model, Size pageSize)
        {
            UserControl template = IsA4(pageSize)
                ? new PrescriptionPrintA4Template { DataContext = model, Width = pageSize.Width, Height = pageSize.Height }
                : new PrescriptionPrintTemplate { DataContext = model, Width = pageSize.Width, Height = pageSize.Height };

            template.Measure(pageSize);
            template.Arrange(new Rect(pageSize));
            template.UpdateLayout();

            var fixedPage = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = System.Windows.Media.Brushes.White
            };

            fixedPage.Children.Add(template);
            FixedPage.SetLeft(template, 0);
            FixedPage.SetTop(template, 0);

            fixedPage.Measure(pageSize);
            fixedPage.Arrange(new Rect(pageSize));
            fixedPage.UpdateLayout();

            return fixedPage;
        }

        /// <summary>
        /// 创建续页 FixedPage（根据纸张尺寸选择A4或A5续页模板）
        /// T4-S5-09
        /// </summary>
        private FixedPage CreateContinuationFixedPage(PrescriptionPrintModel model, Size pageSize, bool isLastPage)
        {
            UserControl template;
            if (IsA4(pageSize))
            {
                var a4Template = new PrescriptionContinuationA4Template
                {
                    DataContext = model,
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };
                if (isLastPage) a4Template.SetAsLastPage();
                template = a4Template;
            }
            else
            {
                var a5Template = new PrescriptionContinuationTemplate
                {
                    DataContext = model,
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };
                if (isLastPage) a5Template.SetAsLastPage();
                template = a5Template;
            }

            template.Measure(pageSize);
            template.Arrange(new Rect(pageSize));
            template.UpdateLayout();

            var fixedPage = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = System.Windows.Media.Brushes.White
            };

            fixedPage.Children.Add(template);
            FixedPage.SetLeft(template, 0);
            FixedPage.SetTop(template, 0);

            fixedPage.Measure(pageSize);
            fixedPage.Arrange(new Rect(pageSize));
            fixedPage.UpdateLayout();

            return fixedPage;
        }

        private bool ExecutePrintWithDialog(FixedDocument document, PrintOptions options)
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

        private bool ExecutePrintDirect(FixedDocument document, PrintOptions options)
        {
            try
            {
                var printQueue = GetPrintQueue(options.PrinterName);
                if (printQueue == null)
                {
                    _logger.LogError("[PRINT] No printer available");
                    // T4-S5-01: 打印失败日志 - 无可用打印机
                    PrintLogRequested?.Invoke(PrintLogEntry.Failed("No printer available", options.PrinterName));
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
                // T4-S5-01: 打印失败日志
                PrintLogRequested?.Invoke(PrintLogEntry.Failed(ex.Message, options.PrinterName));
                return false;
            }
        }

        private void SetupPrinter(PrintDialog printDialog, PrintOptions options)
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

        private PrintQueue? GetPrintQueue(string? printerName)
        {
            try
            {
                if (!string.IsNullOrEmpty(printerName))
                {
                    var printServer = new LocalPrintServer();
                    return printServer.GetPrintQueue(printerName);
                }

                return LocalPrintServer.GetDefaultPrintQueue();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PRINT] GetPrintQueue failed for printer: {PrinterName}, falling back to default", printerName);
                return LocalPrintServer.GetDefaultPrintQueue();
            }
        }

        private void ShowPreviewWindow(FixedDocument document, PrescriptionPrintModel model, PrintOptions options)
        {
            var previewWindow = new Window
            {
                Title = "处方预览",
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = System.Windows.Media.Brushes.White
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 预览区域
            var docViewer = new DocumentViewer
            {
                Document = document,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(240, 240, 240))
            };
            docViewer.FitToWidth();

            var previewBorder = new Border { Child = docViewer };
            Grid.SetColumn(previewBorder, 1);
            mainGrid.Children.Add(previewBorder);

            // 设置面板
            var settingsPanel = CreateSettingsPanel(document, model, options, previewWindow, docViewer);
            Grid.SetColumn(settingsPanel, 0);
            mainGrid.Children.Add(settingsPanel);

            previewWindow.Content = mainGrid;
            previewWindow.ShowDialog();
        }

        private Border CreateSettingsPanel(
            FixedDocument document,
            PrescriptionPrintModel model,
            PrintOptions options,
            Window parentWindow,
            DocumentViewer docViewer)
        {
            var settingsBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(245, 245, 245)),
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(15)
            };

            var settingsStack = new StackPanel();

            // 标题
            settingsStack.Children.Add(new TextBlock
            {
                Text = "打印设置",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // 打印机选择
            settingsStack.Children.Add(new TextBlock { Text = "打印机", Margin = new Thickness(0, 0, 0, 5) });
            var printerComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 15), Height = 28 };
            PopulatePrinterList(printerComboBox);
            settingsStack.Children.Add(printerComboBox);

            // 份数
            settingsStack.Children.Add(new TextBlock { Text = "份数", Margin = new Thickness(0, 0, 0, 5) });
            var copiesPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var copiesTextBox = new TextBox
            {
                Text = options.Copies.ToString(),
                Width = 60,
                Height = 28,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            var decreaseBtn = new Button { Content = "-", Width = 28, Height = 28 };
            var increaseBtn = new Button { Content = "+", Width = 28, Height = 28, Margin = new Thickness(5, 0, 0, 0) };

            decreaseBtn.Click += (s, e) =>
            {
                if (int.TryParse(copiesTextBox.Text, out int copies) && copies > 1)
                    copiesTextBox.Text = (copies - 1).ToString();
            };
            increaseBtn.Click += (s, e) =>
            {
                if (int.TryParse(copiesTextBox.Text, out int copies) && copies < 99)
                    copiesTextBox.Text = (copies + 1).ToString();
            };

            copiesPanel.Children.Add(decreaseBtn);
            copiesPanel.Children.Add(copiesTextBox);
            copiesPanel.Children.Add(increaseBtn);
            settingsStack.Children.Add(copiesPanel);

            // 纸张大小
            settingsStack.Children.Add(new TextBlock { Text = "纸张尺寸", Margin = new Thickness(0, 0, 0, 5) });
            var paperSizeComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 15), Height = 28 };
            paperSizeComboBox.Items.Add(new ComboBoxItem { Content = "A5 (148 x 210 mm)", Tag = Interfaces.PaperSize.A5 });
            paperSizeComboBox.Items.Add(new ComboBoxItem { Content = "A4 (210 x 297 mm)", Tag = Interfaces.PaperSize.A4 });
            paperSizeComboBox.SelectedIndex = options.PaperSize == Interfaces.PaperSize.A4 ? 1 : 0;

            var currentDocument = document;
            paperSizeComboBox.SelectionChanged += (s, e) =>
            {
                if (paperSizeComboBox.SelectedItem is ComboBoxItem item && item.Tag is Interfaces.PaperSize size)
                {
                    var newPageSize = GetPageSize(size);
                    currentDocument = BuildFixedDocument(model, newPageSize);
                    docViewer.Document = currentDocument;
                }
            };
            settingsStack.Children.Add(paperSizeComboBox);

            // 分隔线
            settingsStack.Children.Add(new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 5, 0, 20)
            });

            // 打印按钮
            var printButton = new Button
            {
                Content = "打印",
                Height = 35,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0, 120, 212)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };

            var cancelButton = new Button { Content = "取消", Height = 35 };

            printButton.Click += (s, e) =>
            {
                if (!int.TryParse(copiesTextBox.Text, out int copies) || copies < 1)
                    copies = 1;

                var selectedPrinter = (printerComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var printOptions = new PrintOptions
                {
                    PrinterName = selectedPrinter,
                    Copies = copies,
                    ShowDialog = false
                };

                ExecutePrintDirect(currentDocument, printOptions);
                parentWindow.Close();
            };

            cancelButton.Click += (s, e) => parentWindow.Close();

            settingsStack.Children.Add(printButton);
            settingsStack.Children.Add(cancelButton);

            settingsBorder.Child = settingsStack;
            return settingsBorder;
        }

        private void PopulatePrinterList(ComboBox printerComboBox)
        {
            try
            {
                var printServer = new LocalPrintServer();
                var defaultPrinter = LocalPrintServer.GetDefaultPrintQueue();
                var printQueues = printServer.GetPrintQueues();

                foreach (var pq in printQueues)
                {
                    if (pq != null && !string.IsNullOrEmpty(pq.Name))
                    {
                        var isDefault = pq.Name == defaultPrinter?.Name;
                        var displayName = isDefault ? $"{pq.Name} (默认)" : pq.Name;
                        var item = new ComboBoxItem { Content = displayName, Tag = pq.Name };
                        printerComboBox.Items.Add(item);

                        if (isDefault)
                        {
                            printerComboBox.SelectedItem = item;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PRINT] PopulatePrinterList failed");
            }
        }

        #endregion
    }
}
