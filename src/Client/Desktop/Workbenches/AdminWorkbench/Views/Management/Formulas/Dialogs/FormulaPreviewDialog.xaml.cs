using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Workbench.Admin.Views.Management.Formulas.Dialogs
{
    /// <summary>
    /// FormulaPreviewDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FormulaPreviewDialog : Window
    {
        private readonly FormulaDto _formula;

        public FormulaPreviewDialog(FormulaDto formula)
        {
            InitializeComponent();
            
            _formula = formula ?? throw new ArgumentNullException(nameof(formula));
            LoadFormulaData();
        }

        private void LoadFormulaData()
        {
            // 验方基本信息
            TxtName.Text = _formula.Name;
            TxtEffect.Text = _formula.Effect ?? "无";
            TxtUsage.Text = _formula.Usage ?? "无";
            TxtIndications.Text = _formula.Indications ?? "无";
            TxtContraindications.Text = _formula.Contraindications ?? "无";
            TxtPreparation.Text = _formula.Preparation ?? "无";
            TxtIsShared.Text = _formula.IsShared ? "是" : "否";
            
            // 创建和更新信息
            TxtCreatedInfo.Text = $"创建时间: {_formula.CreateTime:yyyy-MM-dd HH:mm:ss}  创建者: {_formula.CreatedByName ?? "系统"}";
            TxtUpdatedInfo.Text = $"更新时间: {(_formula.UpdateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未更新")}  更新者: 系统";
            
            // 药材组成
            if (_formula.Herbs != null && _formula.Herbs.Any())
            {
                var herbItems = _formula.Herbs.OrderBy(h => h.SortOrder).Select((herb, index) => new
                {
                    SortOrder = index + 1,
                    HerbName = herb.Herb?.Name ?? "未知药材",
                    Quantity = herb.Quantity,
                    Preparation = herb.Preparation ?? "常规",
                    Usage = herb.Usage ?? "煎服"
                }).ToList();
                
                DgHerbs.ItemsSource = herbItems;
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建打印文档
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // 创建打印内容
                    FlowDocument flowDoc = CreatePrintDocument();
                    
                    // 设置页面大小
                    flowDoc.PageWidth = printDialog.PrintableAreaWidth;
                    flowDoc.PageHeight = printDialog.PrintableAreaHeight;
                    
                    // 打印
                    IDocumentPaginatorSource idocument = flowDoc as IDocumentPaginatorSource;
                    printDialog.PrintDocument(idocument.DocumentPaginator, "验方详情");
                    
                    MessageBox.Show("打印任务已发送到打印机", "打印", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreatePrintDocument()
        {
            FlowDocument flowDoc = new FlowDocument();
            
            // 标题
            Paragraph title = new Paragraph(new Run("验方详情"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            flowDoc.Blocks.Add(title);
            
            // 验方名称
            Paragraph name = new Paragraph();
            name.Inlines.Add(new Run("验方名称: ") { FontWeight = FontWeights.Bold });
            name.Inlines.Add(new Run(_formula.Name));
            flowDoc.Blocks.Add(name);
            
            // 功效
            Paragraph effect = new Paragraph();
            effect.Inlines.Add(new Run("功效: ") { FontWeight = FontWeights.Bold });
            effect.Inlines.Add(new Run(_formula.Effect ?? "无"));
            flowDoc.Blocks.Add(effect);
            
            // 用法
            Paragraph usage = new Paragraph();
            usage.Inlines.Add(new Run("用法: ") { FontWeight = FontWeights.Bold });
            usage.Inlines.Add(new Run(_formula.Usage ?? "无"));
            flowDoc.Blocks.Add(usage);
            
            // 适应症
            Paragraph indications = new Paragraph();
            indications.Inlines.Add(new Run("适应症: ") { FontWeight = FontWeights.Bold });
            indications.Inlines.Add(new Run(_formula.Indications ?? "无"));
            flowDoc.Blocks.Add(indications);
            
            // 禁忌症
            Paragraph contraindications = new Paragraph();
            contraindications.Inlines.Add(new Run("禁忌症: ") { FontWeight = FontWeights.Bold });
            contraindications.Inlines.Add(new Run(_formula.Contraindications ?? "无"));
            flowDoc.Blocks.Add(contraindications);
            
            // 制备方法
            Paragraph preparation = new Paragraph();
            preparation.Inlines.Add(new Run("制备方法: ") { FontWeight = FontWeights.Bold });
            preparation.Inlines.Add(new Run(_formula.Preparation ?? "无"));
            flowDoc.Blocks.Add(preparation);
            
            // 药材组成
            if (_formula.Herbs != null && _formula.Herbs.Any())
            {
                Paragraph herbsTitle = new Paragraph(new Run("药材组成:"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 20, 0, 10)
                };
                flowDoc.Blocks.Add(herbsTitle);
                
                Table herbsTable = new Table();
                herbsTable.CellSpacing = 0;
                herbsTable.BorderBrush = Brushes.Black;
                herbsTable.BorderThickness = new Thickness(1);
                
                // 表格列
                herbsTable.Columns.Add(new TableColumn { Width = new GridLength(50) });  // 序号
                herbsTable.Columns.Add(new TableColumn { Width = new GridLength(150) }); // 药材名称
                herbsTable.Columns.Add(new TableColumn { Width = new GridLength(80) });  // 用量
                herbsTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 制法
                herbsTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 用法
                
                herbsTable.RowGroups.Add(new TableRowGroup());
                
                // 表头
                TableRow headerRow = new TableRow();
                headerRow.Background = Brushes.LightGray;
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("序号"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("药材名称"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("用量"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("制法"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("用法"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                herbsTable.RowGroups[0].Rows.Add(headerRow);
                
                // 数据行
                var sortedHerbs = _formula.Herbs.OrderBy(h => h.SortOrder).ToList();
                for (int i = 0; i < sortedHerbs.Count; i++)
                {
                    var herb = sortedHerbs[i];
                    TableRow dataRow = new TableRow();
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run((i + 1).ToString()))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(herb.Herb?.Name ?? "未知药材"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run($"{herb.Quantity}g"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(herb.Preparation ?? "常规"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(herb.Usage ?? "煎服"))) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) });
                    herbsTable.RowGroups[0].Rows.Add(dataRow);
                }
                
                flowDoc.Blocks.Add(herbsTable);
            }
            
            // 打印时间
            Paragraph printTime = new Paragraph(new Run($"打印时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"))
            {
                FontSize = 10,
                Foreground = Brushes.Gray,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            flowDoc.Blocks.Add(printTime);
            
            return flowDoc;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}