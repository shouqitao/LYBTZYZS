using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Xps.Packaging;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Desktop.Prescriptions.Models;
using LYBT.Desktop.Prescriptions.Views;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
// OpenSpec: print-prescription-slip - 使用ConsultationInputDto以匹配ViewModel输出
// OpenSpec: enhance-prescription-print - 使用FixedDocument实现WYSIWYG打印
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 处方打印服务实现
    /// Issue #1380: [PRINT-3] 实现处方打印服务
    /// OpenSpec: print-prescription-slip - 支持完整上下文打印
    /// 使用FlowDocument + PrintDialog实现打印功能
    /// </summary>
    public class PrescriptionPrintService : IPrescriptionPrintService
    {
        private readonly ILogger<PrescriptionPrintService> _logger;
        private readonly IClinicSettingsService _clinicSettingsService;
        private string? _defaultPrinterName;

        public PrescriptionPrintService(
            ILogger<PrescriptionPrintService> logger,
            IClinicSettingsService clinicSettingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clinicSettingsService = clinicSettingsService ?? throw new ArgumentNullException(nameof(clinicSettingsService));
        }

        /// <summary>
        /// 打印处方
        /// Issue #1794: 优化方法长度（39→20行），提取打印流程
        /// </summary>
        public async Task<bool> PrintPrescriptionAsync(PrescriptionDetailDto prescription)
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
        /// OpenSpec: enhance-prescription-print - 使用FixedDocument
        /// </summary>
        private async Task<FixedDocument> PreparePrintDocumentAsync(PrescriptionDetailDto prescription)
        {
            var printDto = await MapToPrintDtoAsync(prescription, null, null);
            return BuildFixedDocument(printDto);
        }

        /// <summary>
        /// 准备打印文档（带完整上下文）
        /// OpenSpec: enhance-prescription-print - 使用FixedDocument
        /// </summary>
        private async Task<FixedDocument> PreparePrintDocumentAsync(PrescriptionDetailDto prescription, PatientDetailDto? patient, ConsultationInputDto? consultation)
        {
            var printDto = await MapToPrintDtoAsync(prescription, patient, consultation);
            return BuildFixedDocument(printDto);
        }

        /// <summary>
        /// 执行打印操作
        /// OpenSpec: enhance-prescription-print - 使用FixedDocument
        /// </summary>
        private bool ExecutePrint(FixedDocument document, Guid prescriptionId)
        {
            var printDialog = new PrintDialog();
            SetupDefaultPrinter(printDialog);

            if (printDialog.ShowDialog() != true)
                return false;

            // 使用FixedDocument的DocumentPaginator
            printDialog.PrintDocument(document.DocumentPaginator, $"处方_{prescriptionId}");
            return true;
        }

        /// <summary>
        /// 预览处方
        /// </summary>
        public async Task PreviewPrescriptionAsync(PrescriptionDetailDto prescription)
        {
            await PreviewPrescriptionAsync(prescription, null, null);
        }

        /// <summary>
        /// 打印处方（带完整上下文）
        /// OpenSpec: print-prescription-slip
        /// </summary>
        public async Task<bool> PrintPrescriptionAsync(PrescriptionDetailDto prescription, PatientDetailDto? patient, ConsultationInputDto? consultation)
        {
            if (prescription == null)
                throw new ArgumentNullException(nameof(prescription));

            try
            {
                _logger.LogInformation("开始打印处方 ID: {PrescriptionId}", prescription.Id);

                var document = await PreparePrintDocumentAsync(prescription, patient, consultation);
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
        /// 预览处方（带完整上下文）
        /// OpenSpec: enhance-prescription-print - 使用FixedDocument实现WYSIWYG预览
        /// </summary>
        public async Task PreviewPrescriptionAsync(PrescriptionDetailDto prescription, PatientDetailDto? patient, ConsultationInputDto? consultation)
        {
            if (prescription == null)
                throw new ArgumentNullException(nameof(prescription));

            try
            {
                _logger.LogInformation("开始预览处方 ID: {PrescriptionId}", prescription.Id);

                // 1. 构建打印数据模型
                var printDto = await MapToPrintDtoAsync(prescription, patient, consultation);

                // 2. 使用FixedDocument实现WYSIWYG（所见即所得）
                var document = BuildFixedDocument(printDto);

                // 3. 创建预览窗口 - 左右分栏布局
                var previewWindow = new Window
                {
                    Title = "处方预览",
                    Width = 900,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = System.Windows.Media.Brushes.White
                };

                // 4. 创建主布局 - 左右分栏
                var mainGrid = new Grid();
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) }); // 左侧设置面板
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 右侧预览区

                // ===== 右侧预览区域（先创建，以便传递给设置面板）=====
                var (previewArea, docViewer) = CreatePreviewAreaWithViewer(document);
                Grid.SetColumn(previewArea, 1);
                mainGrid.Children.Add(previewArea);

                // ===== 左侧设置面板（传递printDto和docViewer以支持纸张尺寸切换）=====
                var settingsPanel = CreateSettingsPanel(document, prescription.Id, previewWindow, printDto, docViewer);
                Grid.SetColumn(settingsPanel, 0);
                mainGrid.Children.Add(settingsPanel);

                previewWindow.Content = mainGrid;
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
        /// 创建左侧设置面板
        /// OpenSpec: enhance-prescription-print - 支持FixedDocument打印和纸张尺寸选择
        /// </summary>
        private Border CreateSettingsPanel(
            FixedDocument document,
            Guid prescriptionId,
            Window parentWindow,
            PrescriptionPrintModel printDto,
            DocumentViewer docViewer)
        {
            var settingsBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)),
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
            var printerComboBox = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Height = 28
            };
            PopulatePrinterList(printerComboBox);
            settingsStack.Children.Add(printerComboBox);

            // 份数
            settingsStack.Children.Add(new TextBlock { Text = "份数", Margin = new Thickness(0, 0, 0, 5) });
            var copiesPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var copiesTextBox = new TextBox
            {
                Text = "1",
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

            // 纸张尺寸（可选）
            settingsStack.Children.Add(new TextBlock { Text = "纸张尺寸", Margin = new Thickness(0, 0, 0, 5) });
            var paperSizeComboBox = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Height = 28
            };
            // 填充纸张尺寸选项
            foreach (var paperSize in AvailablePaperSizes)
            {
                paperSizeComboBox.Items.Add(paperSize);
            }
            paperSizeComboBox.DisplayMemberPath = "DisplayName";
            paperSizeComboBox.SelectedIndex = 0; // 默认选择A5

            // 存储当前文档引用（用于打印）
            var currentDocument = document;

            // 纸张尺寸变更时重建文档
            paperSizeComboBox.SelectionChanged += (s, e) =>
            {
                if (paperSizeComboBox.SelectedItem is PaperSizeInfo selectedSize)
                {
                    // 重建FixedDocument
                    currentDocument = BuildFixedDocument(printDto, selectedSize.Size);
                    // 更新DocumentViewer
                    docViewer.Document = currentDocument;
                }
            };

            settingsStack.Children.Add(paperSizeComboBox);

            // 方向（只读）
            settingsStack.Children.Add(new TextBlock { Text = "方向", Margin = new Thickness(0, 0, 0, 5) });
            settingsStack.Children.Add(new TextBlock
            {
                Text = "纵向",
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // 分隔线
            settingsStack.Children.Add(new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 0, 0, 20)
            });

            // 按钮区
            var printButton = new Button
            {
                Content = "打印",
                Height = 35,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Height = 35
            };

            // 打印按钮事件 - 使用当前文档（可能已因纸张尺寸变更而重建）
            printButton.Click += (s, e) =>
            {
                if (!int.TryParse(copiesTextBox.Text, out int copies) || copies < 1)
                    copies = 1;

                var selectedPrinter = printerComboBox.SelectedItem as PrinterInfo;
                ExecutePrintWithSettings(currentDocument, prescriptionId, selectedPrinter?.Name, copies);
                parentWindow.Close();
            };

            cancelButton.Click += (s, e) => parentWindow.Close();

            settingsStack.Children.Add(printButton);
            settingsStack.Children.Add(cancelButton);

            settingsBorder.Child = settingsStack;
            return settingsBorder;
        }

        /// <summary>
        /// 创建右侧预览区域（返回Border和DocumentViewer）
        /// OpenSpec: enhance-prescription-print - 使用DocumentViewer显示FixedDocument实现WYSIWYG
        /// </summary>
        private (Border previewArea, DocumentViewer docViewer) CreatePreviewAreaWithViewer(FixedDocument document)
        {
            // 使用DocumentViewer显示FixedDocument
            // DocumentViewer提供内置的缩放、导航和搜索功能
            var docViewer = new DocumentViewer
            {
                Document = document,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240))
            };

            // 设置初始缩放以适应窗口
            docViewer.FitToWidth();

            var previewBorder = new Border
            {
                Child = docViewer
            };

            return (previewBorder, docViewer);
        }

        /// <summary>
        /// 填充打印机列表
        /// OpenSpec: enhance-prescription-print
        /// </summary>
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
                        var info = new PrinterInfo
                        {
                            Name = pq.Name,
                            IsDefault = pq.Name == defaultPrinter?.Name,
                            Status = GetPrinterStatus(pq)
                        };
                        printerComboBox.Items.Add(info);

                        if (info.IsDefault)
                        {
                            printerComboBox.SelectedItem = info;
                        }
                    }
                }

                printerComboBox.DisplayMemberPath = "DisplayName";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取打印机列表失败");
            }
        }

        /// <summary>
        /// 获取打印机状态
        /// </summary>
        private static string GetPrinterStatus(PrintQueue pq)
        {
            if (pq.IsOffline) return "脱机";
            if (pq.IsBusy) return "忙";
            return "就绪";
        }

        /// <summary>
        /// 执行打印（带设置）
        /// OpenSpec: enhance-prescription-print - 使用FixedDocument打印
        /// </summary>
        private void ExecutePrintWithSettings(FixedDocument document, Guid prescriptionId, string? printerName, int copies)
        {
            try
            {
                PrintQueue? printQueue = null;

                if (!string.IsNullOrEmpty(printerName))
                {
                    printQueue = FindPrintQueue(printerName);
                }

                printQueue ??= LocalPrintServer.GetDefaultPrintQueue();

                if (printQueue == null)
                {
                    _logger.LogError("未找到可用打印机");
                    return;
                }

                // 使用FixedDocument的DocumentPaginator进行打印
                var paginator = document.DocumentPaginator;

                for (int i = 0; i < copies; i++)
                {
                    var writer = PrintQueue.CreateXpsDocumentWriter(printQueue);
                    writer.Write(paginator);
                }

                _logger.LogInformation("处方打印成功 ID: {PrescriptionId}, 份数: {Copies}", prescriptionId, copies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印失败 ID: {PrescriptionId}", prescriptionId);
            }
        }

        /// <summary>
        /// 打印机信息
        /// </summary>
        private class PrinterInfo
        {
            public string Name { get; set; } = string.Empty;
            public bool IsDefault { get; set; }
            public string Status { get; set; } = "未知";
            public string DisplayName => IsDefault ? $"{Name} (默认)" : $"{Name} - {Status}";
        }

        /// <summary>
        /// 批量打印处方
        /// </summary>
        public async Task<int> BatchPrintAsync(PrescriptionDetailDto[] prescriptions)
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
        /// OpenSpec: enhance-prescription-print - 使用FixedDocument导出
        /// </summary>
        public async Task<bool> ExportToPdfAsync(PrescriptionDetailDto prescription, string filePath)
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

                // 2. 使用FixedDocument实现WYSIWYG导出
                var document = BuildFixedDocument(printDto);

                // 3. 创建XPS文档
                using (var package = Package.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var xpsDocument = new XpsDocument(package, CompressionOption.Maximum))
                    {
                        var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                        // 使用FixedDocument的DocumentPaginator
                        writer.Write(document.DocumentPaginator);
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
        private List<PrescriptionItemPrintModel> MapPrescriptionItems(IList<PrescriptionItemDto> items)
        {
            return items.Select((item, index) => new PrescriptionItemPrintModel
            {
                SequenceNumber = index + 1,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                Unit = item.Unit,
                DecocteMethod = item.DecocteMethod
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
        /// 将PrescriptionDetailDto映射到PrescriptionPrintModel（兼容旧调用）
        /// </summary>
        private Task<PrescriptionPrintModel> MapToPrintDtoAsync(PrescriptionDetailDto prescription)
        {
            return MapToPrintDtoAsync(prescription, null, null);
        }

        /// <summary>
        /// 将PrescriptionDetailDto映射到PrescriptionPrintModel（带完整上下文）
        /// OpenSpec: print-prescription-slip
        /// </summary>
        private async Task<PrescriptionPrintModel> MapToPrintDtoAsync(PrescriptionDetailDto prescription, PatientDetailDto? patient, ConsultationInputDto? consultation)
        {
            var printDto = new PrescriptionPrintModel();

            PopulateClinicInfo(printDto);
            PopulatePatientInfo(printDto, patient);
            PopulateDiagnosisInfo(printDto, prescription, consultation);
            PopulatePrescriptionDetails(printDto, prescription);
            PopulateDoctorInfo(printDto, prescription);

            return await Task.FromResult(printDto);
        }

        /// <summary>
        /// 填充诊所信息
        /// OpenSpec: print-prescription-slip - 从IClinicSettingsService获取配置
        /// </summary>
        private void PopulateClinicInfo(PrescriptionPrintModel printDto)
        {
            var settings = _clinicSettingsService.GetSettings();
            printDto.ClinicName = settings.Name;
            printDto.ClinicAddress = settings.Address;
            printDto.ClinicPhone = settings.Phone;
            printDto.Department = settings.Department;
        }

        /// <summary>
        /// 填充患者信息
        /// OpenSpec: print-prescription-slip - 从PatientDto获取患者信息
        /// </summary>
        private static void PopulatePatientInfo(PrescriptionPrintModel printDto, PatientDetailDto? patient)
        {
            if (patient != null)
            {
                printDto.PatientName = patient.Name;
                printDto.Gender = ConvertGenderToString(patient.Gender);
                printDto.Age = CalculateAge(patient.BirthDate);
            }
            else
            {
                printDto.PatientName = "未知患者";
                printDto.Gender = string.Empty;
                printDto.Age = 0;
            }
            printDto.ConsultationDate = DateTime.Now;
        }

        /// <summary>
        /// 将性别枚举转换为中文字符串
        /// OpenSpec: print-prescription-slip
        /// </summary>
        private static string ConvertGenderToString(Gender gender)
        {
            return gender switch
            {
                Gender.Male => "男",
                Gender.Female => "女",
                _ => "未知"
            };
        }

        /// <summary>
        /// 计算年龄
        /// OpenSpec: print-prescription-slip
        /// </summary>
        private static int CalculateAge(DateTime? birthDate)
        {
            if (birthDate == null) return 0;
            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;
            return age;
        }

        /// <summary>
        /// 填充四诊信息
        /// OpenSpec: print-prescription-slip - 从ConsultationInputDto获取诊断信息
        /// OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
        /// </summary>
        private static void PopulateDiagnosisInfo(PrescriptionPrintModel printDto, PrescriptionDetailDto prescription, ConsultationInputDto? consultation)
        {
            if (consultation != null)
            {
                printDto.PresentIllness = consultation.PresentIllness;
                printDto.TongueDiagnosis = consultation.TongueDiagnosis;
                printDto.PulseDiagnosis = consultation.PulseDiagnosis;
                printDto.TCMDiagnosis = consultation.TCMDiagnosis ?? prescription.Indication;
            }
            else
            {
                printDto.PresentIllness = null;
                printDto.TongueDiagnosis = null;
                printDto.PulseDiagnosis = null;
                printDto.TCMDiagnosis = prescription.Indication;
            }
        }

        /// <summary>
        /// 填充处方详情
        /// OpenSpec: print-prescription-slip - 更新为模板格式
        /// </summary>
        private void PopulatePrescriptionDetails(PrescriptionPrintModel printDto, PrescriptionDetailDto prescription)
        {
            printDto.Items = MapPrescriptionItems(prescription.Items);
            printDto.DosageCount = prescription.DosageCount;
            printDto.Usage = prescription.Usage ?? "水煎服，日1剂，1日2次";
            printDto.SingleDosePrice = prescription.SingleDosePrice;
            printDto.TotalPrice = prescription.TotalPrice;
            // 药费 = 单剂价格 × 剂数
            printDto.MedicineFee = prescription.SingleDosePrice * prescription.DosageCount;
        }

        /// <summary>
        /// 填充医生信息和可选信息
        /// Issue #1794: 从MapToPrintDtoAsync提取
        /// </summary>
        private static void PopulateDoctorInfo(PrescriptionPrintModel printDto, PrescriptionDetailDto prescription)
        {
            // TODO: 从IUserService获取
            printDto.DoctorName = "医生姓名";
            printDto.PrescriptionDate = DateTime.Now;
            printDto.PrescriptionNumber = prescription.Id.ToString("N").Substring(0, 8).ToUpper();
            printDto.Advice = prescription.Advice;
            printDto.FormulaSource = prescription.FormulaSource;
        }

        /// <summary>
        /// 使用Builder构建FlowDocument（A5模板格式）
        /// OpenSpec: print-prescription-slip
        /// </summary>
        private FlowDocument BuildFlowDocument(PrescriptionPrintModel printDto)
        {
            var builder = new PrescriptionFlowDocumentBuilder(printDto);
            return builder.Build();
        }

        // ===== FixedDocument 方法（OpenSpec: enhance-prescription-print）=====

        /// <summary>
        /// A5纸张尺寸常量（96 DPI）
        /// 148mm x 210mm = 559px x 794px
        /// </summary>
        private static readonly Size A5PageSize = new(559, 794);

        /// <summary>
        /// A4纸张尺寸常量（96 DPI）
        /// 210mm x 297mm = 794px x 1123px
        /// </summary>
        private static readonly Size A4PageSize = new(794, 1123);

        /// <summary>
        /// 纸张尺寸信息类
        /// </summary>
        private class PaperSizeInfo
        {
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public Size Size { get; set; }
        }

        /// <summary>
        /// 可用纸张尺寸列表
        /// </summary>
        private static readonly PaperSizeInfo[] AvailablePaperSizes =
        [
            new PaperSizeInfo { Name = "A5", DisplayName = "A5 (148 × 210 mm)", Size = A5PageSize },
            new PaperSizeInfo { Name = "A4", DisplayName = "A4 (210 × 297 mm)", Size = A4PageSize }
        ];

        /// <summary>
        /// 创建FixedDocument用于预览和打印（默认A5）
        /// OpenSpec: enhance-prescription-print - WYSIWYG打印
        /// </summary>
        private FixedDocument BuildFixedDocument(PrescriptionPrintModel printDto)
        {
            return BuildFixedDocument(printDto, A5PageSize);
        }

        /// <summary>
        /// 创建FixedDocument用于预览和打印（指定纸张尺寸）
        /// OpenSpec: enhance-prescription-print - 支持A5/A4纸张选择
        /// </summary>
        private FixedDocument BuildFixedDocument(PrescriptionPrintModel printDto, Size pageSize)
        {
            var document = new FixedDocument();
            document.DocumentPaginator.PageSize = pageSize;

            // 创建页面内容
            var pageContent = new PageContent();
            var fixedPage = CreateFixedPage(printDto, pageSize);

            // 使用IAddChild接口添加页面（关键技巧）
            ((IAddChild)pageContent).AddChild(fixedPage);
            document.Pages.Add(pageContent);

            return document;
        }

        /// <summary>
        /// 将UserControl模板转换为FixedPage
        /// OpenSpec: enhance-prescription-print - 支持A5/A4纸张选择
        /// </summary>
        private FixedPage CreateFixedPage(PrescriptionPrintModel printDto, Size pageSize)
        {
            // 1. 创建模板实例并设置DataContext
            var template = new PrescriptionPrintTemplate
            {
                DataContext = printDto,
                // 动态设置模板尺寸以适应不同纸张
                Width = pageSize.Width,
                Height = pageSize.Height
            };

            // 2. 强制测量和排列
            template.Measure(pageSize);
            template.Arrange(new Rect(pageSize));
            template.UpdateLayout();

            // 3. 创建FixedPage
            var fixedPage = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = System.Windows.Media.Brushes.White
            };

            // 4. 添加模板到FixedPage
            fixedPage.Children.Add(template);
            FixedPage.SetLeft(template, 0);
            FixedPage.SetTop(template, 0);

            // 5. 完成布局
            fixedPage.Measure(pageSize);
            fixedPage.Arrange(new Rect(pageSize));
            fixedPage.UpdateLayout();

            return fixedPage;
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
