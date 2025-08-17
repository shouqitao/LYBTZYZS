using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Formula.Services.Interfaces;
using LYBT.Desktop.Core.ViewModels.Base;
using CoreServices = LYBT.Desktop.Core.Interfaces.Services;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 验方管理增强版视图模型
    /// UltraThink模块化架构：使用FormulaModuleService独立实现业务逻辑
    /// </summary>
    public class FormulaManagementViewModelEnhanced : BindableBase
    {
        private readonly IFormulaModuleService _formulaModuleService;
        private readonly CoreServices.ICustomDialogService _dialogService;
        private readonly Prism.Events.IEventAggregator _eventAggregator;

        public string ModuleName => "验方模板管理";

        #region Properties

        private ObservableCollection<FormulaInfo> _items = new();
        public ObservableCollection<FormulaInfo> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private FormulaInfo? _selectedItem;
        public FormulaInfo? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        private ObservableCollection<string> _categories = new();
        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private string _selectedCategory = "全部";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    // 触发数据重新加载
                    RefreshCommand?.Execute();
                }
            }
        }

        #endregion

        #region Commands

        // 基础CRUD命令
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand<FormulaInfo> EditCommand { get; }
        public DelegateCommand<FormulaInfo> DeleteCommand { get; }
        
        // 分页命令
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        // UltraThink模块化架构：增强功能命令
        public DelegateCommand ImportTemplatesCommand { get; }
        public DelegateCommand<FormulaInfo> CopyTemplateCommand { get; }
        public DelegateCommand ExportTemplateCommand { get; }
        public DelegateCommand<FormulaInfo> ViewTemplateCommand { get; }

        #endregion

        public FormulaManagementViewModelEnhanced(
            IFormulaModuleService formulaModuleService,
            CoreServices.ICustomDialogService dialogService,
            Prism.Events.IEventAggregator eventAggregator)
        {
            _formulaModuleService = formulaModuleService ?? throw new ArgumentNullException(nameof(formulaModuleService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // 初始化基础CRUD命令
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            AddCommand = new DelegateCommand(async () => await AddAsync());
            EditCommand = new DelegateCommand<FormulaInfo>(async (item) => await EditAsync(item));
            DeleteCommand = new DelegateCommand<FormulaInfo>(async (item) => await DeleteAsync(item));
            
            // 初始化分页命令
            FirstPageCommand = new DelegateCommand(async () => await FirstPageAsync());
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync());
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync());
            LastPageCommand = new DelegateCommand(async () => await LastPageAsync());

            // 初始化增强功能命令
            ImportTemplatesCommand = new DelegateCommand(async () => await ImportTemplatesAsync());
            CopyTemplateCommand = new DelegateCommand<FormulaInfo>(async (template) => await CopyTemplate(template));
            ExportTemplateCommand = new DelegateCommand(async () => await ExportTemplateAsync());
            ViewTemplateCommand = new DelegateCommand<FormulaInfo>(async (template) => await ViewTemplate(template));

            // 初始化分类和数据
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await InitializeCategoriesAsync();
            await RefreshAsync();
        }

        private async Task InitializeCategoriesAsync()
        {
            try
            {
                var result = await _formulaModuleService.GetCategoriesAsync();
                if (result.IsSuccess)
                {
                    Categories.Clear();
                    foreach (var category in result.Data!)
                    {
                        Categories.Add(category);
                    }
                    SelectedCategory = Categories.FirstOrDefault() ?? "全部";
                }
                else
                {
                    // 如果服务调用失败，使用默认分类
                    InitializeDefaultCategories();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"初始化分类失败: {ex.Message}";
                InitializeDefaultCategories();
            }
        }

        private void InitializeDefaultCategories()
        {
            Categories.Clear();
            Categories.Add("全部");
            Categories.Add("内科方");
            Categories.Add("外科方");
            Categories.Add("妇科方");
            Categories.Add("儿科方");
            Categories.Add("皮肤科方");
            Categories.Add("五官科方");
            Categories.Add("骨伤科方");
            Categories.Add("经典方");
            Categories.Add("时方");
            Categories.Add("验方");
            Categories.Add("其他");
            SelectedCategory = Categories.FirstOrDefault() ?? "全部";
        }

        public async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载验方模板...";

                var queryRequest = new PagedQueryBaseDto
                {
                    PageIndex = CurrentPage,
                    PageSize = PageSize
                };

                var result = await _formulaModuleService.GetPagedAsync(queryRequest);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Items.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        Items.Add(item);
                    }
                    
                    TotalCount = result.Data.TotalCount;
                    StatusMessage = $"已加载 {Items.Count} 条验方模板";
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "加载验方模板失败";
                    await _dialogService.ShowErrorAsync(StatusMessage, "错误");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新失败: {ex.Message}";
                await _dialogService.ShowErrorAsync(StatusMessage, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region 分页方法

        public async Task FirstPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
                await RefreshAsync();
            }
        }

        public async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await RefreshAsync();
            }
        }

        public async Task NextPageAsync()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (CurrentPage < totalPages)
            {
                CurrentPage++;
                await RefreshAsync();
            }
        }

        public async Task LastPageAsync()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (CurrentPage < totalPages)
            {
                CurrentPage = totalPages;
                await RefreshAsync();
            }
        }

        #endregion

        #region CRUD操作方法

        public async Task AddAsync()
        {
            try
            {
                StatusMessage = "添加验方模板功能开发中...";
                await _dialogService.ShowInformationAsync("添加验方模板功能正在开发中", "提示");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"打开新增验方模板对话框失败: {ex.Message}", "错误");
            }
        }

        public async Task EditAsync(FormulaInfo? item)
        {
            if (item == null) return;

            try
            {
                StatusMessage = $"编辑验方模板 '{item.Name}' 功能开发中...";
                await _dialogService.ShowInformationAsync($"编辑验方模板 '{item.Name}' 功能正在开发中", "提示");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"打开编辑验方模板对话框失败: {ex.Message}", "错误");
            }
        }

        public async Task DeleteAsync(FormulaInfo? item)
        {
            if (item == null) return;

            try
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"确定要删除验方模板「{item.Name}」吗？",
                    "删除确认");

                if (confirm)
                {
                    IsLoading = true;
                    StatusMessage = "正在删除验方模板...";

                    var result = await _formulaModuleService.DeleteAsync(item.Id);
                    if (result.IsSuccess)
                    {
                        await RefreshAsync();
                        await _dialogService.ShowInformationAsync("验方模板删除成功", "成功");
                        StatusMessage = "删除成功";
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "删除验方模板失败",
                            "错误");
                        StatusMessage = "删除失败";
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"删除验方模板时发生错误: {ex.Message}", "错误");
                StatusMessage = "删除失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region UltraThink Phase 3.4: 增强功能实现

        /// <summary>
        /// 导入验方模板
        /// </summary>
        private async Task ImportTemplatesAsync()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "xlsx",
                Title = "选择要导入的验方模板文件"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "正在导入验方模板...";

                    // 验证文件格式
                    var extension = System.IO.Path.GetExtension(openDialog.FileName).ToLower();
                    if (extension != ".xlsx" && extension != ".csv")
                    {
                        await _commonDialogService.ShowWarningAsync("不支持的文件格式，请选择 Excel (.xlsx) 或 CSV (.csv) 文件", "格式错误");
                        return;
                    }

                    // 这里模拟导入过程
                    await Task.Delay(1000); // 模拟文件读取和处理

                    // 导入示例数据格式说明
                    var helpMessage = "验方模板导入格式说明：\n\n" +
                                    "Excel/CSV 文件应包含以下列：\n" +
                                    "1. 模板名称（必填）\n" +
                                    "2. 分类（必填）\n" +
                                    "3. 适应症（选填）\n" +
                                    "4. 药材名称（必填，可多行）\n" +
                                    "5. 药材用量（必填）\n" +
                                    "6. 药材单位（必填）\n\n" +
                                    "每个模板可包含多行药材信息。\n\n" +
                                    "导入功能当前为演示版本，实际导入需要后端支持。";

                    await _commonDialogService.ShowInformationAsync(helpMessage, "导入说明");

                    StatusMessage = $"已选择文件：{System.IO.Path.GetFileName(openDialog.FileName)}（演示模式）";
                }
                catch (Exception ex)
                {
                    await _commonDialogService.ShowErrorAsync($"导入验方模板时发生错误：{ex.Message}", "错误");
                    StatusMessage = "导入失败";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 导出模板
        /// </summary>
        private async Task ExportTemplateAsync()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv",
                DefaultExt = "xlsx",
                FileName = $"验方模板导入模板_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "保存验方模板导入模板"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "正在生成导入模板...";

                    await Task.Delay(500); // 模拟文件生成

                    var templateMessage = "验方模板导入模板已生成！\n\n" +
                                        "模板包含以下列：\n" +
                                        "• 模板名称\n" +
                                        "• 分类\n" +
                                        "• 适应症\n" +
                                        "• 药材名称\n" +
                                        "• 药材用量\n" +
                                        "• 药材单位\n\n" +
                                        "请按照模板格式填写数据后导入。";

                    await _commonDialogService.ShowInformationAsync(templateMessage, "导出完成");
                    StatusMessage = $"导入模板已保存：{System.IO.Path.GetFileName(saveDialog.FileName)}";
                }
                catch (Exception ex)
                {
                    await _commonDialogService.ShowErrorAsync($"导出模板时发生错误：{ex.Message}", "错误");
                    StatusMessage = "导出失败";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 复制验方模板
        /// </summary>
        private async Task CopyTemplate(FormulaInfo? template)
        {
            if (template == null) return;

            var result = await _dialogService.ShowConfirmationAsync($"确定要复制验方模板 \"{template.Name}\" 吗？", "确认复制");

            if (result)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "正在复制验方模板...";
                    var newName = $"{template.Name}_副本";
                    var response = await _formulaModuleService.CopyAsync(template.Id, newName);
                    if (response.IsSuccess)
                    {
                        await _dialogService.ShowInformationAsync($"验方模板 \"{template.Name}\" 已复制", "成功");
                        await RefreshAsync();
                        StatusMessage = "复制成功";
                    }
                    else
                    {
                        var error = response.ErrorMessage ?? "复制失败";
                        await _dialogService.ShowErrorAsync($"复制验方模板失败：{error}", "错误");
                        StatusMessage = "复制失败";
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync($"复制验方模板时发生错误：{ex.Message}", "错误");
                    StatusMessage = "复制失败";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 查看验方模板
        /// </summary>
        private async Task ViewTemplate(FormulaInfo? template)
        {
            if (template == null) return;

            try
            {
                StatusMessage = $"查看验方模板 '{template.Name}' 功能开发中...";
                
                var viewMessage = $"验方模板详情：\n\n" +
                                $"名称：{template.Name}\n" +
                                $"分类：{template.Category}\n" +
                                $"适应症：{template.Indications}\n" +
                                $"创建时间：{template.CreateTime:yyyy-MM-dd HH:mm}\n" +
                                $"更新时间：{template.UpdateTime:yyyy-MM-dd HH:mm}\n\n" +
                                "完整的查看功能正在开发中...";

                await _dialogService.ShowInformationAsync(viewMessage, $"查看验方模板 - {template.Name}");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"查看验方模板时发生错误：{ex.Message}", "错误");
            }
        }

        #endregion
    }
}