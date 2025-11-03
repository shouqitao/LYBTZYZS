using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using LYBT.Desktop.Prescriptions.Models;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 处方FlowDocument构建器 - 使用Builder模式
    /// Issue #1379: [PRINT-2] 实现标准处方模板
    /// </summary>
    public class PrescriptionFlowDocumentBuilder
    {
        private readonly PrescriptionPrintDto _prescription;
        private readonly FlowDocument _document;

        // 字体定义
        private static readonly FontFamily DefaultFont = new FontFamily("SimSun"); // 宋体
        private static readonly double DefaultFontSize = 12;
        private static readonly double HeaderFontSize = 18;
        private static readonly double SubHeaderFontSize = 14;

        // 颜色定义
        private static readonly Brush BorderBrush = Brushes.Black;
        private static readonly double BorderThickness = 1.0;

        public PrescriptionFlowDocumentBuilder(PrescriptionPrintDto prescription)
        {
            _prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));

            // 初始化FlowDocument（A4纸张）
            _document = new FlowDocument
            {
                PageWidth = 210 * 96 / 25.4, // A4宽度：210mm转像素（96 DPI）
                PageHeight = 297 * 96 / 25.4, // A4高度：297mm转像素
                PagePadding = new Thickness(40), // 边距：约10mm
                FontFamily = DefaultFont,
                FontSize = DefaultFontSize,
                LineHeight = 20
            };
        }

        /// <summary>
        /// 添加诊所抬头
        /// </summary>
        public PrescriptionFlowDocumentBuilder AddHeader()
        {
            // 诊所名称 - 居中加粗
            var clinicParagraph = new Paragraph(new Run(_prescription.ClinicName))
            {
                FontSize = HeaderFontSize,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            _document.Blocks.Add(clinicParagraph);

            // "中医门诊处方笺"标题
            var titleParagraph = new Paragraph(new Run("中医门诊处方笺"))
            {
                FontSize = SubHeaderFontSize,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            _document.Blocks.Add(titleParagraph);

            // 诊所地址和电话
            if (!string.IsNullOrEmpty(_prescription.ClinicAddress) || !string.IsNullOrEmpty(_prescription.ClinicPhone))
            {
                var infoText = string.Join(" | ",
                    new[] { _prescription.ClinicAddress, _prescription.ClinicPhone }
                    .Where(s => !string.IsNullOrEmpty(s)));

                var infoParagraph = new Paragraph(new Run(infoText))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                _document.Blocks.Add(infoParagraph);
            }

            // 分隔线
            AddSeparatorLine();

            return this;
        }

        /// <summary>
        /// 添加患者信息
        /// </summary>
        public PrescriptionFlowDocumentBuilder AddPatientInfo()
        {
            var section = new Section
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            // Issue #1551: 第一行显示处方编号（如果有）
            if (!string.IsNullOrEmpty(_prescription.PrescriptionNumber))
            {
                var prescriptionNumberParagraph = new Paragraph();
                prescriptionNumberParagraph.Inlines.Add(new Run("处方编号：") { FontWeight = FontWeights.Bold });
                prescriptionNumberParagraph.Inlines.Add(new Run(_prescription.PrescriptionNumber) { Foreground = Brushes.DarkBlue });
                prescriptionNumberParagraph.Margin = new Thickness(0, 0, 0, 5);
                section.Blocks.Add(prescriptionNumberParagraph);
            }

            var paragraph = new Paragraph();

            // 姓名
            paragraph.Inlines.Add(new Run("姓名：") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run($"{_prescription.PatientName}    "));

            // 性别
            paragraph.Inlines.Add(new Run("性别：") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run($"{_prescription.Gender}    "));

            // 年龄
            paragraph.Inlines.Add(new Run("年龄：") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run($"{_prescription.Age}岁    "));

            // 日期
            paragraph.Inlines.Add(new Run("就诊日期：") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run(_prescription.ConsultationDate.ToString("yyyy年MM月dd日")));

            section.Blocks.Add(paragraph);
            _document.Blocks.Add(section);

            AddSeparatorLine();

            return this;
        }

        /// <summary>
        /// 添加四诊信息
        /// </summary>
        public PrescriptionFlowDocumentBuilder AddFourDiagnostics()
        {
            var section = new Section
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            // 四诊信息标题
            var title = new Paragraph(new Run("四诊信息："))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            section.Blocks.Add(title);

            // 望诊
            if (!string.IsNullOrEmpty(_prescription.Inspection))
            {
                AddDiagnosticItem(section, "望", _prescription.Inspection);
            }

            // 闻诊
            if (!string.IsNullOrEmpty(_prescription.AuscultationOlfaction))
            {
                AddDiagnosticItem(section, "闻", _prescription.AuscultationOlfaction);
            }

            // 问诊
            if (!string.IsNullOrEmpty(_prescription.Inquiry))
            {
                AddDiagnosticItem(section, "问", _prescription.Inquiry);
            }

            // 切诊
            if (!string.IsNullOrEmpty(_prescription.Palpation))
            {
                AddDiagnosticItem(section, "切", _prescription.Palpation);
            }

            // 中医诊断
            if (!string.IsNullOrEmpty(_prescription.TCMDiagnosis))
            {
                AddDiagnosticItem(section, "中医诊断", _prescription.TCMDiagnosis);
            }

            // 治疗原则
            if (!string.IsNullOrEmpty(_prescription.TreatmentPrinciple))
            {
                AddDiagnosticItem(section, "治疗原则", _prescription.TreatmentPrinciple);
            }

            _document.Blocks.Add(section);
            AddSeparatorLine();

            return this;
        }

        /// <summary>
        /// 添加处方药材表格
        /// </summary>
        public PrescriptionFlowDocumentBuilder AddPrescriptionTable()
        {
            var section = new Section
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            // 处方内容标题
            var title = new Paragraph(new Run("处方内容："))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            section.Blocks.Add(title);

            // 创建表格
            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(BorderThickness)
            };

            // 定义列
            table.Columns.Add(new TableColumn { Width = new GridLength(50) }); // 序号
            table.Columns.Add(new TableColumn { Width = new GridLength(200) }); // 药材名
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // 剂量
            table.Columns.Add(new TableColumn { Width = new GridLength(60) }); // 单位

            // 表头
            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow { FontWeight = FontWeights.Bold, Background = Brushes.LightGray };

            headerRow.Cells.Add(CreateTableCell("序号", TextAlignment.Center));
            headerRow.Cells.Add(CreateTableCell("药材名称", TextAlignment.Center));
            headerRow.Cells.Add(CreateTableCell("剂量", TextAlignment.Center));
            headerRow.Cells.Add(CreateTableCell("单位", TextAlignment.Center));

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // 数据行
            var dataGroup = new TableRowGroup();

            foreach (var item in _prescription.Items.OrderBy(i => i.SequenceNumber))
            {
                var dataRow = new TableRow();

                dataRow.Cells.Add(CreateTableCell(item.SequenceNumber.ToString(), TextAlignment.Center));
                dataRow.Cells.Add(CreateTableCell(item.HerbName, TextAlignment.Left));
                dataRow.Cells.Add(CreateTableCell(item.Quantity.ToString("0.##"), TextAlignment.Right));
                dataRow.Cells.Add(CreateTableCell(item.Unit, TextAlignment.Center));

                dataGroup.Rows.Add(dataRow);
            }

            table.RowGroups.Add(dataGroup);
            section.Blocks.Add(table);

            _document.Blocks.Add(section);

            return this;
        }

        /// <summary>
        /// 添加用法说明
        /// </summary>
        public PrescriptionFlowDocumentBuilder AddUsageInstructions()
        {
            var section = new Section
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            var paragraph = new Paragraph();

            // 剂数
            paragraph.Inlines.Add(new Run("剂数：") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run($"{_prescription.DosageCount} 剂    "));

            // 用法
            paragraph.Inlines.Add(new Run("用法：") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run(_prescription.Usage));

            section.Blocks.Add(paragraph);

            // 医嘱（如果有）
            if (!string.IsNullOrEmpty(_prescription.Advice))
            {
                var adviceParagraph = new Paragraph();
                adviceParagraph.Inlines.Add(new Run("医嘱：") { FontWeight = FontWeights.Bold });
                adviceParagraph.Inlines.Add(new Run(_prescription.Advice));
                adviceParagraph.Margin = new Thickness(0, 5, 0, 0);
                section.Blocks.Add(adviceParagraph);
            }

            _document.Blocks.Add(section);
            AddSeparatorLine();

            return this;
        }

        /// <summary>
        /// 添加价格信息
        /// </summary>
        public PrescriptionFlowDocumentBuilder AddPriceInfo()
        {
            var section = new Section
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            var title = new Paragraph(new Run("费用信息："))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            section.Blocks.Add(title);

            var paragraph = new Paragraph();

            // 单剂价格
            paragraph.Inlines.Add(new Run("单剂价格："));
            paragraph.Inlines.Add(new Run($"¥{_prescription.SingleDosePrice:F2}    ") { FontWeight = FontWeights.Bold });

            // 总价
            paragraph.Inlines.Add(new Run("总价："));
            paragraph.Inlines.Add(new Run($"¥{_prescription.TotalPrice:F2}")
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.Red
            });

            section.Blocks.Add(paragraph);
            _document.Blocks.Add(section);

            AddSeparatorLine();

            return this;
        }

        /// <summary>
        /// 添加医生签名
        /// </summary>
        public PrescriptionFlowDocumentBuilder AddSignature()
        {
            var section = new Section
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            var paragraph = new Paragraph();

            // 医生签名
            paragraph.Inlines.Add(new Run("医生签名：") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run($"_________________ ({_prescription.DoctorName})    "));

            // 日期
            paragraph.Inlines.Add(new Run("日期：") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run(_prescription.PrescriptionDate.ToString("yyyy年MM月dd日")));

            section.Blocks.Add(paragraph);

            // 处方编号（如果有）
            if (!string.IsNullOrEmpty(_prescription.PrescriptionNumber))
            {
                var numberParagraph = new Paragraph(new Run($"处方编号：{_prescription.PrescriptionNumber}"))
                {
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                section.Blocks.Add(numberParagraph);
            }

            _document.Blocks.Add(section);

            return this;
        }

        /// <summary>
        /// 构建最终的FlowDocument
        /// </summary>
        public FlowDocument Build()
        {
            return _document;
        }

        // ===== 私有辅助方法 =====

        /// <summary>
        /// 添加分隔线
        /// </summary>
        private void AddSeparatorLine()
        {
            var separator = new Paragraph(new Run(new string('─', 80)))
            {
                FontSize = 10,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 5)
            };
            _document.Blocks.Add(separator);
        }

        /// <summary>
        /// 添加四诊项目
        /// </summary>
        private void AddDiagnosticItem(Section section, string label, string content)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 2, 0, 2),
                TextIndent = 20
            };

            paragraph.Inlines.Add(new Run($"{label}：") { FontWeight = FontWeights.SemiBold });
            paragraph.Inlines.Add(new Run(content));

            section.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 创建表格单元格
        /// </summary>
        private TableCell CreateTableCell(string text, TextAlignment alignment)
        {
            var cell = new TableCell(new Paragraph(new Run(text))
            {
                Margin = new Thickness(5),
                TextAlignment = alignment
            })
            {
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(BorderThickness),
                Padding = new Thickness(5)
            };

            return cell;
        }
    }
}
