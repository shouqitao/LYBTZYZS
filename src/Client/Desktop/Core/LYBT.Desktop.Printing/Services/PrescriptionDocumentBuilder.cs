using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Desktop.Printing.Templates;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Printing.Services
{
    /// <summary>
    /// 处方文档构建器
    /// U3-5: 从 PrescriptionPrintService 拆分，负责 FixedDocument 页面构建逻辑
    /// </summary>
    public class PrescriptionDocumentBuilder
    {
        private readonly ILogger<PrescriptionDocumentBuilder> _logger;

        // 纸张尺寸定义（像素，96 DPI）
        public static readonly Size A5PageSize = new(559, 794);  // 148mm x 210mm
        public static readonly Size A4PageSize = new(794, 1123); // 210mm x 297mm

        // T4-S5-09: 分页阈值和容量常量
        public const int A5FirstPageHerbLimit = 12;      // A5首页最多显示12味药材
        public const int A4FirstPageHerbLimit = 20;      // A4首页最多显示20味药材
        public const int ContinuationPageHerbLimit = 20; // 续页最多显示20味药材（头部更简洁）

        public PrescriptionDocumentBuilder(ILogger<PrescriptionDocumentBuilder> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据纸张枚举获取页面尺寸
        /// </summary>
        public static Size GetPageSize(PaperSize paperSize)
        {
            return paperSize switch
            {
                PaperSize.A4 => A4PageSize,
                PaperSize.A5 => A5PageSize,
                _ => A5PageSize
            };
        }

        /// <summary>
        /// 判断是否为A4纸张尺寸
        /// </summary>
        public static bool IsA4(Size pageSize) => pageSize.Width >= A4PageSize.Width;

        /// <summary>
        /// 根据纸张大小获取首页药材限制
        /// </summary>
        public static int GetFirstPageHerbLimit(Size pageSize) =>
            IsA4(pageSize) ? A4FirstPageHerbLimit : A5FirstPageHerbLimit;

        /// <summary>
        /// 构建FixedDocument，支持多页
        /// T4-S5-09: 当药材超过首页限制时自动分页 (A5=12味, A4=20味)
        /// </summary>
        public FixedDocument BuildFixedDocument(PrescriptionPrintModel model, Size pageSize)
        {
            var document = new FixedDocument();
            document.DocumentPaginator.PageSize = pageSize;

            var itemCount = model.Items?.Count ?? 0;
            var firstPageLimit = GetFirstPageHerbLimit(pageSize);

            if (itemCount <= firstPageLimit)
            {
                // 单页模式
                var pageContent = new PageContent();
                var fixedPage = CreateFixedPage(model, pageSize);
                ((IAddChild)pageContent).AddChild(fixedPage);
                document.Pages.Add(pageContent);
            }
            else
            {
                // 多页模式 T4-S5-09
                _logger.LogInformation("[PRINT] Multi-page mode: {ItemCount} herbs, threshold={Threshold}",
                    itemCount, firstPageLimit);
                BuildMultiPageDocument(document, model, pageSize);
            }

            return document;
        }

        /// <summary>
        /// 构建多页文档
        /// T4-S5-09: 首页显示前N味药材（使用完整模板），后续页使用续页模板
        /// </summary>
        public void BuildMultiPageDocument(FixedDocument document, PrescriptionPrintModel model, Size pageSize)
        {
            var allItems = model.Items?.ToList() ?? new List<PrescriptionItemPrintModel>();
            var totalItems = allItems.Count;
            var offset = 0;
            var firstPageLimit = GetFirstPageHerbLimit(pageSize);

            // 第1页：使用完整模板，限制药材数量
            var firstPageItems = allItems.Take(firstPageLimit).ToList();
            var firstPageModel = CloneModelWithItems(model, firstPageItems);
            var firstPage = CreateFixedPage(firstPageModel, pageSize);
            var firstPageContent = new PageContent();
            ((IAddChild)firstPageContent).AddChild(firstPage);
            document.Pages.Add(firstPageContent);
            offset += firstPageLimit;

            // 后续页：使用续页模板
            while (offset < totalItems)
            {
                var remainingCount = totalItems - offset;
                var pageItems = allItems.Skip(offset).Take(ContinuationPageHerbLimit).ToList();
                var isLastPage = (offset + pageItems.Count) >= totalItems;

                var continuationModel = CloneModelWithItems(model, pageItems);
                var continuationPage = CreateContinuationFixedPage(continuationModel, pageSize, isLastPage);
                var continuationPageContent = new PageContent();
                ((IAddChild)continuationPageContent).AddChild(continuationPage);
                document.Pages.Add(continuationPageContent);

                offset += pageItems.Count;
            }

            _logger.LogInformation("[PRINT] Multi-page document built: {PageCount} pages for {ItemCount} herbs",
                document.Pages.Count, totalItems);
        }

        /// <summary>
        /// 克隆打印模型但替换药材列表
        /// T4-S5-09: 委托给模型的 CloneWithItems 方法
        /// </summary>
        public static PrescriptionPrintModel CloneModelWithItems(
            PrescriptionPrintModel source,
            List<PrescriptionItemPrintModel> items)
        {
            return source.CloneWithItems(items);
        }

        /// <summary>
        /// 创建首页 FixedPage（根据纸张尺寸选择A4或A5模板）
        /// </summary>
        public FixedPage CreateFixedPage(PrescriptionPrintModel model, Size pageSize)
        {
            var template = IsA4(pageSize)
                ? (UserControl)new PrescriptionPrintA4Template { DataContext = model, Width = pageSize.Width, Height = pageSize.Height }
                : new PrescriptionPrintTemplate { DataContext = model, Width = pageSize.Width, Height = pageSize.Height };

            return CreatePageFromTemplate(template, pageSize);
        }

        /// <summary>
        /// 创建续页 FixedPage（根据纸张尺寸选择A4或A5续页模板）
        /// T4-S5-09
        /// </summary>
        public FixedPage CreateContinuationFixedPage(PrescriptionPrintModel model, Size pageSize, bool isLastPage)
        {
            UserControl template;
            if (IsA4(pageSize))
            {
                var a4Template = new PrescriptionContinuationA4Template
                {
                    DataContext = model,
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };
                if (isLastPage) a4Template.SetAsLastPage();
                template = a4Template;
            }
            else
            {
                var a5Template = new PrescriptionContinuationTemplate
                {
                    DataContext = model,
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };
                if (isLastPage) a5Template.SetAsLastPage();
                template = a5Template;
            }

            return CreatePageFromTemplate(template, pageSize);
        }

        /// <summary>
        /// 从 XAML 模板创建 FixedPage（共享布局逻辑）
        /// </summary>
        public static FixedPage CreatePageFromTemplate(UserControl template, Size pageSize)
        {
            template.Measure(pageSize);
            template.Arrange(new Rect(pageSize));
            template.UpdateLayout();

            var fixedPage = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = System.Windows.Media.Brushes.White
            };

            fixedPage.Children.Add(template);
            FixedPage.SetLeft(template, 0);
            FixedPage.SetTop(template, 0);

            fixedPage.Measure(pageSize);
            fixedPage.Arrange(new Rect(pageSize));
            fixedPage.UpdateLayout();

            return fixedPage;
        }
    }
}
