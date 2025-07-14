using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Profile {
    /// <summary>
    /// 经验方模板详情与编辑视图模型
    /// </summary>
    public class FormulaTemplatesProfileViewModel : BindableBase {
        private readonly IFormulaTemplateService _service;

        private FormulaTemplateDetailDto _template = new();
        public FormulaTemplateDetailDto Template {
            get => _template;
            set => SetProperty(ref _template, value);
        }

        public ObservableCollection<HerbDto> Herbs { get; } = new();

        private HerbDto? _selectedHerb;
        public HerbDto? SelectedHerb {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        private string _editModeTitle = "新增模板";
        public string EditModeTitle {
            get => _editModeTitle;
            set => SetProperty(ref _editModeTitle, value);
        }

        private bool _isEditable;
        public bool IsEditable {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand RemoveHerbCommand { get; }

        public Action? CancelAction { get; set; }

        public FormulaTemplatesProfileViewModel(IFormulaTemplateService service) {
            _service = service;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
            AddHerbCommand = new DelegateCommand(AddHerb);
            RemoveHerbCommand = new DelegateCommand(RemoveHerb, () => SelectedHerb != null).ObservesProperty(() => SelectedHerb);
        }

        public async Task LoadAsync(Guid? id = null) {
            if (id.HasValue && id.Value != Guid.Empty) {
                var detail = await _service.GetByIdAsync(id.Value);
                if (detail != null) {
                    Template = detail;
                    Herbs.Clear();
                    foreach (var h in detail.Herbs)
                        Herbs.Add(h);
                    EditModeTitle = "编辑模板";
                }
            } else {
                Template = new FormulaTemplateDetailDto();
                Herbs.Clear();
                EditModeTitle = "新增模板";
            }
        }

        private void AddHerb() {
            Herbs.Add(new HerbDto());
        }

        private void RemoveHerb() {
            if (SelectedHerb != null)
                Herbs.Remove(SelectedHerb);
        }

        private async Task SaveAsync() {
            Template.Herbs = Herbs.ToList();
            bool ok;
            if (Template.Id == Guid.Empty) {
                ok = await _service.AddAsync(Template);
            } else {
                ok = await _service.UpdateAsync(Template);
            }
            if (!ok)
                MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                CancelAction?.Invoke();
        }

        private void Cancel() => CancelAction?.Invoke();
    }
}
