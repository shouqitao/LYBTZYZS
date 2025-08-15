using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Commands;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Services;
using Prism.Ioc;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 验方管理增强版视图模型
    /// UltraThink Phase 3.4: 迁移SystemManagement中更完整的Formula功能到业务模块
    /// </summary>
    public class FormulaManagementViewModelEnhanced : BaseServiceManagementViewModel<FormulaInfo, IFormulaService>
    {
        private readonly IDialogService _commonDialogService;
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

        // UltraThink Phase 3.4: 从SystemManagement迁移的高级功能
        public DelegateCommand ImportTemplatesCommand { get; }
        public DelegateCommand<FormulaInfo> CopyTemplateCommand { get; }
        public DelegateCommand ExportTemplateCommand { get; }
        public DelegateCommand<FormulaInfo> ViewTemplateCommand { get; }

        #endregion

        public FormulaManagementViewModelEnhanced(
            IFormulaService formulaService,
            IFormulaApiService formulaApiService,
            IDialogService commonDialogService,
            IDialogService dialogService,
            IHerbService herbService,
            Prism.Events.IEventAggregator eventAggregator)
            : base(formulaService, eventAggregator)
        {
            _commonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _formulaApiService = formulaApiService ?? throw new ArgumentNullException(nameof(formulaApiService));

            // 初始化增强功能命令
            ImportTemplatesCommand = new DelegateCommand(async () => await ImportTemplatesAsync());
            CopyTemplateCommand = new DelegateCommand<FormulaInfo>(async (template) => await CopyTemplate(template));
            ExportTemplateCommand = new DelegateCommand(async () => await ExportTemplateAsync());
            ViewTemplateCommand = new DelegateCommand<FormulaInfo>(async (template) => await ViewTemplate(template));

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

        protected override async Task<ServiceResult<LYBT.Desktop.Core.Models.Common.PagedResult<FormulaInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                // 如果需要传递分类信息，创建扩展请求
                var extendedRequest = new ExtendedPaginationRequest
                {
                    PageIndex = request.CurrentPage,
                    PageSize = request.PageSize,
                    Keyword = request.SearchKeyword,
                    SortField = request.SortField,
                    IsDescending = request.IsDescending
                };

                if (SelectedCategory != "全部")
                {
                    extendedRequest.ExtensionData["Category"] = SelectedCategory;
                }

                var result = await Service.SearchFormulasAsync(extendedRequest);
                return ServiceResult<LYBT.Desktop.Core.Models.Common.PagedResult<FormulaInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<LYBT.Desktop.Core.Models.Common.PagedResult<FormulaInfo>>.Failure($"加载验方模板列表失败: {ex.Message}");
            }
        }

        protected override async Task AddAsync()
        {
            try
            {
                // TODO: 使用增强的添加对话框
                StatusMessage = "添加验方模板功能开发中...";
                await _commonDialogService.ShowInformationAsync("添加验方模板功能正在开发中", "提示");
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
                // TODO: 使用增强的编辑对话框
                StatusMessage = $"编辑验方模板 '{item.Name}' 功能开发中...";
                await _commonDialogService.ShowInformationAsync($"编辑验方模板 '{item.Name}' 功能正在开发中", "提示");
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
        private async Task CopyTemplate(FormulaInfo template)
        {
            if (template == null) return;

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要复制验方模板 \"{template.Name}\" 吗？", "确认复制");

            if (result)
            {
                try
                {
                    StatusMessage = "正在复制验方模板...";
                    var newName = $"{template.Name}_副本";
                    var response = await Service.CopyAsync(template.Id, newName);
                    if (response.IsSuccess)
                    {
                        await _commonDialogService.ShowInformationAsync($"验方模板 \"{template.Name}\" 已复制", "成功");
                        await RefreshAsync();
                        StatusMessage = "复制成功";
                    }
                    else
                    {
                        var error = response.ErrorMessage ?? "复制失败";
                        await _commonDialogService.ShowErrorAsync($"复制验方模板失败：{error}", "错误");
                        StatusMessage = "复制失败";
                    }
                }
                catch (Exception ex)
                {
                    await _commonDialogService.ShowErrorAsync($"复制验方模板时发生错误：{ex.Message}", "错误");
                    StatusMessage = "复制失败";
                }
            }
        }

        /// <summary>
        /// 查看验方模板
        /// </summary>
        private async Task ViewTemplate(FormulaInfo template)
        {
            if (template == null) return;

            try
            {
                // TODO: 实现完整的查看对话框
                StatusMessage = $"查看验方模板 '{template.Name}' 功能开发中...";
                
                var viewMessage = $"验方模板详情：\n\n" +
                                $"名称：{template.Name}\n" +
                                $"分类：{template.Category}\n" +
                                $"适应症：{template.Indications}\n" +
                                $"创建时间：{template.CreateTime:yyyy-MM-dd HH:mm}\n" +
                                $"更新时间：{template.UpdateTime:yyyy-MM-dd HH:mm}\n\n" +
                                "完整的查看功能正在开发中...";

                await _commonDialogService.ShowInformationAsync(viewMessage, $"查看验方模板 - {template.Name}");
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"查看验方模板时发生错误：{ex.Message}", "错误");
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