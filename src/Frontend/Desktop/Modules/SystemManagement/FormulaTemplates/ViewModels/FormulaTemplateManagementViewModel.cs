using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using Prism.Commands;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services;
using Prism.Ioc;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.ViewModels
{
    /// <summary>
    /// 验方模板管理视图模型
    /// </summary>
    public class FormulaTemplateManagementViewModel : BaseManagementViewModel<FormulaTemplateInfo, IFormulaTemplateApiService>
    {
        private readonly ICommonDialogService _commonDialogService;
        private readonly IDialogService _dialogService;
        private readonly IHerbService _herbService;
        private readonly IFormulaTemplateService _formulaTemplateService;

        protected override string ModuleName => "验方模板管理";

        #region Properties

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
            set => SetProperty(ref _selectedCategory, value);
        }

        #endregion

        #region Commands

        public DelegateCommand ImportTemplatesCommand { get; }
        public DelegateCommand<FormulaTemplateInfo> CopyTemplateCommand { get; }

        #endregion

        public FormulaTemplateManagementViewModel(IFormulaTemplateApiService service,
            ICommonDialogService commonDialogService,
            IDialogService dialogService,
            IHerbService herbService,
            IFormulaTemplateService formulaTemplateService)
            : base(service)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
            _herbService = herbService;
            _formulaTemplateService = formulaTemplateService;
            // 初始化额外的命令
            ImportTemplatesCommand = new DelegateCommand(ImportTemplates);
            CopyTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(async (template) => await CopyTemplate(template));

            // 初始化分类
            InitializeCategories();
        }

        private void InitializeCategories()
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
            SelectedCategory = Categories.First();
        }

        #region 重写基类方法

        protected override async Task<ServiceResult<PagedResult<FormulaTemplateInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                var category = SelectedCategory == "全部" ? null : SelectedCategory;

                var response = await Service.GetFormulaTemplatesAsync(SearchKeyword, category);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var paginatedResult = response.Content;
                    
                    // 转换为前端模型
                    var formulaTemplateInfos = paginatedResult.Items.Select(dto => ConvertToFormulaTemplateInfo(dto)).ToList();

                    var result = new PagedResult<FormulaTemplateInfo>
                    {
                        Items = formulaTemplateInfos,
                        TotalCount = paginatedResult.TotalCount,
                        CurrentPage = paginatedResult.CurrentPage,
                        PageSize = paginatedResult.PageSize
                    };

                    return ServiceResult<PagedResult<FormulaTemplateInfo>>.Success(result);
                }
                else
                {
                    var error = response.Error?.Content ?? "获取验方模板列表失败";
                    return ServiceResult<PagedResult<FormulaTemplateInfo>>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载验方模板列表异常: {ex.Message}");
                return ServiceResult<PagedResult<FormulaTemplateInfo>>.Failure($"加载验方模板列表失败: {ex.Message}");
            }
        }

        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(FormulaTemplateInfo item)
        {
            try
            {
                var response = await Service.DeleteFormulaTemplateAsync(item.Id);
                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    var error = response.Error?.Content ?? "删除验方模板失败";
                    return ServiceResult<bool>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除验方模板失败: {ex.Message}");
            }
        }

        protected override string GetItemDisplayName(FormulaTemplateInfo item)
        {
            return item.Name ?? string.Empty;
        }

        protected override void ExecuteAdd()
        {
            try
            {
                // 直接使用注入的服务
                var dialog = new Views.AddFormulaTemplateDialog(_herbService, _formulaTemplateService, _commonDialogService);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开新增验方模板对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteEdit(FormulaTemplateInfo item)
        {
            if (item == null) return;

            try
            {
                // 创建编辑对话框的ViewModel
                var viewModel = new EditFormulaTemplateDialogViewModel(_formulaTemplateService, _herbService, _commonDialogService);
                var dialog = new Views.EditFormulaTemplateDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                dialog.Initialize(item.Id);
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开编辑验方模板对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteView(FormulaTemplateInfo item)
        {
            if (item == null) return;

            try
            {
                // 创建查看对话框的ViewModel
                var viewModel = new ViewFormulaTemplateDialogViewModel(_formulaTemplateService, _commonDialogService);
                var dialog = new Views.ViewFormulaTemplateDialog(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };
                
                dialog.Initialize(item.Id);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开验方模板详情对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }


        #endregion

        #region 额外功能

        private async void ImportTemplates()
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
                    
                    _commonDialogService.ShowInformationAsync(helpMessage, "导入说明").GetAwaiter().GetResult();
                    
                    // TODO: 实际导入时需要：
                    // 1. 使用 EPPlus 或类似库读取 Excel 文件
                    // 2. 解析文件内容并验证数据格式
                    // 3. 批量调用 API 创建验方模板
                    // 4. 显示导入进度和结果
                    
                    _commonDialogService.ShowInformationAsync($"已选择文件：{openDialog.FileName}\n\n实际导入功能需要后端API支持批量导入。", "提示").GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"导入验方模板时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task CopyTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要复制验方模板 \"{template.Name}\" 吗？", "确认复制");
            
            if (result)
            {
                try
                {
                    var newName = $"{template.Name}_副本";
                    var response = await Service.CopyFormulaTemplateAsync(template.Id, newName);
                    if (response.IsSuccessStatusCode)
                    {
                        _commonDialogService.ShowInformationAsync($"验方模板 \"{template.Name}\" 已复制", "成功").GetAwaiter().GetResult();
                        RefreshCommand.Execute();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "复制失败";
                        _commonDialogService.ShowErrorAsync($"复制验方模板失败：{error}", "错误").GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"复制验方模板时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
                }
            }
        }

        #endregion

        #region 辅助方法

        private FormulaTemplateInfo ConvertToFormulaTemplateInfo(FormulaTemplateDto dto)
        {
            return new FormulaTemplateInfo
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                Category = dto.Category ?? string.Empty,
                Indications = dto.Indications,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                // 药材信息需要从详情接口获取
                Herbs = new List<FormulaHerbItem>()
            };
        }

        #endregion
    }
}