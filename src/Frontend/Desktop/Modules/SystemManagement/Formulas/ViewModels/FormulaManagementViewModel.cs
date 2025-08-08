using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Commands;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services;
using Prism.Ioc;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.SystemManagement.Formulas.ViewModels
{
    /// <summary>
    /// 验方模板管理视图模型
    /// </summary>
    public class FormulaManagementViewModel : BaseServiceManagementViewModel<FormulaInfo, IFormulaService>
    {
        private readonly ICommonDialogService _commonDialogService;
        private readonly IDialogService _dialogService;
        private readonly IHerbService _herbService;
        private readonly IFormulaApiService _formulaApiService;

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
        public DelegateCommand<FormulaInfo> CopyTemplateCommand { get; }

        #endregion

        public FormulaManagementViewModel(
            IFormulaService formulaService,
            IFormulaApiService formulaApiService,
            ICommonDialogService commonDialogService,
            IDialogService dialogService,
            IHerbService herbService,
            Prism.Events.IEventAggregator eventAggregator)
            : base(formulaService, eventAggregator)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
            _herbService = herbService;
            _formulaApiService = formulaApiService;
            // 初始化额外的命令
            ImportTemplatesCommand = new DelegateCommand(async () => await ImportTemplatesAsync());
            CopyTemplateCommand = new DelegateCommand<FormulaInfo>(async (template) => await CopyTemplate(template));

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

        protected override async Task<ServiceResult<PagedResult<FormulaInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                // 如果需要传递分类信息，创建扩展请求
                var extendedRequest = new ExtendedPaginationRequest
                {
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize,
                    SearchKeyword = request.SearchKeyword,
                    SortField = request.SortField,
                    SortAscending = request.SortAscending
                };

                if (SelectedCategory != "全部")
                {
                    extendedRequest.ExtensionData["Category"] = SelectedCategory;
                }

                var result = await Service.SearchFormulasAsync(extendedRequest);
                return ServiceResult<PagedResult<FormulaInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<FormulaInfo>>.Failure($"加载验方模板列表失败: {ex.Message}");
            }
        }

        protected override async Task AddAsync()
        {
            try
            {
                // 直接使用注入的服务
                var dialog = new Views.AddFormulaDialog(_herbService, Service, _commonDialogService);
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"打开新增验方模板对话框失败: {ex.Message}", "错误");
            }
        }

        protected override async Task EditAsync(FormulaInfo item)
        {
            if (item == null) return;

            try
            {
                // 创建编辑对话框的ViewModel
                var viewModel = new EditFormulaDialogViewModel(Service, _herbService, _commonDialogService);
                var dialog = new Views.EditFormulaDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                dialog.Initialize(item.Id);

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"打开编辑验方模板对话框失败: {ex.Message}", "错误");
            }
        }

        protected override async Task DeleteAsync(FormulaInfo item)
        {
            if (item == null) return;

            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要删除验方模板「{item.Name}」吗？",
                "删除确认");

            if (confirm)
            {
                var result = await Service.DeleteAsync(item.Id);
                if (result.IsSuccess)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync("验方模板删除成功", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "删除验方模板失败",
                        "错误");
                }
            }
        }

        /// <summary>
        /// 查看验方模板
        /// </summary>
        private async Task ViewAsync(FormulaInfo item)
        {
            if (item == null) return;

            try
            {
                // 创建查看对话框的ViewModel
                var viewModel = new ViewFormulaDialogViewModel(Service, _commonDialogService);
                var dialog = new Views.ViewFormulaDialog(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                dialog.Initialize(item.Id);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"打开验方模板详情对话框失败: {ex.Message}", "错误");
            }
        }


        #endregion

        #region 额外功能

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

        private async Task CopyTemplate(FormulaInfo template)
        {
            if (template == null) return;

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要复制验方模板 \"{template.Name}\" 吗？", "确认复制");

            if (result)
            {
                try
                {
                    var newName = $"{template.Name}_副本";
                    var response = await Service.CopyAsync(template.Id, newName);
                    if (response.IsSuccess)
                    {
                        _commonDialogService.ShowInformationAsync($"验方模板 \"{template.Name}\" 已复制", "成功").GetAwaiter().GetResult();
                        RefreshCommand.Execute();
                    }
                    else
                    {
                        var error = response.ErrorMessage ?? "复制失败";
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

        private FormulaInfo ConvertToFormulaInfo(FormulaDto dto)
        {
            return new FormulaInfo
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                Category = "其他", // FormulaDto没有Category属性，使用默认值
                Indications = dto.Effect ?? string.Empty, // 使用Effect字段
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                // 药材信息需要从详情接口获取
                Herbs = new List<FormulaHerbItem>()
            };
        }

        #endregion
    }
}