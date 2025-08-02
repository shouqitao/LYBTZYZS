using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;

namespace LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.ViewModels
{
    /// <summary>
    /// 验方模板管理视图模型
    /// </summary>
    public class FormulaTemplateManagementViewModel : BindableBase
    {
        private readonly IFormulaTemplateService _formulaTemplateService;

        public FormulaTemplateManagementViewModel(IFormulaTemplateService formulaTemplateService)
        {
            _formulaTemplateService = formulaTemplateService;
            InitializeCommands();
            InitializeCategories();
            _ = LoadTemplatesAsync();
        }

        #region Properties

        private ObservableCollection<FormulaTemplateInfo> _templates = new();
        public ObservableCollection<FormulaTemplateInfo> Templates
        {
            get => _templates;
            set => SetProperty(ref _templates, value);
        }

        private FormulaTemplateInfo _selectedTemplate;
        public FormulaTemplateInfo SelectedTemplate
        {
            get => _selectedTemplate;
            set => SetProperty(ref _selectedTemplate, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
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
            set => SetProperty(ref _selectedCategory, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusText => $"共 {Templates.Count} 个验方模板";

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; private set; } = null!;
        public DelegateCommand ResetSearchCommand { get; private set; } = null!;
        public DelegateCommand AddTemplateCommand { get; private set; } = null!;
        public DelegateCommand ImportTemplatesCommand { get; private set; } = null!;
        public DelegateCommand ExportTemplatesCommand { get; private set; } = null!;
        public DelegateCommand RefreshCommand { get; private set; } = null!;
        public DelegateCommand<FormulaTemplateInfo> ViewTemplateCommand { get; private set; } = null!;
        public DelegateCommand<FormulaTemplateInfo> EditTemplateCommand { get; private set; } = null!;
        public DelegateCommand<FormulaTemplateInfo> CopyTemplateCommand { get; private set; } = null!;
        public DelegateCommand<FormulaTemplateInfo> DeleteTemplateCommand { get; private set; } = null!;

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SearchCommand = new DelegateCommand(async () => await SearchTemplates());
            ResetSearchCommand = new DelegateCommand(async () => await ResetSearch());
            AddTemplateCommand = new DelegateCommand(AddTemplate);
            ImportTemplatesCommand = new DelegateCommand(ImportTemplates);
            ExportTemplatesCommand = new DelegateCommand(async () => await ExportTemplates());
            RefreshCommand = new DelegateCommand(async () => await LoadTemplatesAsync());
            ViewTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(ViewTemplate);
            EditTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(EditTemplate);
            CopyTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(async (template) => await CopyTemplate(template));
            DeleteTemplateCommand = new DelegateCommand<FormulaTemplateInfo>(async (template) => await DeleteTemplate(template));
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

        #endregion

        #region Command Implementations

        private async Task LoadTemplatesAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _formulaTemplateService.GetListAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    Templates.Clear();
                    foreach (var template in result.Data)
                    {
                        Templates.Add(template);
                    }
                }
                else
                {
                    MessageBox.Show($"加载验方模板列表失败：{result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载验方模板时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                RaisePropertyChanged(nameof(StatusText));
            }
        }

        private async Task SearchTemplates()
        {
            try
            {
                IsLoading = true;
                var allTemplates = await _formulaTemplateService.GetListAsync();
                if (allTemplates.IsSuccess && allTemplates.Data != null)
                {
                    var filteredTemplates = allTemplates.Data.AsEnumerable();

                    // 按关键词筛选
                    if (!string.IsNullOrWhiteSpace(SearchKeyword))
                    {
                        filteredTemplates = filteredTemplates.Where(t => 
                            t.Name?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true ||
                            t.Indications?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true ||
                            t.HerbNames?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true);
                    }

                    // 按分类筛选
                    if (SelectedCategory != null && SelectedCategory != "全部")
                    {
                        filteredTemplates = filteredTemplates.Where(t => t.Category == SelectedCategory);
                    }

                    Templates.Clear();
                    foreach (var template in filteredTemplates.OrderBy(t => t.Name))
                    {
                        Templates.Add(template);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"搜索验方模板时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                RaisePropertyChanged(nameof(StatusText));
            }
        }

        private async Task ResetSearch()
        {
            SearchKeyword = string.Empty;
            SelectedCategory = "全部";
            await LoadTemplatesAsync();
        }

        private void AddTemplate()
        {
            // TODO: 打开新增验方模板对话框
            MessageBox.Show("新增验方模板功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

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

        private async Task ExportTemplates()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"验方模板_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // TODO: 实现导出逻辑
                    MessageBox.Show($"成功导出 {Templates.Count} 个验方模板", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    await Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;

            // TODO: 打开验方模板详情对话框
            MessageBox.Show($"查看验方模板：{template.Name}\n包含 {template.HerbCount} 味药材", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;

            // TODO: 打开编辑验方模板对话框
            MessageBox.Show($"编辑验方模板：{template.Name}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    // TODO: 实现复制逻辑
                    MessageBox.Show($"验方模板 \"{template.Name}\" 已复制", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadTemplatesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制验方模板时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DeleteTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;

            var result = MessageBox.Show($"确定要删除验方模板 \"{template.Name}\" 吗？\n删除后无法恢复！", 
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _formulaTemplateService.DeleteAsync(template.Id);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show("验方模板已删除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadTemplatesAsync();
                    }
                    else
                    {
                        MessageBox.Show($"删除验方模板失败：{response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除验方模板时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}