using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 简化的处方打印服务实现
    /// </summary>
    public class SimplePrescriptionPrintService : IPrescriptionPrintService
    {
        /// <summary>
        /// 预览处方
        /// </summary>
        public async Task<PreviewResult> PreviewPrescriptionAsync(object medicalRecord)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (medicalRecord is SimplePrescriptionModel model)
                    {
                        var html = GenerateHtmlContent(model);
                        return new PreviewResult
                        {
                            Success = true,
                            Content = html,
                            Message = "预览生成成功"
                        };
                    }
                    
                    return new PreviewResult
                    {
                        Success = false,
                        Message = "无效的处方数据"
                    };
                }
                catch (Exception ex)
                {
                    return new PreviewResult
                    {
                        Success = false,
                        Message = $"预览生成失败：{ex.Message}"
                    };
                }
            });
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
                    if (medicalRecord is SimplePrescriptionModel model)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var document = CreatePrescriptionDocument(model);
                            var printDialog = new PrintDialog();
                            
                            if (printDialog.ShowDialog() == true)
                            {
                                document.PageHeight = printDialog.PrintableAreaHeight;
                                document.PageWidth = printDialog.PrintableAreaWidth;
                                printDialog.PrintDocument(
                                    ((IDocumentPaginatorSource)document).DocumentPaginator, 
                                    "中医处方"
                                );
                            }
                        });
                        
                        return true;
                    }
                    
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// 保存为PDF（实际保存为HTML，可通过浏览器打印为PDF）
        /// </summary>
        public async Task<bool> SaveAsPdfAsync(object medicalRecord, string fileName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (medicalRecord is SimplePrescriptionModel model)
                    {
                        var html = GenerateHtmlContent(model);
                        var htmlPath = fileName.Replace(".pdf", ".html");
                        File.WriteAllText(htmlPath, html, Encoding.UTF8);
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                $"处方已导出到：{htmlPath}\n请使用浏览器打开并打印为PDF",
                                "导出成功",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        });
                        
                        return true;
                    }
                    
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// 创建处方文档
        /// </summary>
        private FlowDocument CreatePrescriptionDocument(SimplePrescriptionModel model)
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("Microsoft YaHei"),
                FontSize = 12
            };

            // 诊所名称
            document.Blocks.Add(new Paragraph(new Run("凌隐宝堂中医诊所"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // 处方标题
            document.Blocks.Add(new Paragraph(new Run("中医处方笺"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // 患者信息
            document.Blocks.Add(new Paragraph
            {
                Inlines =
                {
                    new Run($"患者姓名：{model.PatientName}    "),
                    new Run($"性别：{model.PatientGender}    "),
                    new Run($"年龄：{model.PatientAge}岁    "),
                    new Run($"电话：{model.PatientPhone}")
                },
                Margin = new Thickness(0, 0, 0, 10)
            });

            // 医生信息
            document.Blocks.Add(new Paragraph
            {
                Inlines =
                {
                    new Run($"医生：{model.DoctorName}    "),
                    new Run($"日期：{model.PrescriptionDate:yyyy年MM月dd日}")
                },
                Margin = new Thickness(0, 0, 0, 10)
            });

            // 分隔线
            document.Blocks.Add(new Paragraph(new Run("────────────────────────────────"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            });

            // 诊断信息
            if (!string.IsNullOrWhiteSpace(model.Diagnosis))
            {
                document.Blocks.Add(new Paragraph(new Run($"【诊断】{model.Diagnosis}"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 5, 0, 10)
                });
            }

            // 处方标题
            document.Blocks.Add(new Paragraph(new Run("【处方】"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            });

            // 处方内容
            var list = new List();
            list.MarkerStyle = TextMarkerStyle.Decimal;
            
            foreach (var item in model.Herbs)
            {
                var listItem = new ListItem(new Paragraph(new Run(
                    $"{item.Name} {item.Quantity}{item.Unit}"
                )));
                list.ListItems.Add(listItem);
            }
            
            document.Blocks.Add(list);

            // 总价
            document.Blocks.Add(new Paragraph(new Run($"总价：￥{model.TotalPrice:F2}"))
            {
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 10, 0, 10)
            });

            // 用法
            document.Blocks.Add(new Paragraph(new Run($"【用法】{model.Usage}"))
            {
                Margin = new Thickness(0, 10, 0, 5)
            });

            // 医嘱
            if (!string.IsNullOrWhiteSpace(model.DoctorAdvice))
            {
                document.Blocks.Add(new Paragraph(new Run($"【医嘱】{model.DoctorAdvice}"))
                {
                    Margin = new Thickness(0, 5, 0, 5)
                });
            }

            // 签名区
            document.Blocks.Add(new Paragraph(new Run($"\n\n医生签名：___________"))
            {
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            });

            return document;
        }

        /// <summary>
        /// 生成HTML内容
        /// </summary>
        private string GenerateHtmlContent(SimplePrescriptionModel model)
        {
            var html = new StringBuilder();
            
            html.AppendLine(string.Format(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>中医处方</title>
    <style>
        body { 
            font-family: 'Microsoft YaHei', sans-serif; 
            max-width: 800px; 
            margin: 0 auto; 
            padding: 20px;
        }
        h1 { text-align: center; color: #2E86AB; margin-bottom: 10px; }
        h2 { text-align: center; color: #333; margin-bottom: 30px; }
        .info-row { margin: 10px 0; }
        .info-row span { margin-right: 30px; }
        .section { margin: 20px 0; }
        .section-title { font-weight: bold; color: #2E86AB; }
        .herb-list { margin-left: 20px; }
        .herb-item { margin: 5px 0; }
        .total { text-align: right; font-size: 18px; font-weight: bold; color: #d9534f; margin: 20px 0; }
        .signature { text-align: right; margin-top: 50px; }
        hr { border: none; border-top: 1px solid #ddd; margin: 20px 0; }
        @media print { 
            body { padding: 10px; } 
            h1 { color: #000; }
        }
    </style>
</head>
<body>
    <h1>凌隐宝堂中医诊所</h1>
    <h2>中医处方笺</h2>
    
    <div class='info-row'>
        <span><b>患者姓名：</b>{0}</span>
        <span><b>性别：</b>{1}</span>
        <span><b>年龄：</b>{2}岁</span>
        <span><b>电话：</b>{3}</span>
    </div>
    
    <div class='info-row'>
        <span><b>医生：</b>{4}</span>
        <span><b>开方日期：</b>{5:yyyy年MM月dd日}</span>
    </div>
    
    <hr>",
                model.PatientName,
                model.PatientGender,
                model.PatientAge,
                model.PatientPhone,
                model.DoctorName,
                model.PrescriptionDate
            ));

            // 诊断
            if (!string.IsNullOrWhiteSpace(model.Diagnosis))
            {
                html.AppendLine($@"
    <div class='section'>
        <span class='section-title'>【诊断】</span>{model.Diagnosis}
    </div>");
            }

            // 处方
            html.AppendLine(@"
    <div class='section'>
        <span class='section-title'>【处方】</span>
        <div class='herb-list'>");

            int index = 1;
            foreach (var herb in model.Herbs)
            {
                html.AppendLine($@"
            <div class='herb-item'>{index}. {herb.Name} {herb.Quantity}{herb.Unit}</div>");
                index++;
            }

            html.AppendLine(@"
        </div>
    </div>");

            // 总价
            html.AppendLine($@"
    <div class='total'>总价：￥{model.TotalPrice:F2}</div>");

            // 用法
            html.AppendLine($@"
    <div class='section'>
        <span class='section-title'>【用法】</span>{model.Usage}
    </div>");

            // 医嘱
            if (!string.IsNullOrWhiteSpace(model.DoctorAdvice))
            {
                html.AppendLine($@"
    <div class='section'>
        <span class='section-title'>【医嘱】</span>{model.DoctorAdvice}
    </div>");
            }

            // 签名
            html.AppendLine(@"
    <div class='signature'>
        医生签名：_______________
    </div>
</body>
</html>");

            return html.ToString();
        }
    }

    /// <summary>
    /// 简化的处方模型
    /// </summary>
    public class SimplePrescriptionModel
    {
        public string PatientName { get; set; } = string.Empty;
        public string PatientGender { get; set; } = string.Empty;
        public int PatientAge { get; set; }
        public string PatientPhone { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime PrescriptionDate { get; set; } = DateTime.Now;
        public string? Diagnosis { get; set; }
        public List<HerbItem> Herbs { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public string Usage { get; set; } = "每日一剂，水煎服，分两次温服";
        public string? DoctorAdvice { get; set; }
    }

    /// <summary>
    /// 药材项
    /// </summary>
    public class HerbItem
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "克";
    }
}