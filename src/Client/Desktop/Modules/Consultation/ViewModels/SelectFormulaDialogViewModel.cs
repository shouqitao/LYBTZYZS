using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Shared.Interfaces.Services;

// UltraThink重构: 统一FormulaInfo和FormulaDto，使用FormulaDto作为统一模型
using LYBT.Desktop.Core.Models.Formulas;
using IFormulaService = LYBT.Shared.Interfaces.Services.IFormulaService;
namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 验方选择对话框视图模型
    /// </summary>
    public class SelectFormulaDialogViewModel : BindableBase
    {
        private readonly IFormulaService _formulaService;
        private readonly ICustomDialogService _dialogService;
        
        private bool _isLoading;
        private string _searchKeyword = string.Empty;
        private string _selectedCategory = "全部";
        private FormulaInfo? _selectedFormula;
        private ObservableCollection<FormulaInfo> _formulas;
        private ObservableCollection<string> _categories;
        private string _previewText = string.Empty;
        
        #region 属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
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
                    SearchCommand.Execute();
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
                    FilterByCategory();
                }
            }
        }

        /// <summary>
        /// 选中的验方
        /// </summary>
        public FormulaInfo? SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                if (SetProperty(ref _selectedFormula, value))
                {
                    UpdatePreview();
                    ConfirmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 验方列表
        /// </summary>
        public ObservableCollection<FormulaInfo> Formulas
        {
            get => _formulas;
            set => SetProperty(ref _formulas, value);
        }

        /// <summary>
        /// 分类列表
        /// </summary>
        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        /// <summary>
        /// 预览文本
        /// </summary>
        public string PreviewText
        {
            get => _previewText;
            set => SetProperty(ref _previewText, value);
        }

        /// <summary>
        /// 是否显示空状态
        /// </summary>
        public bool ShowEmptyState => !IsLoading && (_formulas == null || !_formulas.Any());

        /// <summary>
        /// 空状态消息
        /// </summary>
        public string EmptyStateMessage
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                    return $"未找到包含 \"{SearchKeyword}\" 的验方";
                if (SelectedCategory != "全部")
                    return $"分类 \"{SelectedCategory}\" 下暂无验方";
                return "暂无验方数据";
            }
        }

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand<FormulaInfo> SelectFormulaCommand { get; }
        public DelegateCommand<FormulaInfo> ViewDetailsCommand { get; }

        #endregion

        #region 回调

        public Action<FormulaInfo>? OnFormulaSelected { get; set; }
        public Action? OnCancelled { get; set; }

        #endregion

        private List<FormulaInfo> _allFormulas = new();

        public SelectFormulaDialogViewModel(
            IFormulaService formulaService,
            ICustomDialogService dialogService)
        {
            _formulaService = formulaService;
            _dialogService = dialogService;
            
            _formulas = new ObservableCollection<FormulaInfo>();
            _categories = new ObservableCollection<string> { "全部" };

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            RefreshCommand = new DelegateCommand(async () => await LoadFormulasAsync());
            ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            SelectFormulaCommand = new DelegateCommand<FormulaInfo>(ExecuteSelectFormula);
            ViewDetailsCommand = new DelegateCommand<FormulaInfo>(ExecuteViewDetails);

            // 初始加载
            Task.Run(async () => await LoadFormulasAsync());
        }

        private async Task LoadFormulasAsync()
        {
            try
            {
                IsLoading = true;

                // 加载验方列表
                var result = await _formulaService.GetFormulasAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    // 使用AutoMapper转换FormulaDto到FormulaInfo
                    // 临时手动转换，因为AutoMapper需要配置
                    _allFormulas = result.Data.Select(dto => new FormulaInfo
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        Category = "其他", // 默认分类
                        Description = dto.Effect,
                        Source = dto.Source ?? "",
                        CreateTime = dto.CreateTime,
                        UpdateTime = dto.UpdateTime,
                        // Status = CommonStatus.Enabled, // 默认启用状态
                        Remark = dto.Remark
                    }).ToList();
                    
                    // 提取分类
                    var categories = _allFormulas
                        .Where(f => !string.IsNullOrWhiteSpace(f.Category))
                        .Select(f => f.Category)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();
                    
                    Categories.Clear();
                    Categories.Add("全部");
                    foreach (var category in categories)
                    {
                        Categories.Add(category);
                    }
                    
                    // 显示所有验方
                    Formulas = new ObservableCollection<FormulaInfo>(_allFormulas);
                    RaisePropertyChanged(nameof(ShowEmptyState));
                    RaisePropertyChanged(nameof(EmptyStateMessage));
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "加载验方列表失败",
                        "加载失败");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    $"加载验方列表时发生错误：{ex.Message}",
                    "系统错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task ExecuteSearchAsync()
        {
            try
            {
                IsLoading = true;
                
                if (string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    // 如果搜索词为空，显示当前分类的所有验方
                    FilterByCategory();
                }
                else
                {
                    // 在当前分类中搜索
                    var filteredFormulas = _allFormulas.Where(f =>
                    {
                        // 分类筛选
                        if (SelectedCategory != "全部" && f.Category != SelectedCategory)
                            return false;
                        
                        // 关键词搜索（名称、描述、适应症）
                        var keyword = SearchKeyword.ToLower();
                        return f.Name.ToLower().Contains(keyword) ||
                               (!string.IsNullOrWhiteSpace(f.Description) && f.Description.ToLower().Contains(keyword)) ||
                               (!string.IsNullOrWhiteSpace(f.Indications) && f.Indications.ToLower().Contains(keyword));
                    }).ToList();
                    
                    Formulas = new ObservableCollection<FormulaInfo>(filteredFormulas);
                    RaisePropertyChanged(nameof(ShowEmptyState));
                    RaisePropertyChanged(nameof(EmptyStateMessage));
                }
            }
            finally
            {
                IsLoading = false;
            }

            return Task.CompletedTask;
        }

        private void FilterByCategory()
        {
            if (SelectedCategory == "全部")
            {
                Formulas = new ObservableCollection<FormulaInfo>(_allFormulas);
            }
            else
            {
                var filtered = _allFormulas.Where(f => f.Category == SelectedCategory).ToList();
                Formulas = new ObservableCollection<FormulaInfo>(filtered);
            }
            
            RaisePropertyChanged(nameof(ShowEmptyState));
            RaisePropertyChanged(nameof(EmptyStateMessage));
        }

        private void UpdatePreview()
        {
            if (SelectedFormula == null)
            {
                PreviewText = "请选择一个验方查看详情";
                return;
            }

            var preview = $"【{SelectedFormula.Name}】\n\n";
            
            if (!string.IsNullOrWhiteSpace(SelectedFormula.Category))
                preview += $"分类：{SelectedFormula.Category}\n";
            
            if (!string.IsNullOrWhiteSpace(SelectedFormula.Source))
                preview += $"来源：{SelectedFormula.Source}\n";
            
            if (!string.IsNullOrWhiteSpace(SelectedFormula.Indications))
                preview += $"适应症：{SelectedFormula.Indications}\n";
            
            preview += $"\n组成（{SelectedFormula.HerbCount}味）：\n";
            
            foreach (var herb in SelectedFormula.Herbs.Take(10))
            {
                preview += $"  {herb.HerbName} {herb.Quantity}{herb.Unit}";
                if (!string.IsNullOrWhiteSpace(herb.ProcessingMethod))
                    preview += $"（{herb.ProcessingMethod}）";
                preview += "\n";
            }
            
            if (SelectedFormula.Herbs.Count > 10)
                preview += $"  ... 还有 {SelectedFormula.Herbs.Count - 10} 味药材\n";
            
            if (!string.IsNullOrWhiteSpace(SelectedFormula.Description))
                preview += $"\n功效：{SelectedFormula.Description}\n";
            
            if (!string.IsNullOrWhiteSpace(SelectedFormula.DosageInstruction))
                preview += $"用法：{SelectedFormula.DosageInstruction}\n";
            
            if (!string.IsNullOrWhiteSpace(SelectedFormula.Contraindications))
                preview += $"禁忌：{SelectedFormula.Contraindications}\n";
            
            preview += $"\n参考价格：¥{SelectedFormula.TotalPrice:F2}";

            PreviewText = preview;
        }

        private void ExecuteSelectFormula(FormulaInfo formula)
        {
            SelectedFormula = formula;
        }

        private void ExecuteViewDetails(FormulaInfo formula)
        {
            if (formula == null) return;
            
            // 选中并更新预览
            SelectedFormula = formula;
            
            // 可以在这里添加更多详情展示逻辑
            _dialogService.ShowInformationAsync(
                PreviewText,
                $"验方详情 - {formula.Name}");
        }

        private bool CanExecuteConfirm()
        {
            return SelectedFormula != null;
        }

        private void ExecuteConfirm()
        {
            if (SelectedFormula != null)
            {
                OnFormulaSelected?.Invoke(SelectedFormula);
            }
        }

        private void ExecuteCancel()
        {
            OnCancelled?.Invoke();
        }
    }

    /// <summary>
    /// 验方分类选项
    /// </summary>
    public class FormulaCategoryOption
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}