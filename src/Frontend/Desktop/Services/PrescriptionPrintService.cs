using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Text;
using System.Linq;
using System.Dynamic;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 处方打印服务实现
    /// </summary>
    public class PrescriptionPrintService : IPrescriptionPrintService
    {
        /// <summary>
        /// 预览处方
        /// </summary>
        public async Task<PreviewResult> PreviewPrescriptionAsync(object medicalRecord)
        {
            await Task.CompletedTask;

            try
            {
                var content = BuildPrescriptionContent(medicalRecord);
                return new PreviewResult
                {
                    Success = true,
                    Content = content,
                    Message = "预览生成成功"
                };
            }
            catch (Exception ex)
            {
                return new PreviewResult
                {
                    Success = false,
                    Content = string.Empty,
                    Message = $"预览生成失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        public async Task<bool> PrintPrescriptionAsync(object medicalRecord)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return Application.Current.Dispatcher.Invoke(() =>
                    {
                        var printDialog = new System.Windows.Controls.PrintDialog();

                        // 显示打印对话框
                        if (printDialog.ShowDialog() != true)
                            return false;

                        // 创建打印文档
                        var flowDoc = CreatePrintDocument(medicalRecord);

                        // 设置页面大小
                        flowDoc.PageWidth = printDialog.PrintableAreaWidth;
                        flowDoc.PageHeight = printDialog.PrintableAreaHeight;

                        // 执行打印
                        DocumentPaginator paginator = ((IDocumentPaginatorSource)flowDoc).DocumentPaginator;
                        printDialog.PrintDocument(paginator, "处方打印");

                        return true;
                    });
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// 保存为PDF
        /// </summary>
        public async Task<bool> SaveAsPdfAsync(object medicalRecord, string fileName)
        {
            await Task.CompletedTask;

            // PDF 保存功能需要第三方库如 iTextSharp，这里先返回 true 作为占位符
            // 实际实现中需要添加PDF生成库
            return true;
        }

        /// <summary>
        /// 构建处方打印内容（文本格式）
        /// </summary>
        private string BuildPrescriptionContent(object data)
        {
            dynamic prescription = data;
            var content = new StringBuilder();

            // 诊所抬头
            content.AppendLine($"{prescription.ClinicName}");
            content.AppendLine("═════════════════════════════════════");
            content.AppendLine();

            // 患者信息
            content.AppendLine($"患者姓名：{prescription.PatientName}");
            content.AppendLine($"看诊时间：{((DateTime)prescription.ConsultationTime):yyyy年MM月dd日 HH:mm}");
            content.AppendLine($"医生：{prescription.DoctorName}");
            content.AppendLine();

            // 中医诊断
            if (!string.IsNullOrWhiteSpace(prescription.TCMDiagnosis))
            {
                content.AppendLine($"中医诊断：{prescription.TCMDiagnosis}");
                content.AppendLine();
            }

            // 治疗原则
            if (!string.IsNullOrWhiteSpace(prescription.TreatmentPrinciple))
            {
                content.AppendLine($"治疗原则：{prescription.TreatmentPrinciple}");
                content.AppendLine();
            }

            // 处方内容
            content.AppendLine("处方：");
            content.AppendLine("─────────────────────────────────────");

            int index = 1;
            foreach (var item in prescription.PrescriptionItems)
            {
                content.AppendLine($"{index,2}. {item.HerbName,-15} {item.Quantity,6}{item.Unit}");
                index++;
            }

            content.AppendLine("─────────────────────────────────────");
            content.AppendLine($"共 {prescription.TotalItems} 味药材");
            content.AppendLine();

            // 用法用量
            content.AppendLine("用法：水煎服，一日一剂，分早晚两次温服。");
            content.AppendLine();

            // 医嘱
            if (!string.IsNullOrWhiteSpace(prescription.MedicalAdvice))
            {
                content.AppendLine($"医嘱：{prescription.MedicalAdvice}");
                content.AppendLine();
            }

            // 备注
            if (!string.IsNullOrWhiteSpace(prescription.Remark))
            {
                content.AppendLine($"备注：{prescription.Remark}");
                content.AppendLine();
            }

            content.AppendLine("═════════════════════════════════════");
            content.AppendLine($"打印时间：{((DateTime)prescription.PrintTime):yyyy-MM-dd HH:mm:ss}");

            return content.ToString();
        }

        /// <summary>
        /// 创建打印文档
        /// </summary>
        private FlowDocument CreatePrintDocument(object data)
        {
            dynamic prescription = data;
            var doc = new FlowDocument();

            // 设置页面样式
            doc.PagePadding = new Thickness(50);
            doc.FontFamily = new FontFamily("Microsoft YaHei");
            doc.FontSize = 12;

            // 诊所名称（标题）
            var title = new Paragraph(new Run($"{prescription.ClinicName}"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(title);

            // 分隔线
            doc.Blocks.Add(new Paragraph(new Run("═════════════════════════════════════"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // 患者信息
            var patientInfo = new Paragraph();
            patientInfo.Inlines.Add(new Run($"患者姓名：{prescription.PatientName}") { FontWeight = FontWeights.Bold });
            patientInfo.Inlines.Add(new LineBreak());
            patientInfo.Inlines.Add(new Run($"看诊时间：{((DateTime)prescription.ConsultationTime):yyyy年MM月dd日 HH:mm}"));
            patientInfo.Inlines.Add(new LineBreak());
            patientInfo.Inlines.Add(new Run($"医生：{prescription.DoctorName}"));
            patientInfo.Margin = new Thickness(0, 0, 0, 15);
            doc.Blocks.Add(patientInfo);

            // 诊断信息
            if (!string.IsNullOrWhiteSpace(prescription.TCMDiagnosis))
            {
                doc.Blocks.Add(new Paragraph(new Run($"中医诊断：{prescription.TCMDiagnosis}"))
                {
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // 处方标题
            doc.Blocks.Add(new Paragraph(new Run("处方："))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            });

            // 处方列表
            var prescriptionList = new List();
            int index = 1;
            foreach (var item in prescription.PrescriptionItems)
            {
                prescriptionList.ListItems.Add(new ListItem(
                    new Paragraph(new Run($"{item.HerbName}  {item.Quantity}{item.Unit}"))
                ));
                index++;
            }
            doc.Blocks.Add(prescriptionList);

            // 用法
            doc.Blocks.Add(new Paragraph(new Run("用法：水煎服，一日一剂，分早晚两次温服。"))
            {
                Margin = new Thickness(0, 15, 0, 10)
            });

            // 医嘱和备注
            if (!string.IsNullOrWhiteSpace(prescription.MedicalAdvice))
            {
                doc.Blocks.Add(new Paragraph(new Run($"医嘱：{prescription.MedicalAdvice}"))
                {
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // 打印时间
            doc.Blocks.Add(new Paragraph(new Run($"打印时间：{((DateTime)prescription.PrintTime):yyyy-MM-dd HH:mm:ss}"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            });

            return doc;
        }
    }
}