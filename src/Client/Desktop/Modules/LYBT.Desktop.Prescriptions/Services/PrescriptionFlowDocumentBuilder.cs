using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using LYBT.Desktop.Prescriptions.Models;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 处方FlowDocument构建器 - 基于普通处方模板（A5纸张）
    /// OpenSpec: print-prescription-slip
    /// </summary>
    public class PrescriptionFlowDocumentBuilder
    {
        private readonly PrescriptionPrintDto _prescription;
        private readonly FlowDocument _document;

        // 字体定义 - 使用华文楷体
        private static readonly FontFamily KaiTiFont = new FontFamily("STKaiti, 华文楷体, KaiTi, SimKai, Microsoft YaHei");
        private static readonly double TitleFontSize = 16; // 标题字号（加大）
        private static readonly double ContentFontSize = 11; // 正文字号
        private static readonly double RpFontSize = 12; // Rp.字号

        // 行高定义（基于模板的spacing设置）
        private static readonly double LineHeight440 = 20; // line="440" exact
        private static readonly double LineHeight400 = 18; // line="400" exact
        private static readonly double LineHeight360 = 16; // line="360" auto

        // 每行显示的药材列数
        private const int HerbsPerRow = 4;

        public PrescriptionFlowDocumentBuilder(PrescriptionPrintDto prescription)
        {
            _prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));

            // 初始化FlowDocument（A5纸张：148mm x 210mm）
            // 96 DPI: 1mm = 96/25.4 = 3.78像素
            _document = new FlowDocument
            {
                PageWidth = 559,  // A5宽度：148mm * 3.78
                PageHeight = 794, // A5高度：210mm * 3.78
                PagePadding = new Thickness(
                    57,  // left: 15mm
                    38,  // top: 10mm
                    57,  // right: 15mm
                    38   // bottom: 10mm
                ),
                FontFamily = KaiTiFont,
                FontSize = ContentFontSize,
                ColumnWidth = double.MaxValue, // 单列布局
                TextAlignment = TextAlignment.Justify // 两端对齐
            };
        }

        /// <summary>
        /// 构建完整的处方文档（按模板顺序）
        /// OpenSpec: enhance-prescription-print - 固定头部/底部布局
        /// </summary>
        public FlowDocument Build()
        {
            // === 头部区域（固定） ===
            AddHeader();
            AddPatientInfoLine1();
            AddPatientInfoLine2();
            AddAddress();
            AddDiagnosis();
            AddSymptoms();

            // === 中间区域（内容区） ===
            AddPrescriptionContent();
            AddUsageAndDosage();

            // === 弹性空白区 - 将底部推至页面底端 ===
            AddFlexibleSpace();

            // === 底部区域（固定在页面底部） ===
            AddSeparatorLine();
            AddSignatures();
            AddFees();

            return _document;
        }

        /// <summary>
        /// 添加内容区与底部之间的间距
        /// OpenSpec: enhance-prescription-print - 简化版本
        /// </summary>
        private void AddFlexibleSpace()
        {
            // 添加固定间距，确保内容区与底部分隔
            // 后续可根据实际打印效果调整
            var spacerParagraph = new Paragraph
            {
                Margin = new Thickness(0, 15, 0, 0)
            };
            _document.Blocks.Add(spacerParagraph);
        }

        /// <summary>
        /// 添加标题：诊所名称 + 普通处方笺
        /// </summary>
        private void AddHeader()
        {
            // 诊所名称 + "普通处方笺" 居中加粗
            var headerText = $"{_prescription.ClinicName}普通处方笺";
            var paragraph = new Paragraph(new Run(headerText))
            {
                FontSize = TitleFontSize,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            _document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 第一行：姓名/性别/年龄/时间
        /// OpenSpec: enhance-prescription-print - 修复字段间距
        /// </summary>
        private void AddPatientInfoLine1()
        {
            var paragraph = new Paragraph
            {
                LineHeight = LineHeight440,
                Margin = new Thickness(0, 0, 0, 2)
            };

            // 姓名
            paragraph.Inlines.Add(new Run("姓名："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.PatientName, 8));
            paragraph.Inlines.Add(new Run("  ")); // 字段间空格

            // 性别
            paragraph.Inlines.Add(new Run("性别："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.Gender, 4));
            paragraph.Inlines.Add(new Run("  "));

            // 年龄
            paragraph.Inlines.Add(new Run("年龄："));
            var ageText = _prescription.Age > 0 ? $"{_prescription.Age}岁" : "    ";
            paragraph.Inlines.Add(CreateUnderlinedValue(ageText, 5));
            paragraph.Inlines.Add(new Run("  "));

            // 时间
            paragraph.Inlines.Add(new Run("时间："));
            var dateText = _prescription.ConsultationDate.ToString("yyyy年M月d日");
            paragraph.Inlines.Add(CreateUnderlinedValue(dateText, 0));

            _document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 第二行：门诊号/科别/电话
        /// OpenSpec: enhance-prescription-print - 修复字段间距
        /// </summary>
        private void AddPatientInfoLine2()
        {
            var paragraph = new Paragraph
            {
                LineHeight = LineHeight440,
                Margin = new Thickness(0, 0, 0, 2)
            };

            // 门诊号
            paragraph.Inlines.Add(new Run("门诊号："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.OutpatientNumber ?? "", 10));
            paragraph.Inlines.Add(new Run("  "));

            // 科别
            paragraph.Inlines.Add(new Run("科别："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.Department, 8));
            paragraph.Inlines.Add(new Run("  "));

            // 电话
            paragraph.Inlines.Add(new Run("电话："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.PatientPhone ?? "", 0));

            _document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 住址行
        /// </summary>
        private void AddAddress()
        {
            var paragraph = new Paragraph
            {
                LineHeight = LineHeight440,
                Margin = new Thickness(0, 0, 0, 0)
            };

            paragraph.Inlines.Add(new Run("住址："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.PatientAddress ?? "", 0));

            _document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 诊断行
        /// </summary>
        private void AddDiagnosis()
        {
            var paragraph = new Paragraph
            {
                LineHeight = LineHeight440,
                Margin = new Thickness(0, 0, 0, 0)
            };

            paragraph.Inlines.Add(new Run("诊断："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.TCMDiagnosis ?? "", 0));

            _document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 诊见行（症状）
        /// </summary>
        private void AddSymptoms()
        {
            var paragraph = new Paragraph
            {
                LineHeight = LineHeight440,
                Margin = new Thickness(0, 0, 0, 0)
            };

            paragraph.Inlines.Add(new Run("诊见："));
            // 合并四诊信息作为诊见
            var symptoms = _prescription.Symptoms;
            if (string.IsNullOrEmpty(symptoms))
            {
                // 如果没有专门的症状字段，尝试合并四诊信息
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(_prescription.Inspection)) parts.Add(_prescription.Inspection);
                if (!string.IsNullOrEmpty(_prescription.Inquiry)) parts.Add(_prescription.Inquiry);
                if (!string.IsNullOrEmpty(_prescription.Palpation)) parts.Add(_prescription.Palpation);
                symptoms = string.Join("，", parts);
            }
            paragraph.Inlines.Add(CreateUnderlinedValue(symptoms ?? "", 0));

            _document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 处方内容：Rp. + 药材（4列格式）
        /// </summary>
        private void AddPrescriptionContent()
        {
            // Rp. 标记
            var rpParagraph = new Paragraph(new Run("Rp."))
            {
                FontSize = RpFontSize,
                FontWeight = FontWeights.Bold,
                LineHeight = LineHeight360,
                Margin = new Thickness(0, 5, 0, 3)
            };
            _document.Blocks.Add(rpParagraph);

            // 药材列表（4列格式）
            var sortedItems = _prescription.Items.OrderBy(i => i.SequenceNumber).ToList();

            for (int rowStart = 0; rowStart < sortedItems.Count; rowStart += HerbsPerRow)
            {
                var rowParagraph = new Paragraph
                {
                    LineHeight = LineHeight400,
                    Margin = new Thickness(0, 0, 0, 0)
                };

                for (int col = 0; col < HerbsPerRow && rowStart + col < sortedItems.Count; col++)
                {
                    var item = sortedItems[rowStart + col];
                    // 格式化为 "药名+剂量+单位"，如 "黄芪10g"
                    var herbText = $"{item.HerbName}{item.Quantity:0.##}{item.Unit}";

                    // 固定宽度对齐（使用空格填充）
                    var paddedText = herbText.PadRight(12, '\u3000'); // 使用全角空格填充
                    if (col < HerbsPerRow - 1 && rowStart + col < sortedItems.Count - 1)
                    {
                        paddedText = herbText.PadRight(10);
                    }
                    else
                    {
                        paddedText = herbText;
                    }

                    rowParagraph.Inlines.Add(new Run(paddedText + "     "));
                }

                _document.Blocks.Add(rowParagraph);
            }
        }

        /// <summary>
        /// 用法：X剂，水煎服，日1剂，1日2次
        /// </summary>
        private void AddUsageAndDosage()
        {
            // 空行
            _document.Blocks.Add(new Paragraph { Margin = new Thickness(0, 5, 0, 0) });

            var paragraph = new Paragraph
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 0)
            };

            // 格式：X剂，水煎服，日1剂，1日2次
            var usageText = $"{_prescription.DosageCount}剂，{_prescription.Usage}";
            paragraph.Inlines.Add(new Run(usageText));

            _document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 分隔线
        /// </summary>
        private void AddSeparatorLine()
        {
            // 使用实线分隔
            var line = new Paragraph
            {
                Margin = new Thickness(0, 10, 0, 5),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0, 0, 0, 1.5)
            };
            _document.Blocks.Add(line);
        }

        /// <summary>
        /// 签名区：医师签字/审核/调配
        /// OpenSpec: enhance-prescription-print - 修复字段间距
        /// </summary>
        private void AddSignatures()
        {
            var paragraph = new Paragraph
            {
                LineHeight = LineHeight360,
                Margin = new Thickness(0, 5, 0, 2)
            };

            // 医师签字
            paragraph.Inlines.Add(new Run("医师签字："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.DoctorName, 8));
            paragraph.Inlines.Add(new Run("    "));

            // 审核
            paragraph.Inlines.Add(new Run("审核："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.Reviewer ?? "", 8));
            paragraph.Inlines.Add(new Run("    "));

            // 调配
            paragraph.Inlines.Add(new Run("调配："));
            paragraph.Inlines.Add(CreateUnderlinedValue(_prescription.Dispenser ?? "", 8));

            _document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 费用区：诊疗费/药费/治疗费/合计
        /// OpenSpec: enhance-prescription-print - 修复字段间距
        /// </summary>
        private void AddFees()
        {
            var paragraph = new Paragraph
            {
                LineHeight = LineHeight360,
                Margin = new Thickness(0, 0, 0, 0)
            };

            // 诊疗费
            paragraph.Inlines.Add(new Run("诊疗费："));
            var consultFee = _prescription.ConsultationFee > 0 ? $"{_prescription.ConsultationFee:F0}" : "";
            paragraph.Inlines.Add(CreateUnderlinedValue(consultFee, 5));
            paragraph.Inlines.Add(new Run("  "));

            // 药费
            paragraph.Inlines.Add(new Run("药费："));
            var medicineFee = _prescription.MedicineFee > 0 ? $"{_prescription.MedicineFee:F0}" : "";
            paragraph.Inlines.Add(CreateUnderlinedValue(medicineFee, 5));
            paragraph.Inlines.Add(new Run("  "));

            // 治疗费
            paragraph.Inlines.Add(new Run("治疗费："));
            var treatFee = _prescription.TreatmentFee > 0 ? $"{_prescription.TreatmentFee:F0}" : "";
            paragraph.Inlines.Add(CreateUnderlinedValue(treatFee, 5));
            paragraph.Inlines.Add(new Run("  "));

            // 合计
            paragraph.Inlines.Add(new Run("合计："));
            var total = _prescription.TotalPrice > 0 ? $"{_prescription.TotalPrice:F0}" : "";
            paragraph.Inlines.Add(CreateUnderlinedValue(total, 6));

            _document.Blocks.Add(paragraph);
        }

        // ===== 辅助方法 =====

        /// <summary>
        /// 创建带下划线的值（模拟模板中的下划线填写区域）
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="minWidth">最小宽度（字符数），0表示填满剩余空间</param>
        private Run CreateUnderlinedValue(string value, int minWidth)
        {
            string displayValue;
            if (string.IsNullOrEmpty(value))
            {
                // 空值时显示空格占位
                displayValue = minWidth > 0 ? new string(' ', minWidth) : "          ";
            }
            else
            {
                // 有值时显示值，并填充到最小宽度
                displayValue = minWidth > 0 && value.Length < minWidth
                    ? value.PadRight(minWidth)
                    : value;
            }

            return new Run(displayValue)
            {
                TextDecorations = TextDecorations.Underline
            };
        }
    }
}
