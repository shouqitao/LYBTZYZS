using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Core.Views
{
    public partial class PrintPreviewDialog : Window
    {
        private readonly string _content;
        private readonly IPrescriptionPrintService _printService;
        private readonly object _medicalRecord;

        public PrintPreviewDialog(string content, IPrescriptionPrintService printService = null, object medicalRecord = null)
        {
            InitializeComponent();
            
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _printService = printService;
            _medicalRecord = medicalRecord;

            // 显示预览内容
            PreviewTextBlock.Text = content;
            
            // 如果没有打印服务，禁用打印按钮
            if (_printService == null)
            {
                PrintButton.IsEnabled = false;
                PrintButton.ToolTip = "打印服务未可用";
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintButton.IsEnabled = false;
                PrintButton.Content = "正在打印...";

                if (_printService != null && _medicalRecord != null)
                {
                    // 使用打印服务打印
                    var success = await _printService.PrintPrescriptionAsync(_medicalRecord);
                    if (success)
                    {
                        MessageBox.Show("打印成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("打印失败，请检查打印机设置", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // 直接打印文档
                    await PrintDirectlyAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印过程中发生错误: {ex.Message}", "打印错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PrintButton.IsEnabled = true;
                PrintButton.Content = "打印";
            }
        }

        private async Task PrintDirectlyAsync()
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // 创建打印文档
                        var printDocument = CreatePrintDocument(_content);
                        
                        // 显示打印对话框
                        var printDialog = new PrintDialog();
                        if (printDialog.ShowDialog() == true)
                        {
                            printDialog.PrintDocument(
                                ((IDocumentPaginatorSource)printDocument).DocumentPaginator, 
                                "凌隐宝堂处方");
                            
                            MessageBox.Show("打印成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            DialogResult = true;
                            Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"打印失败: {ex.Message}", "打印错误", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            });
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveButton.IsEnabled = false;
                SaveButton.Content = "正在保存...";

                if (_printService != null && _medicalRecord != null)
                {
                    // 使用打印服务保存
                    var saveFileDialog = new SaveFileDialog
                    {
                        Title = "保存处方文档",
                        Filter = "文本文件 (*.txt)|*.txt|PDF文件 (*.pdf)|*.pdf|所有文件 (*.*)|*.*",
                        DefaultExt = "txt",
                        FileName = $"处方_{DateTime.Now:yyyyMMdd_HHmmss}"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        var success = await _printService.SaveAsPdfAsync(_medicalRecord, saveFileDialog.FileName);
                        if (success)
                        {
                            MessageBox.Show($"文档已保存至: {saveFileDialog.FileName}", "保存成功", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else
                {
                    // 直接保存文本文件
                    await SaveDirectlyAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存过程中发生错误: {ex.Message}", "保存错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = "保存为文件";
            }
        }

        private async Task SaveDirectlyAsync()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "保存处方文档",
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"处方_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    await File.WriteAllTextAsync(saveFileDialog.FileName, _content, System.Text.Encoding.UTF8);
                    MessageBox.Show($"文档已保存至: {saveFileDialog.FileName}", "保存成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败: {ex.Message}", "保存错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private FlowDocument CreatePrintDocument(string content)
        {
            var flowDocument = new FlowDocument();
            
            // 设置文档样式
            flowDocument.FontFamily = new FontFamily("宋体");
            flowDocument.FontSize = 14;
            flowDocument.LineHeight = 18;
            flowDocument.PagePadding = new Thickness(50);
            flowDocument.ColumnWidth = double.PositiveInfinity;
            
            // 添加内容
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run(content));
            flowDocument.Blocks.Add(paragraph);
            
            return flowDocument;
        }

        /// <summary>
        /// 静态方法：显示打印预览对话框
        /// </summary>
        public static bool? ShowPrintPreview(string content, IPrescriptionPrintService printService = null, object medicalRecord = null)
        {
            var dialog = new PrintPreviewDialog(content, printService, medicalRecord);
            return dialog.ShowDialog();
        }
    }
}