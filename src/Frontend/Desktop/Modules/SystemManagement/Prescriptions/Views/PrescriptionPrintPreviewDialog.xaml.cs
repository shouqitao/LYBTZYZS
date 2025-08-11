using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Admin.Prescriptions.Services;

namespace LYBT.Desktop.Admin.Prescriptions.Views
{
    /// <summary>
    /// 处方打印预览对话框
    /// </summary>
    public partial class PrescriptionPrintPreviewDialog : Window
    {
        private readonly IPrescriptionPrintService _printService;
        private PrescriptionInfo? _prescription;
        private FlowDocument? _document;

        public PrescriptionPrintPreviewDialog(
            IPrescriptionPrintService printService,
            PrescriptionInfo prescription)
        {
            InitializeComponent();
            _printService = printService;
            _prescription = prescription;
            
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 显示处方信息
                if (_prescription != null)
                {
                    PrescriptionInfoText.Text = $"处方编号：{_prescription.PrescriptionNumber} | " +
                                               $"患者：{_prescription.PatientName} | " +
                                               $"开方日期：{_prescription.CreateTime:yyyy-MM-dd}";
                }

                // 生成预览文档
                _document = await _printService.GeneratePrintPreview(_prescription!);
                if (_document != null)
                {
                    DocumentViewer.Document = _document;
                }
                else
                {
                    MessageBox.Show("生成预览失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载预览失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void ZoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentViewer == null) return;

            var selectedItem = ZoomComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            var zoomText = selectedItem.Content.ToString();
            
            switch (zoomText)
            {
                case "50%":
                    DocumentViewer.Zoom = 50;
                    break;
                case "75%":
                    DocumentViewer.Zoom = 75;
                    break;
                case "100%":
                    DocumentViewer.Zoom = 100;
                    break;
                case "125%":
                    DocumentViewer.Zoom = 125;
                    break;
                case "150%":
                    DocumentViewer.Zoom = 150;
                    break;
                case "200%":
                    DocumentViewer.Zoom = 200;
                    break;
                case "适应宽度":
                    FitToWidth();
                    break;
                case "适应页面":
                    FitToPage();
                    break;
            }
        }

        private void FitToWidth()
        {
            if (DocumentViewer?.Document == null) return;
            
            // 计算适应宽度的缩放比例
            var pageWidth = DocumentViewer.Document.PageWidth;
            var viewerWidth = DocumentViewer.ActualWidth - 40; // 减去边距
            
            if (pageWidth > 0 && viewerWidth > 0)
            {
                var zoom = (viewerWidth / pageWidth) * 100;
                DocumentViewer.Zoom = Math.Min(200, Math.Max(50, zoom));
            }
        }

        private void FitToPage()
        {
            if (DocumentViewer?.Document == null) return;
            
            // 计算适应页面的缩放比例
            var pageWidth = DocumentViewer.Document.PageWidth;
            var pageHeight = DocumentViewer.Document.PageHeight;
            var viewerWidth = DocumentViewer.ActualWidth - 40;
            var viewerHeight = DocumentViewer.ActualHeight - 40;
            
            if (pageWidth > 0 && pageHeight > 0 && viewerWidth > 0 && viewerHeight > 0)
            {
                var zoomWidth = (viewerWidth / pageWidth) * 100;
                var zoomHeight = (viewerHeight / pageHeight) * 100;
                DocumentViewer.Zoom = Math.Min(200, Math.Max(50, Math.Min(zoomWidth, zoomHeight)));
            }
        }

        private void PageSetup_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现页面设置对话框
            MessageBox.Show("页面设置功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_prescription == null)
                {
                    MessageBox.Show("无法导出：处方数据为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 选择保存路径
                var saveDialog = new SaveFileDialog
                {
                    Title = "导出处方为PDF",
                    Filter = "PDF文件|*.pdf|XPS文件|*.xps",
                    FileName = $"处方_{_prescription.PrescriptionNumber}_{DateTime.Now:yyyyMMdd}.pdf",
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var success = await _printService.ExportToPdf(_prescription, saveDialog.FileName);
                    if (success)
                    {
                        var result = MessageBox.Show(
                            "导出成功！是否打开文件？",
                            "成功",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            // 打开导出的文件
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveDialog.FileName.Replace(".pdf", ".xps"), // 暂时是XPS格式
                                UseShellExecute = true
                            });
                        }
                    }
                    else
                    {
                        MessageBox.Show("导出失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_prescription == null)
                {
                    MessageBox.Show("无法打印：处方数据为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var success = await _printService.PrintPrescription(_prescription);
                if (success)
                {
                    MessageBox.Show("打印任务已发送到打印机", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("打印取消或失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}