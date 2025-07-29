using LYBT.WPF.Client.Core.Models.FormulaTemplates;
using LYBT.WPF.Client.Core.Models.Herbs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.Windows.Data;

namespace LYBT.WPF.Client.Modules.SystemManagement.PrescriptionTemplates.ViewModels
{
    /// <summary>
    /// 验方模板管理视图模型
    /// </summary>
    public class PrescriptionTemplateManagementViewModel : BindableBase
    {
        private string _searchKeyword = string.Empty;
        private FormulaTemplateInfo _selectedTemplate;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;

        public ObservableCollection<FormulaTemplateInfo> Templates { get; }
        public ICollectionView TemplatesView { get; }

        // Commands
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddTemplateCommand { get; }
        public DelegateCommand ImportTemplatesCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<FormulaTemplateInfo> ViewTemplateCommand { get; }
        public DelegateCommand<FormulaTemplateInfo> EditTemplateCommand { get; }
        public DelegateCommand<FormulaTemplateInfo> DeleteTemplateCommand { get; }
        public DelegateCommand<FormulaTemplateInfo> CopyTemplateCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>选中的验方模板</summary>
        public FormulaTemplateInfo SelectedTemplate
        {
            get => _selectedTemplate;
            set => SetProperty(ref _selectedTemplate, value);
        }

        /// <summary>当前页码</summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>页大小</summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>总记录数</summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>状态文本</summary>
        public string StatusText => $"共 {TotalCount} 个验方模板，第 {CurrentPage} 页，共 {TotalPages} 页";

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        public PrescriptionTemplateManagementViewModel()
        {
            Templates = new ObservableCollection<FormulaTemplateInfo>();
            TemplatesView = CollectionViewSource.GetDefaultView(Templates);

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            AddTemplateCommand = new DelegateCommand(ExecuteAddTemplate);
            ImportTemplatesCommand = new DelegateCommand(ExecuteImportTemplates);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            ViewTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(ExecuteViewTemplate);
            EditTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(ExecuteEditTemplate);
            DeleteTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(ExecuteDeleteTemplate);
            CopyTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(ExecuteCopyTemplate);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, CanExecuteFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, CanExecuteNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, CanExecuteLastPage);

            // 加载初始数据
            LoadTemplates();
        }

        private async void LoadTemplates()
        {
            IsLoading = true;
            try
            {
                // TODO: 调用API获取验方模板列表
                await Task.Delay(1000); // 模拟API调用

                // 模拟数据
                Templates.Clear();
                var templateNames = new[]
                {
                    "小柴胡汤", "大承气汤", "麻黄汤", "桂枝汤", "白虎汤", "真武汤", "理中汤", "四君子汤", "四物汤", "逍遥散",
                    "补中益气汤", "六味地黄丸", "金匮肾气丸", "归脾汤", "甘麦大枣汤", "温胆汤", "二陈汤", "半夏白术天麻汤", "定志丸", "安神定志丸",
                    "清热解毒汤", "银翘散", "桑菊饮", "麻杏石甘汤", "止嗽散", "川贝枇杷露", "养阴清肺汤", "沙参麦冬汤", "百合固金汤", "清燥救肺汤"
                };

                var random = new Random();
                for (int i = 0; i < templateNames.Length; i++)
                {
                    var herbs = GenerateHerbsForTemplate(templateNames[i]);
                    Templates.Add(new FormulaTemplateInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = templateNames[i],
                        Herbs = herbs,
                        Remark = GetTemplateRemark(templateNames[i]),
                        IsActive = true,
                        CreatedTime = DateTime.Now.AddDays(-random.Next(1, 365)),
                        UpdatedTime = DateTime.Now.AddDays(-random.Next(1, 30))
                    });
                }

                TotalCount = Templates.Count;
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(TotalPages));

                // 更新分页命令状态
                FirstPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
                LastPageCommand.RaiseCanExecuteChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<HerbInfo> GenerateHerbsForTemplate(string templateName)
        {
            var herbs = new List<HerbInfo>();
            var herbNames = new[] { "柴胡", "黄芩", "人参", "半夏", "甘草", "生姜", "大枣", "当归", "白芍", "川芎", "熟地黄" };
            var random = new Random();
            
            var herbCount = random.Next(3, 8);
            for (int i = 0; i < herbCount; i++)
            {
                herbs.Add(new HerbInfo
                {
                    Id = Guid.NewGuid(),
                    Name = herbNames[random.Next(herbNames.Length)],
                    Unit = "g",
                    Price = (decimal)(random.NextDouble() * 20 + 5)
                });
            }
            
            return herbs;
        }

        private string GetTemplateRemark(string templateName)
        {
            var remarkMap = new Dictionary<string, string>
            {
                { "小柴胡汤", "和解少阳，主治往来寒热，胸胁苦满，默默不欲饮食，心烦喜呕" },
                { "大承气汤", "峻下热结，主治阳明腑实证" },
                { "麻黄汤", "发汗解表，宣肺平喘，主治外感风寒表实证" },
                { "桂枝汤", "解肌发表，调和营卫，主治外感风寒表虚证" },
                { "四君子汤", "益气健脾，主治脾胃气虚证" }
            };
            return remarkMap.ContainsKey(templateName) ? remarkMap[templateName] : "经典验方，临床常用";
        }

        private void ExecuteSearch()
        {
            // TODO: 实现搜索逻辑
            LoadTemplates();
        }

        private void ExecuteAddTemplate()
        {
            // TODO: 打开新增验方模板对话框
        }

        private void ExecuteImportTemplates()
        {
            // TODO: 打开导入验方模板对话框
        }

        private void ExecuteRefresh()
        {
            LoadTemplates();
        }

        private void ExecuteViewTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;
            // TODO: 打开查看验方模板详情对话框
        }

        private void ExecuteEditTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;
            // TODO: 打开编辑验方模板对话框
        }

        private void ExecuteDeleteTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;
            // TODO: 确认删除验方模板
        }

        private void ExecuteCopyTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;
            // TODO: 复制验方模板
        }

        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
            LoadTemplates();
        }

        private bool CanExecuteFirstPage()
        {
            return CurrentPage > 1;
        }

        private void ExecutePreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadTemplates();
            }
        }

        private bool CanExecutePreviousPage()
        {
            return CurrentPage > 1;
        }

        private void ExecuteNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadTemplates();
            }
        }

        private bool CanExecuteNextPage()
        {
            return CurrentPage < TotalPages;
        }

        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
            LoadTemplates();
        }

        private bool CanExecuteLastPage()
        {
            return CurrentPage < TotalPages;
        }
    }
}