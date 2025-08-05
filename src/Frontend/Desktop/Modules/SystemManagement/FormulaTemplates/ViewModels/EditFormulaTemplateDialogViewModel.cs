using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.Shared.Models.Contracts.FormulaTemplates;

namespace LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.ViewModels
{
    /// <summary>
    /// 编辑验方模板对话框视图模型
    /// </summary>
    public class EditFormulaTemplateDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IFormulaTemplateService _formulaTemplateService;
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

        private decimal _herbDosage = 10;
        public decimal HerbDosage
        {
            get => _herbDosage;
            set => SetProperty(ref _herbDosage, value);
        }

        private string _herbUnit = "g";
        public string HerbUnit
        {
            get => _herbUnit;
            set => SetProperty(ref _herbUnit, value);
        }

        private ObservableCollection<FormulaTemplateHerbItem> _templateHerbs = new();
        public ObservableCollection<FormulaTemplateHerbItem> TemplateHerbs
        {
            get => _templateHerbs;
            set => SetProperty(ref _templateHerbs, value);
        }

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand<FormulaTemplateHerbItem> RemoveHerbCommand { get; }

        #endregion

        public EditFormulaTemplateDialogViewModel(IFormulaTemplateService formulaTemplateService, IHerbService herbService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _formulaTemplateService = formulaTemplateService;
            _herbService = herbService;

            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave)
                .ObservesProperty(() => TemplateName)
                .ObservesProperty(() => Category)
                .ObservesProperty(() => Indications)
                .ObservesProperty(() => TemplateHerbs);
            
            CancelCommand = new DelegateCommand(ExecuteCancel);
            AddHerbCommand = new DelegateCommand(ExecuteAddHerb, CanExecuteAddHerb)
                .ObservesProperty(() => SelectedHerb)
                .ObservesProperty(() => HerbDosage);
            
            RemoveHerbCommand = new DelegateCommand<FormulaTemplateHerbItem>(ExecuteRemoveHerb);

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
                var response = await _formulaTemplateService.GetByIdAsync(_templateId);
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
                            TemplateHerbs.Add(new FormulaTemplateHerbItem
                            {
                                HerbId = herb.HerbId,
                                HerbName = herb.HerbName,
                                Dosage = herb.Dosage,
                                Unit = herb.Unit,
                                Remark = herb.Remark,
                                SortOrder = herb.SortOrder
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
                var dto = new FormulaTemplateUpdateDto
                {
                    Id = _templateId,
                    Name = TemplateName.Trim(),
                    Category = Category.Trim(),
                    Indications = Indications.Trim(),
                    Efficacy = Efficacy?.Trim(),
                    Usage = Usage?.Trim(),
                    Remark = Remark?.Trim(),
                    Herbs = TemplateHerbs.Select((h, index) => new FormulaTemplateHerbDto
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = h.Dosage,
                        Unit = h.Unit,
                        Remark = h.Remark,
                        SortOrder = index
                    }).ToList()
                };

                var response = await _formulaTemplateService.UpdateAsync(dto);
                if (response.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("验方模板更新成功", "成功").GetAwaiter().GetResult();
                    _window.DialogResult = true;
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
            _window.DialogResult = false;
            _window.Close();
        }

        private bool CanExecuteAddHerb()
        {
            return SelectedHerb != null && HerbDosage > 0;
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

            var herbItem = new FormulaTemplateHerbItem
            {
                HerbId = SelectedHerb.Id,
                HerbName = SelectedHerb.Name,
                Dosage = HerbDosage,
                Unit = HerbUnit,
                SortOrder = TemplateHerbs.Count
            };

            TemplateHerbs.Add(herbItem);

            // 重置输入
            SelectedHerb = null;
            HerbDosage = 10;
            HerbUnit = "g";
            SearchHerbText = string.Empty;
        }

        private void ExecuteRemoveHerb(FormulaTemplateHerbItem herb)
        {
            if (herb != null)
            {
                TemplateHerbs.Remove(herb);
                // 重新排序
                for (int i = 0; i < TemplateHerbs.Count; i++)
                {
                    TemplateHerbs[i].SortOrder = i;
                }
            }
        }
    }
}