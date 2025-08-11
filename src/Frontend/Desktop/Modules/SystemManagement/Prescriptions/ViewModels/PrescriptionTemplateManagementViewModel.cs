using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Text.Json;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Admin.Prescriptions.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Admin.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方模板管理视图模型
    /// </summary>
    public class PrescriptionTemplateManagementViewModel : INotifyPropertyChanged
    {
        #region 字段

        private readonly IPrescriptionTemplateService _templateService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<PrescriptionTemplateManagementViewModel> _logger;
        
        private ObservableCollection<TemplateItemViewModel> _allTemplates = new();
        private ObservableCollection<TemplateItemViewModel> _filteredTemplates = new();
        private string _searchKeyword = string.Empty;
        private string _selectedCategory = "全部";
        private bool _showFrequentOnly;
        private bool _showPublicOnly;
        private bool _isLoading;

        #endregion

        #region 属性

        /// <summary>
        /// 所有模板
        /// </summary>
        public ObservableCollection<TemplateItemViewModel> AllTemplates
        {
            get => _allTemplates;
            set => SetProperty(ref _allTemplates, value);
        }

        /// <summary>
        /// 过滤后的模板
        /// </summary>
        public ObservableCollection<TemplateItemViewModel> FilteredTemplates
        {
            get => _filteredTemplates;
            set => SetProperty(ref _filteredTemplates, value);
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// 选中的分类
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// 仅显示常用
        /// </summary>
        public bool ShowFrequentOnly
        {
            get => _showFrequentOnly;
            set
            {
                if (SetProperty(ref _showFrequentOnly, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// 仅显示公开
        /// </summary>
        public bool ShowPublicOnly
        {
            get => _showPublicOnly;
            set
            {
                if (SetProperty(ref _showPublicOnly, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 模板总数
        /// </summary>
        public int TotalTemplateCount => AllTemplates.Count;

        /// <summary>
        /// 选中的模板数
        /// </summary>
        public int SelectedTemplateCount => AllTemplates.Count(t => t.IsSelected);

        /// <summary>
        /// 是否有选中的模板
        /// </summary>
        public bool HasSelectedTemplates => SelectedTemplateCount > 0;

        #endregion

        #region 命令

        public ICommand ApplyTemplateCommand { get; }
        public ICommand EditTemplateCommand { get; }
        public ICommand DeleteTemplateCommand { get; }
        public ICommand DuplicateTemplateCommand { get; }
        public ICommand BatchDeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 模板应用事件
        /// </summary>
        public event EventHandler<PrescriptionTemplate>? TemplateApplied;

        #endregion

        #region 构造函数

        public PrescriptionTemplateManagementViewModel(
            IPrescriptionTemplateService templateService,
            IDialogService dialogService,
            ILogger<PrescriptionTemplateManagementViewModel> logger)
        {
            _templateService = templateService;
            _dialogService = dialogService;
            _logger = logger;

            // 初始化命令
            ApplyTemplateCommand = new DelegateCommand<TemplateItemViewModel>(ExecuteApplyTemplate);
            EditTemplateCommand = new DelegateCommand<TemplateItemViewModel>(ExecuteEditTemplate);
            DeleteTemplateCommand = new DelegateCommand<TemplateItemViewModel>(async vm => await ExecuteDeleteTemplate(vm));
            DuplicateTemplateCommand = new DelegateCommand<TemplateItemViewModel>(ExecuteDuplicateTemplate);
            BatchDeleteCommand = new DelegateCommand(async () => await ExecuteBatchDelete());
            RefreshCommand = new DelegateCommand(async () => await LoadTemplatesAsync());
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载模板
        /// </summary>
        public async Task LoadTemplatesAsync()
        {
            try
            {
                IsLoading = true;
                var templates = await _templateService.GetAvailableTemplatesAsync();
                
                AllTemplates.Clear();
                foreach (var template in templates)
                {
                    AllTemplates.Add(new TemplateItemViewModel(template));
                }

                ApplyFilters();
                _logger.LogInformation($"加载模板成功，共{AllTemplates.Count}个");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载模板失败");
                await _dialogService.ShowErrorAsync($"加载模板失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 添加模板
        /// </summary>
        public async Task AddTemplate(PrescriptionTemplate template)
        {
            try
            {
                var success = await _templateService.CreateTemplateAsync(template);
                if (success)
                {
                    AllTemplates.Add(new TemplateItemViewModel(template));
                    ApplyFilters();
                    await _dialogService.ShowInformationAsync("模板创建成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("模板创建失败", "错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加模板失败");
                await _dialogService.ShowErrorAsync($"添加模板失败：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 按分类筛选
        /// </summary>
        public void FilterByCategory(string category)
        {
            SelectedCategory = category;
        }

        /// <summary>
        /// 导入模板
        /// </summary>
        public async Task ImportTemplatesAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    await _dialogService.ShowErrorAsync("文件不存在", "错误");
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var templates = JsonSerializer.Deserialize<List<PrescriptionTemplate>>(json);
                
                if (templates == null || !templates.Any())
                {
                    await _dialogService.ShowWarningAsync("文件中没有有效的模板", "警告");
                    return;
                }

                int successCount = 0;
                foreach (var template in templates)
                {
                    template.Id = Guid.NewGuid(); // 重新生成ID避免冲突
                    if (await _templateService.CreateTemplateAsync(template))
                    {
                        successCount++;
                    }
                }

                await LoadTemplatesAsync();
                await _dialogService.ShowInformationAsync($"成功导入{successCount}个模板", "导入完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入模板失败");
                await _dialogService.ShowErrorAsync($"导入模板失败：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 导出模板
        /// </summary>
        public async Task ExportTemplatesAsync(string filePath)
        {
            try
            {
                var selectedTemplates = AllTemplates
                    .Where(t => t.IsSelected)
                    .Select(t => t.Template)
                    .ToList();

                if (!selectedTemplates.Any())
                {
                    selectedTemplates = AllTemplates.Select(t => t.Template).ToList();
                }

                var json = JsonSerializer.Serialize(selectedTemplates, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json);
                await _dialogService.ShowInformationAsync($"成功导出{selectedTemplates.Count}个模板", "导出完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出模板失败");
                await _dialogService.ShowErrorAsync($"导出模板失败：{ex.Message}", "错误");
            }
        }

        #endregion

        #region 命令实现

        private void ExecuteApplyTemplate(TemplateItemViewModel? templateVm)
        {
            if (templateVm?.Template != null)
            {
                TemplateApplied?.Invoke(this, templateVm.Template);
            }
        }

        private void ExecuteEditTemplate(TemplateItemViewModel? templateVm)
        {
            if (templateVm?.Template == null) return;

            // TODO: 打开编辑器
            _dialogService.ShowInformationAsync("模板编辑功能开发中...", "提示").GetAwaiter().GetResult();
        }

        private async Task ExecuteDeleteTemplate(TemplateItemViewModel? templateVm)
        {
            if (templateVm?.Template == null) return;

            var result = await _dialogService.ShowConfirmationAsync(
                $"确定要删除模板【{templateVm.Template.Name}】吗？",
                "确认删除");

            if (result)
            {
                var success = await _templateService.DeleteTemplateAsync(templateVm.Template.Id);
                if (success)
                {
                    AllTemplates.Remove(templateVm);
                    ApplyFilters();
                    await _dialogService.ShowInformationAsync("模板删除成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("模板删除失败", "错误");
                }
            }
        }

        private void ExecuteDuplicateTemplate(TemplateItemViewModel? templateVm)
        {
            if (templateVm?.Template == null) return;

            var newTemplate = JsonSerializer.Deserialize<PrescriptionTemplate>(
                JsonSerializer.Serialize(templateVm.Template));
            
            if (newTemplate != null)
            {
                newTemplate.Id = Guid.NewGuid();
                newTemplate.Name = $"{newTemplate.Name} - 副本";
                newTemplate.IsPublic = false;
                newTemplate.UsageCount = 0;
                
                AddTemplate(newTemplate).GetAwaiter().GetResult();
            }
        }

        private async Task ExecuteBatchDelete()
        {
            var selectedTemplates = AllTemplates.Where(t => t.IsSelected).ToList();
            if (!selectedTemplates.Any())
            {
                await _dialogService.ShowWarningAsync("请先选择要删除的模板", "提示");
                return;
            }

            var result = await _dialogService.ShowConfirmationAsync(
                $"确定要删除选中的{selectedTemplates.Count}个模板吗？",
                "确认批量删除");

            if (result)
            {
                int successCount = 0;
                foreach (var templateVm in selectedTemplates)
                {
                    if (await _templateService.DeleteTemplateAsync(templateVm.Template.Id))
                    {
                        AllTemplates.Remove(templateVm);
                        successCount++;
                    }
                }

                ApplyFilters();
                await _dialogService.ShowInformationAsync($"成功删除{successCount}个模板", "删除完成");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 应用筛选
        /// </summary>
        private void ApplyFilters()
        {
            var filtered = AllTemplates.AsEnumerable();

            // 分类筛选
            if (SelectedCategory != "全部")
            {
                if (SelectedCategory == "个人")
                {
                    filtered = filtered.Where(t => t.Template.IsPersonal);
                }
                else
                {
                    filtered = filtered.Where(t => t.Template.Category == SelectedCategory);
                }
            }

            // 搜索筛选
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var keyword = SearchKeyword.ToLower();
                filtered = filtered.Where(t =>
                    t.Template.Name.ToLower().Contains(keyword) ||
                    t.Template.Diagnosis.ToLower().Contains(keyword) ||
                    t.Template.Syndrome.ToLower().Contains(keyword) ||
                    t.HerbPreview.ToLower().Contains(keyword));
            }

            // 常用筛选
            if (ShowFrequentOnly)
            {
                filtered = filtered.Where(t => t.Template.UsageCount > 5);
            }

            // 公开筛选
            if (ShowPublicOnly)
            {
                filtered = filtered.Where(t => t.Template.IsPublic);
            }

            FilteredTemplates = new ObservableCollection<TemplateItemViewModel>(filtered);
            
            OnPropertyChanged(nameof(TotalTemplateCount));
            OnPropertyChanged(nameof(SelectedTemplateCount));
            OnPropertyChanged(nameof(HasSelectedTemplates));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null!)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 模板项视图模型
    /// </summary>
    public class TemplateItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public PrescriptionTemplate Template { get; }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 药材预览文本
        /// </summary>
        public string HerbPreview
        {
            get
            {
                if (Template.Items == null || !Template.Items.Any())
                    return "暂无药材";

                var herbs = Template.Items.Take(5).Select(i => $"{i.HerbName} {i.Quantity}{i.Unit}");
                var preview = string.Join("、", herbs);
                
                if (Template.Items.Count > 5)
                    preview += $" 等{Template.Items.Count}味药";
                
                return preview;
            }
        }

        /// <summary>
        /// 转发模板属性
        /// </summary>
        public string Name => Template.Name;
        public string Category => Template.Category;
        public string Syndrome => Template.Syndrome;
        public string TreatmentPrinciple => Template.TreatmentPrinciple;
        public int UsageCount => Template.UsageCount;

        public TemplateItemViewModel(PrescriptionTemplate template)
        {
            Template = template;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}