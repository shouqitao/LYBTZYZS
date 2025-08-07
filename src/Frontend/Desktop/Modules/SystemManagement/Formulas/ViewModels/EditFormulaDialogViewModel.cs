using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.Shared.Models.Contracts.Formulas;

namespace LYBT.WPF.Client.Modules.SystemManagement.Formulas.ViewModels
{
    /// <summary>
    /// 编辑验方模板对话框视图模型
    /// </summary>
    public class EditFormulaDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IFormulaService _formulaService;
        private readonly IHerbService _herbService;
        private readonly Window _window;
        private Guid _templateId;

        #region Properties

        private string _templateName = string.Empty;
        public string TemplateName
        {
            get => _templateName;
            set => SetProperty(ref _templateName, value);
        }

        private string _category = string.Empty;
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private string _indications = string.Empty;
        public string Indications
        {
            get => _indications;
            set => SetProperty(ref _indications, value);
        }

        private string _efficacy = string.Empty;
        public string Efficacy
        {
            get => _efficacy;
            set => SetProperty(ref _efficacy, value);
        }

        private string _usage = string.Empty;
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        private string _remark = string.Empty;
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private ObservableCollection<HerbInfo> _availableHerbs = new();
        public ObservableCollection<HerbInfo> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        private HerbInfo? _selectedHerb;
        public HerbInfo? SelectedHerb
        {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        private string _searchHerbText = string.Empty;
        public string SearchHerbText
        {
            get => _searchHerbText;
            set => SetProperty(ref _searchHerbText, value);
        }

        private decimal _herbQuantity = 10;
        public decimal HerbQuantity
        {
            get => _herbQuantity;
            set => SetProperty(ref _herbQuantity, value);
        }

        private string _herbUnit = "g";
        public string HerbUnit
        {
            get => _herbUnit;
            set => SetProperty(ref _herbUnit, value);
        }

        private ObservableCollection<FormulaHerbItem> _templateHerbs = new();
        public ObservableCollection<FormulaHerbItem> TemplateHerbs
        {
            get => _templateHerbs;
            set => SetProperty(ref _templateHerbs, value);
        }

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand<FormulaHerbItem> RemoveHerbCommand { get; }

        #endregion

        public EditFormulaDialogViewModel(IFormulaService formulaService, IHerbService herbService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _formulaService = formulaService;
            _herbService = herbService;

            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave)
                .ObservesProperty(() => TemplateName)
                .ObservesProperty(() => Category)
                .ObservesProperty(() => Indications)
                .ObservesProperty(() => TemplateHerbs);
            
            CancelCommand = new DelegateCommand(ExecuteCancel);
            AddHerbCommand = new DelegateCommand(ExecuteAddHerb, CanExecuteAddHerb)
                .ObservesProperty(() => SelectedHerb)
                .ObservesProperty(() => HerbQuantity);
            
            RemoveHerbCommand = new DelegateCommand<FormulaHerbItem>(ExecuteRemoveHerb);

            // 获取当前窗口实例
            _window = Application.Current.Windows[Application.Current.Windows.Count - 1];

            // 加载可用药材
            _ = LoadAvailableHerbs();
        }

        public async void Initialize(Guid templateId)
        {
            _templateId = templateId;
            await LoadTemplateData();
        }

        private async System.Threading.Tasks.Task LoadTemplateData()
        {
            try
            {
                var response = await _formulaService.GetByIdAsync(_templateId);
                if (response.IsSuccess && response.Data != null)
                {
                    var template = response.Data;
                    TemplateName = template.Name;
                    Category = template.Category;
                    Indications = template.Indications ?? string.Empty;
                    Efficacy = template.Efficacy ?? string.Empty;
                    Usage = template.Usage ?? string.Empty;
                    Remark = template.Remark ?? string.Empty;

                    // 加载药材列表
                    TemplateHerbs.Clear();
                    if (template.Herbs != null)
                    {
                        foreach (var herb in template.Herbs)
                        {
                            TemplateHerbs.Add(new FormulaHerbItem
                            {
                                HerbId = herb.HerbId,
                                HerbName = herb.HerbName,
                                Quantity = herb.Quantity,
                                Unit = herb.Unit,
                            });
                        }
                    }
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"加载验方模板失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载验方模板失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                _window.Close();
            }
        }

        private async System.Threading.Tasks.Task LoadAvailableHerbs()
        {
            try
            {
                var herbs = await _herbService.GetAvailableHerbsAsync();
                AvailableHerbs.Clear();
                foreach (var herb in herbs.OrderBy(h => h.Name))
                {
                    AvailableHerbs.Add(herb);
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载药材列表失败: {ex.Message}", "错误");
            }
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(TemplateName) &&
                   !string.IsNullOrWhiteSpace(Category) &&
                   !string.IsNullOrWhiteSpace(Indications) &&
                   TemplateHerbs.Count > 0;
        }

        private async void ExecuteSave()
        {
            try
            {
                var dto = new FormulaUpdateDto
                {
                    Id = _templateId,
                    Name = TemplateName.Trim(),
                    Category = Category.Trim(),
                    Indications = Indications.Trim(),
                    Efficacy = Efficacy?.Trim(),
                    Usage = Usage?.Trim(),
                    Remark = Remark?.Trim(),
                    Herbs = TemplateHerbs.Select((h, index) => new FormulaHerbDto
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Quantity = h.Quantity,
                        Unit = h.Unit,
                        SortOrder = index
                    }).ToList()
                };

                var response = await _formulaService.UpdateAsync(dto);
                if (response.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("验方模板更新成功", "成功").GetAwaiter().GetResult();
                    
                    // 安全设置DialogResult
                    try
                    {
                        if (_window.IsVisible)
                        {
                            _window.DialogResult = true;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // 如果无法设置DialogResult，继续关闭窗口
                    }
                    _window.Close();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"更新验方模板失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"更新验方模板失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteCancel()
        {
            // 安全设置DialogResult
            try
            {
                if (_window.IsVisible)
                {
                    _window.DialogResult = false;
                }
            }
            catch (InvalidOperationException)
            {
                // 如果无法设置DialogResult，继续关闭窗口
            }
            _window.Close();
        }

        private bool CanExecuteAddHerb()
        {
            return SelectedHerb != null && HerbQuantity > 0;
        }

        private void ExecuteAddHerb()
        {
            if (SelectedHerb == null) return;

            // 检查是否已经添加
            if (TemplateHerbs.Any(h => h.HerbId == SelectedHerb.Id))
            {
                _commonDialogService.ShowInformationAsync($"药材 {SelectedHerb.Name} 已经在配方中", "提示").GetAwaiter().GetResult();
                return;
            }

            var herbItem = new FormulaHerbItem
            {
                HerbId = SelectedHerb.Id,
                HerbName = SelectedHerb.Name,
                Quantity = HerbQuantity,
                Unit = HerbUnit
                // SortOrder字段已按优化标准移除
            };

            TemplateHerbs.Add(herbItem);

            // 重置输入
            SelectedHerb = null;
            HerbQuantity = 10;
            HerbUnit = "g";
            SearchHerbText = string.Empty;
        }

        private void ExecuteRemoveHerb(FormulaHerbItem herb)
        {
            if (herb != null)
            {
                TemplateHerbs.Remove(herb);
                // 重新排序
                for (int i = 0; i < TemplateHerbs.Count; i++)
                {
                }
            }
        }
    }
}