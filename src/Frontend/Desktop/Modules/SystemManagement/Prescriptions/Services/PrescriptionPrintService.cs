using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using System.IO;
using System.Printing;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Prescriptions.Services
{
    /// <summary>
    /// 处方打印服务
    /// 负责处方的打印、预览和PDF导出功能
    /// </summary>
    public class PrescriptionPrintService : IAdvancedPrescriptionPrintService
    {
        private readonly ILogger<PrescriptionPrintService> _logger;
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPatientService _patientService;

        // 打印设置
        private readonly double _pageWidth = 21.0 * 96; // A4纸宽度 (21cm)
        private readonly double _pageHeight = 29.7 * 96; // A4纸高度 (29.7cm)
        private readonly Thickness _pageMargin = new Thickness(96); // 1英寸边距

        public PrescriptionPrintService(
            ILogger<PrescriptionPrintService> logger,
            IPrescriptionService prescriptionService,
            IPatientService patientService)
        {
            _logger = logger;
            _prescriptionService = prescriptionService;
            _patientService = patientService;
        }

        #region 打印功能

        /// <summary>
        /// 打印单个处方
        /// </summary>
        public async Task<bool> PrintPrescription(PrescriptionInfo prescription)
        {
            try
            {
                _logger.LogInformation($"开始打印处方 - ID: {prescription.Id}");

                // 创建打印文档
                var document = await CreatePrescriptionDocument(prescription);
                if (document == null)
                {
                    _logger.LogError("创建打印文档失败");
                    return false;
                }

                // 显示打印对话框
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // 设置文档分页器
                    var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                    // 执行打印
                    printDialog.PrintDocument(paginator, $"处方_{prescription.PrescriptionNumber}");
                    
                    _logger.LogInformation($"处方打印成功 - {prescription.PrescriptionNumber}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印处方失败");
                return false;
            }
        }

        /// <summary>
        /// 批量打印处方
        /// </summary>
        public async Task<int> BatchPrintPrescriptions(IEnumerable<PrescriptionInfo> prescriptions)
        {
            try
            {
                _logger.LogInformation($"开始批量打印 - 数量: {prescriptions.Count()}");

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() != true)
                {
                    return 0;
                }

                int successCount = 0;
                foreach (var prescription in prescriptions)
                {
                    try
                    {
                        var document = await CreatePrescriptionDocument(prescription);
                        if (document != null)
                        {
                            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                            paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
                            
                            printDialog.PrintDocument(paginator, $"处方_{prescription.PrescriptionNumber}");
                            successCount++;
                            
                            // 延迟避免打印队列拥堵
                            await Task.Delay(500);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"打印处方失败 - ID: {prescription.Id}");
                    }
                }

                _logger.LogInformation($"批量打印完成 - 成功: {successCount}/{prescriptions.Count()}");
                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量打印失败");
                return 0;
            }
        }

        #endregion

        #region 预览功能

        /// <summary>
        /// 生成打印预览
        /// </summary>
        public async Task<FlowDocument?> GeneratePrintPreview(PrescriptionInfo prescription)
        {
            try
            {
                _logger.LogInformation($"生成打印预览 - ID: {prescription.Id}");
                return await CreatePrescriptionDocument(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成打印预览失败");
                return null;
            }
        }

        #endregion

        #region 导出功能

        /// <summary>
        /// 导出为PDF
        /// </summary>
        public async Task<bool> ExportToPdf(PrescriptionInfo prescription, string filePath)
        {
            try
            {
                _logger.LogInformation($"导出PDF - 处方: {prescription.PrescriptionNumber}, 路径: {filePath}");

                var document = await CreatePrescriptionDocument(prescription);
                if (document == null)
                {
                    return false;
                }

                // 创建XPS文档（中间格式）
                var tempXpsPath = Path.GetTempFileName() + ".xps";
                using (var xpsDocument = new XpsDocument(tempXpsPath, FileAccess.Write))
                {
                    var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                    writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);
                }

                // TODO: 将XPS转换为PDF（需要第三方库如PdfSharp）
                // 这里暂时保存为XPS格式
                File.Move(tempXpsPath, filePath.Replace(".pdf", ".xps"), true);

                _logger.LogInformation("PDF导出成功（暂时为XPS格式）");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出PDF失败");
                return false;
            }
        }

        #endregion

        #region 文档创建

        /// <summary>
        /// 创建处方打印文档
        /// </summary>
        private async Task<FlowDocument?> CreatePrescriptionDocument(PrescriptionInfo prescription)
        {
            try
            {
                // 获取完整的处方数据
                var fullPrescription = await LoadFullPrescriptionData(prescription);
                if (fullPrescription == null)
                {
                    return null;
                }

                // 创建FlowDocument
                var document = new FlowDocument
                {
                    PageWidth = _pageWidth,
                    PageHeight = _pageHeight,
                    PagePadding = _pageMargin,
                    ColumnWidth = double.PositiveInfinity,
                    FontFamily = new FontFamily("Microsoft YaHei"),
                    FontSize = 14
                };

                // 添加诊所标题
                AddClinicHeader(document);

                // 添加处方标题
                AddPrescriptionTitle(document);

                // 添加处方信息
                AddPrescriptionInfo(document, fullPrescription);

                // 添加处方项目表格
                AddPrescriptionItems(document, fullPrescription);

                // 添加用法说明
                AddUsageInstructions(document, fullPrescription);

                // 添加底部签名区
                AddSignatureArea(document, fullPrescription);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方文档失败");
                return null;
            }
        }

        /// <summary>
        /// 添加诊所标题
        /// </summary>
        private void AddClinicHeader(FlowDocument document)
        {
            var header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            header.Inlines.Add(new Run("凌隐宝堂中医诊所")
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold
            });

            header.Inlines.Add(new LineBreak());
            header.Inlines.Add(new Run("Traditional Chinese Medicine Clinic")
            {
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray
            });

            header.Inlines.Add(new LineBreak());
            header.Inlines.Add(new Run("地址：XX市XX区XX路XX号 | 电话：XXX-XXXXXXXX")
            {
                FontSize = 10,
                Foreground = Brushes.Gray
            });

            document.Blocks.Add(header);

            // 添加分隔线
            document.Blocks.Add(new BlockUIContainer(
                new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Margin = new Thickness(0, 10, 0, 10)
                }));
        }

        /// <summary>
        /// 添加处方标题
        /// </summary>
        private void AddPrescriptionTitle(FlowDocument document)
        {
            var title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 20)
            };

            title.Inlines.Add(new Run("中 医 处 方")
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold
            });

            document.Blocks.Add(title);
        }

        /// <summary>
        /// 添加处方信息
        /// </summary>
        private void AddPrescriptionInfo(FlowDocument document, PrescriptionInfo prescription)
        {
            var infoTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // 定义列
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var rowGroup = new TableRowGroup();

            // 第一行：处方编号、日期、医生
            var row1 = new TableRow();
            row1.Cells.Add(CreateInfoCell($"处方编号：{prescription.PrescriptionNumber}"));
            row1.Cells.Add(CreateInfoCell($"开方日期：{prescription.CreateTime:yyyy年MM月dd日}"));
            row1.Cells.Add(CreateInfoCell($"开方医生：{prescription.DoctorName}"));
            rowGroup.Rows.Add(row1);

            // 第二行：患者信息
            var row2 = new TableRow();
            row2.Cells.Add(CreateInfoCell($"患者姓名：{prescription.PatientName}"));
            row2.Cells.Add(CreateInfoCell($"性别：{prescription.PatientInfo}"));
            row2.Cells.Add(CreateInfoCell($"年龄：--"));
            rowGroup.Rows.Add(row2);

            // 第三行：诊断
            var row3 = new TableRow();
            var diagnosisCell = CreateInfoCell($"诊断：{prescription.Diagnosis}");
            diagnosisCell.ColumnSpan = 3;
            row3.Cells.Add(diagnosisCell);
            rowGroup.Rows.Add(row3);

            infoTable.RowGroups.Add(rowGroup);
            document.Blocks.Add(infoTable);
        }

        /// <summary>
        /// 添加处方项目表格
        /// </summary>
        private void AddPrescriptionItems(FlowDocument document, PrescriptionInfo prescription)
        {
            var itemsSection = new Section();
            
            // 添加"处方"标签
            itemsSection.Blocks.Add(new Paragraph(new Run("【处方】")
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            }));

            // 创建药材表格
            var itemsTable = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 10, 0, 20)
            };

            // 定义列
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(50) });  // 序号
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) }); // 药材名称
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // 数量
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(80) });  // 单位
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 单价
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 小计

            var rowGroup = new TableRowGroup();

            // 表头
            var headerRow = new TableRow { Background = Brushes.LightGray };
            headerRow.Cells.Add(CreateHeaderCell("序号"));
            headerRow.Cells.Add(CreateHeaderCell("药材名称"));
            headerRow.Cells.Add(CreateHeaderCell("数量"));
            headerRow.Cells.Add(CreateHeaderCell("单位"));
            headerRow.Cells.Add(CreateHeaderCell("单价(元)"));
            headerRow.Cells.Add(CreateHeaderCell("小计(元)"));
            rowGroup.Rows.Add(headerRow);

            // 药材项目
            int index = 1;
            foreach (var item in prescription.Items)
            {
                var itemRow = new TableRow();
                itemRow.Cells.Add(CreateItemCell(index.ToString()));
                itemRow.Cells.Add(CreateItemCell(item.HerbName));
                itemRow.Cells.Add(CreateItemCell(item.Quantity.ToString("F1")));
                itemRow.Cells.Add(CreateItemCell(item.Unit));
                itemRow.Cells.Add(CreateItemCell(item.Price.ToString("F2")));
                itemRow.Cells.Add(CreateItemCell(item.Subtotal.ToString("F2")));
                rowGroup.Rows.Add(itemRow);
                index++;
            }

            // 合计行
            var totalRow = new TableRow { Background = Brushes.LightYellow };
            var totalLabelCell = CreateItemCell("合计");
            totalLabelCell.ColumnSpan = 5;
            totalLabelCell.TextAlignment = TextAlignment.Right;
            totalLabelCell.Padding = new Thickness(5);
            totalRow.Cells.Add(totalLabelCell);
            
            var totalAmountCell = CreateItemCell($"¥{prescription.TotalPrice:F2}");
            totalAmountCell.FontWeight = FontWeights.Bold;
            totalRow.Cells.Add(totalAmountCell);
            rowGroup.Rows.Add(totalRow);

            itemsTable.RowGroups.Add(rowGroup);
            itemsSection.Blocks.Add(itemsTable);
            
            document.Blocks.Add(itemsSection);
        }

        /// <summary>
        /// 添加用法说明
        /// </summary>
        private void AddUsageInstructions(FlowDocument document, PrescriptionInfo prescription)
        {
            var usageSection = new Section { Margin = new Thickness(0, 10, 0, 20) };

            // 剂数和用法
            var dosageInfo = new Paragraph();
            dosageInfo.Inlines.Add(new Run($"【剂数】{prescription.DosageCount} 剂")
            {
                FontWeight = FontWeights.Bold
            });
            dosageInfo.Inlines.Add(new Run("    "));
            dosageInfo.Inlines.Add(new Run($"【用法】{prescription.Usage ?? "水煎服，每日一剂，分两次服用"}")
            {
                FontWeight = FontWeights.Bold
            });
            usageSection.Blocks.Add(dosageInfo);

            // 医嘱
            if (!string.IsNullOrEmpty(prescription.Remark))
            {
                var remarkPara = new Paragraph();
                remarkPara.Inlines.Add(new Run("【医嘱】")
                {
                    FontWeight = FontWeights.Bold
                });
                remarkPara.Inlines.Add(new Run(prescription.Remark));
                usageSection.Blocks.Add(remarkPara);
            }

            // 注意事项
            var notePara = new Paragraph { Margin = new Thickness(0, 10, 0, 0) };
            notePara.Inlines.Add(new Run("【注意事项】")
            {
                FontWeight = FontWeights.Bold
            });
            notePara.Inlines.Add(new LineBreak());
            notePara.Inlines.Add(new Run("1. 请遵医嘱服用，不可自行增减药量\n"));
            notePara.Inlines.Add(new Run("2. 服药期间忌食生冷、辛辣、油腻食物\n"));
            notePara.Inlines.Add(new Run("3. 如有不适，请及时就医"));
            usageSection.Blocks.Add(notePara);

            document.Blocks.Add(usageSection);
        }

        /// <summary>
        /// 添加签名区
        /// </summary>
        private void AddSignatureArea(FlowDocument document, PrescriptionInfo prescription)
        {
            // 添加分隔线
            document.Blocks.Add(new BlockUIContainer(
                new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0, 0.5, 0, 0),
                    Margin = new Thickness(0, 20, 0, 20)
                }));

            var signatureTable = new Table { CellSpacing = 0 };
            signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var rowGroup = new TableRowGroup();
            var row = new TableRow();

            // 医师签名
            var doctorCell = new TableCell();
            doctorCell.Blocks.Add(new Paragraph(new Run($"医师签名：{prescription.DoctorName}")
            {
                FontSize = 12
            }));
            row.Cells.Add(doctorCell);

            // 审核药师
            var pharmacistCell = new TableCell();
            pharmacistCell.Blocks.Add(new Paragraph(new Run("审核药师：__________")
            {
                FontSize = 12
            }));
            row.Cells.Add(pharmacistCell);

            // 调配药师
            var dispenserCell = new TableCell();
            dispenserCell.Blocks.Add(new Paragraph(new Run("调配药师：__________")
            {
                FontSize = 12
            }));
            row.Cells.Add(dispenserCell);

            rowGroup.Rows.Add(row);
            signatureTable.RowGroups.Add(rowGroup);
            document.Blocks.Add(signatureTable);

            // 添加打印时间
            var printTime = new Paragraph(new Run($"打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            {
                FontSize = 10,
                Foreground = Brushes.Gray
            })
            {
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            document.Blocks.Add(printTime);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 加载完整的处方数据
        /// </summary>
        private async Task<PrescriptionInfo?> LoadFullPrescriptionData(PrescriptionInfo prescription)
        {
            try
            {
                // 如果缺少详细信息，从服务加载
                if (prescription.Items == null || !prescription.Items.Any())
                {
                    var result = await _prescriptionService.GetByIdAsync(prescription.Id);
                    if (result.IsSuccess && result.Data != null)
                    {
                        // TODO: 映射完整数据
                        return prescription;
                    }
                }

                return prescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载处方数据失败");
                return null;
            }
        }

        /// <summary>
        /// 创建信息单元格
        /// </summary>
        private TableCell CreateInfoCell(string text)
        {
            var cell = new TableCell
            {
                Padding = new Thickness(5),
                BorderBrush = Brushes.Transparent
            };
            cell.Blocks.Add(new Paragraph(new Run(text)) { FontSize = 12 });
            return cell;
        }

        /// <summary>
        /// 创建表头单元格
        /// </summary>
        private TableCell CreateHeaderCell(string text)
        {
            var cell = new TableCell
            {
                Padding = new Thickness(5),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5),
                TextAlignment = TextAlignment.Center
            };
            cell.Blocks.Add(new Paragraph(new Run(text)
            {
                FontWeight = FontWeights.Bold
            }));
            return cell;
        }

        /// <summary>
        /// 创建项目单元格
        /// </summary>
        private TableCell CreateItemCell(string text)
        {
            var cell = new TableCell
            {
                Padding = new Thickness(5),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5),
                TextAlignment = TextAlignment.Center
            };
            cell.Blocks.Add(new Paragraph(new Run(text)));
            return cell;
        }

        #endregion
    }

    #region 接口定义

    /// <summary>
    /// 高级处方打印服务接口
    /// </summary>
    public interface IAdvancedPrescriptionPrintService
    {
        Task<bool> PrintPrescription(PrescriptionInfo prescription);
        Task<int> BatchPrintPrescriptions(IEnumerable<PrescriptionInfo> prescriptions);
        Task<FlowDocument?> GeneratePrintPreview(PrescriptionInfo prescription);
        Task<bool> ExportToPdf(PrescriptionInfo prescription, string filePath);
    }

    #endregion
}