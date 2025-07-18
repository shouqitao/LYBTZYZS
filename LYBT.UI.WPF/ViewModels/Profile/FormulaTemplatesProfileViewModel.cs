using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Common.Enums;
using LYBT.Common.HerbCombination;
using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.Module.Herbs.Dtos;

namespace LYBT.UI.WPF.ViewModels.Profile {
    /// <summary>
    /// View model for formula template details and editing.
    /// </summary>
    public class FormulaTemplatesProfileViewModel : BindableBase {
        private readonly IFormulaTemplateService _service;
        private readonly IHerbService _herbService;

        private FormulaTemplateDetailDto _template = new();
        public FormulaTemplateDetailDto Template {
            get => _template;
            set => SetProperty(ref _template, value);
        }

        public HerbCombinationEditorViewModel HerbEditor { get; } = new() { Mode = HerbEditorMode.Template };

        public ObservableCollection<HerbDto> HerbCatalog { get; } = new();

        public ObservableCollection<string> FormulaNameCatalog { get; } = new();

        private string _editModeTitle = "模板详细信息";
        public string EditModeTitle {
            get => _editModeTitle;
            set => SetProperty(ref _editModeTitle, value);
        }

        private bool _isEditable;
        public bool IsEditable {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

        private ProfileMode _mode;
        /// <summary>Current view mode</summary>
        public ProfileMode Mode {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public Action? CancelAction { get; set; }

        public FormulaTemplatesProfileViewModel(IFormulaTemplateService service, IHerbService herbService) {
            _service = service;
            _herbService = herbService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public async Task LoadAsync(Guid? id = null, ProfileMode mode = ProfileMode.View) {
            Mode = mode;
            if (id.HasValue && id.Value != Guid.Empty) {
                var detail = await _service.GetByIdAsync(id.Value);
                Template = detail ?? new FormulaTemplateDetailDto();
            } else {
                Template = new FormulaTemplateDetailDto();
            }

            var herbs = await _herbService.GetListAsync();
            HerbCatalog.Clear();
            foreach (var h in herbs)
                HerbCatalog.Add(h);

            HerbEditor.Items.Clear();
            HerbEditor.FormulaName = Template.Name;
            foreach (var h in Template.Herbs)
                HerbEditor.Items.Add(new HerbCombinationItem { HerbId = h.Id.ToString(), Name = h.Name, Unit = h.Unit });

            var list = await _service.GetListAsync();
            FormulaNameCatalog.Clear();
            foreach (var t in list)
                if (!string.IsNullOrWhiteSpace(t.Name))
                    FormulaNameCatalog.Add(t.Name);

            switch (mode) {
                case ProfileMode.Create:
                    EditModeTitle = "新建模板信息";
                    IsEditable = true;
                    if (HerbEditor.Items.Count == 0)
                        HerbEditor.Items.Add(new HerbCombinationItem());
                    break;
                case ProfileMode.Edit:
                    EditModeTitle = "编辑模板信息";
                    IsEditable = true;
                    break;
                default:
                    EditModeTitle = "模板详细信息";
                    IsEditable = false;
                    break;
            }
        }

        private async Task SaveAsync() {
            HerbEditor.CleanEmptyRows();
            if (!HerbEditor.Validate(out var msg)) {
                MessageBox.Show(msg!, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Template.Name = HerbEditor.FormulaName;
            Template.Herbs = HerbEditor.Items.Select(i => new HerbDto {
                Id = Guid.TryParse(i.HerbId, out var id) ? id : Guid.Empty,
                Name = i.Name,
                Unit = i.Unit
            }).ToList();

            bool ok = Template.Id == Guid.Empty
                ? await _service.AddAsync(Template)
                : await _service.UpdateAsync(Template);
            if (!ok)
                MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            else {
                Mode = ProfileMode.View;
                IsEditable = false;
                EditModeTitle = "模板详细信息";
                CancelAction?.Invoke();
            }
        }

        private void Cancel() {
            Mode = ProfileMode.View;
            IsEditable = false;
            EditModeTitle = "模板详细信息";
            CancelAction?.Invoke();
        }
    }
}
