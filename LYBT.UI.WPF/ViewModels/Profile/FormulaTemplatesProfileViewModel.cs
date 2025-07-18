using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Common.Enums;
using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Prescriptions.Dtos;

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

        public ObservableCollection<PrescriptionItemDto> Items { get; } = new();
        public ObservableCollection<HerbDto> AllHerbs { get; } = new();

        private PrescriptionItemDto? _selectedItem;
        public PrescriptionItemDto? SelectedItem {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }



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
        public DelegateCommand AddItemCommand { get; }
        public DelegateCommand<object?> RemoveItemCommand { get; }

        public Action? CancelAction { get; set; }

        public FormulaTemplatesProfileViewModel(IFormulaTemplateService service, IHerbService herbService) {
            _service = service;
            _herbService = herbService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
            AddItemCommand = new DelegateCommand(AddItem);
            RemoveItemCommand = new DelegateCommand<object?>(param => RemoveItem(param as PrescriptionItemDto));
            _ = LoadHerbsAsync();
        }

        public async Task LoadAsync(Guid? id = null, ProfileMode mode = ProfileMode.View) {
            Mode = mode;
            if (id.HasValue && id.Value != Guid.Empty) {
                var detail = await _service.GetByIdAsync(id.Value);
                Template = detail ?? new FormulaTemplateDetailDto();
            } else {
                Template = new FormulaTemplateDetailDto();
            }

            Items.Clear();
            foreach (var h in Template.Herbs)
                Items.Add(new PrescriptionItemDto { HerbId = h.Id, HerbName = h.Name });

            var herbs = await _herbService.GetListAsync();
            HerbCatalog.Clear();
            AllHerbs.Clear();
            foreach (var h in herbs) {
                HerbCatalog.Add(h);
                AllHerbs.Add(h);
            }



            var list = await _service.GetListAsync();
            FormulaNameCatalog.Clear();
            foreach (var t in list)
                if (!string.IsNullOrWhiteSpace(t.Name))
                    FormulaNameCatalog.Add(t.Name);

            switch (mode) {
                case ProfileMode.Create:
                    EditModeTitle = "新建模板信息";
                    IsEditable = true;

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
            try {
                bool success;
                if (Template.Id == Guid.Empty) {
                    Template.Herbs = Items.Select(i => new HerbDto { Id = i.HerbId, Name = i.HerbName }).ToList();
                    success = await _service.AddAsync(Template);
                } else {
                    Template.Herbs = Items.Select(i => new HerbDto { Id = i.HerbId, Name = i.HerbName }).ToList();
                    success = await _service.UpdateAsync(Template);
                }
                if (!success)
                    MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                else {
                    Mode = ProfileMode.View;
                    IsEditable = false;
                    EditModeTitle = "模板详细信息";
                    CancelAction?.Invoke();
                }
            } catch (Exception ex) {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel() {
            Mode = ProfileMode.View;
            IsEditable = false;
            EditModeTitle = "模板详细信息";
            CancelAction?.Invoke();
        }

        private void AddItem() {
            Items.Add(new PrescriptionItemDto());
        }

        private void RemoveItem(PrescriptionItemDto? item) {
            if (item != null)
                Items.Remove(item);
        }

        private async Task LoadHerbsAsync() {
            var list = await _herbService.GetListAsync();
            AllHerbs.Clear();
            foreach (var h in list)
                AllHerbs.Add(h);
        }
    }
}
