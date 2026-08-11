using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Printing.Services
{
    /// <summary>
    /// 处方预览窗口构建器
    /// U3-5: 从 PrescriptionPrintService 拆分，负责预览窗口与设置面板构建
    /// </summary>
    public class PrescriptionPreviewWindowBuilder
    {
        private readonly ILogger<PrescriptionPreviewWindowBuilder> _logger;
        private readonly PrescriptionDocumentBuilder _documentBuilder;
        private readonly PrescriptionPrintExecutor _executor;

        public PrescriptionPreviewWindowBuilder(
            ILogger<PrescriptionPreviewWindowBuilder> logger,
            PrescriptionDocumentBuilder documentBuilder,
            PrescriptionPrintExecutor executor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        /// <summary>
        /// 显示处方预览窗口
        /// </summary>
        public void ShowPreviewWindow(FixedDocument document, PrescriptionPrintModel model, PrintOptions options, Action? onPrintCompleted = null)
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
            var settingsPanel = CreateSettingsPanel(document, model, options, previewWindow, docViewer, onPrintCompleted);
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
            DocumentViewer docViewer,
            Action? onPrintCompleted = null)
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
            paperSizeComboBox.Items.Add(new ComboBoxItem { Content = "A5 (148 x 210 mm)", Tag = PaperSize.A5 });
            paperSizeComboBox.Items.Add(new ComboBoxItem { Content = "A4 (210 x 297 mm)", Tag = PaperSize.A4 });
            paperSizeComboBox.SelectedIndex = options.PaperSize == PaperSize.A4 ? 1 : 0;

            var currentDocument = document;
            paperSizeComboBox.SelectionChanged += (s, e) =>
            {
                if (paperSizeComboBox.SelectedItem is ComboBoxItem item && item.Tag is PaperSize size)
                {
                    var newPageSize = PrescriptionDocumentBuilder.GetPageSize(size);
                    currentDocument = _documentBuilder.BuildFixedDocument(model, newPageSize);
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

                _executor.ExecutePrintDirect(currentDocument, printOptions);
                // T5-2 #18: 实际打印成功后触发回写回调（记录打印状态）
                onPrintCompleted?.Invoke();
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
                var defaultPrinter = LocalPrintServer.GetDefaultPrintQueue();
                var printQueues = _executor.GetPrintQueues();

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
    }
}
