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

namespace LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.ViewModels
{
    /// <summary>
    /// 验方模板管理视图模型（重构版）
    /// </summary>
    public class FormulaTemplateManagementViewModelRefactored : BaseManagementViewModel<FormulaTemplateInfo, IFormulaTemplateApiService>
    {
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

        public FormulaTemplateManagementViewModelRefactored(IFormulaTemplateApiService service)
            : base(service)
        {
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
                var dialog = new Views.AddFormulaTemplateDialog();
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开新增验方模板对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void ExecuteEdit(FormulaTemplateInfo item)
        {
            if (item == null) return;

            try
            {
                var dialog = new Views.EditFormulaTemplateDialog(item.Id);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开编辑验方模板对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void ExecuteView(FormulaTemplateInfo item)
        {
            if (item == null) return;

            try
            {
                var dialog = new Views.ViewFormulaTemplateDialog(item.Id);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开验方模板详情对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        #endregion

        #region 额外功能

        private void ImportTemplates()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "xlsx",
                Title = "选择要导入的验方模板文件"
            };

            if (openDialog.ShowDialog() == true)
            {
                // TODO: 实现导入逻辑
                MessageBox.Show($"导入验方模板功能待实现：{openDialog.FileName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task CopyTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;

            var result = MessageBox.Show($"确定要复制验方模板 \"{template.Name}\" 吗？", 
                "确认复制", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var newName = $"{template.Name}_副本";
                    var response = await Service.CopyFormulaTemplateAsync(template.Id, newName);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"验方模板 \"{template.Name}\" 已复制", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshCommand.Execute();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "复制失败";
                        MessageBox.Show($"复制验方模板失败：{error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制验方模板时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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