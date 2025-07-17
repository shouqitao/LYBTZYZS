using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Prescriptions.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using LYBT.Common.Enums;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Profile {
    /// <summary>
    /// 经验方模板详情与编辑视图模型
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

        private PrescriptionItemDto? _selectedItem;
        public PrescriptionItemDto? SelectedItem {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public ObservableCollection<HerbDto> AllHerbs { get; } = new();

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
        /// <summary>
        /// 当前视图模式
        /// </summary>
        public ProfileMode Mode {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand RemoveHerbCommand { get; }

        public Action? CancelAction { get; set; }

        public FormulaTemplatesProfileViewModel(IFormulaTemplateService service, IHerbService herbService) {
            _service = service;
            _herbService = herbService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
            RemoveHerbCommand = new DelegateCommand(RemoveHerb, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
            _ = LoadAllHerbsAsync();
        }

        private async Task LoadAllHerbsAsync() {
            var list = await _herbService.GetListAsync();
            AllHerbs.Clear();
            foreach (var h in list)
                AllHerbs.Add(h);
        }

        public async Task LoadAsync(Guid? id = null, ProfileMode mode = ProfileMode.View) {
            Mode = mode;
            if (id.HasValue && id.Value != Guid.Empty) {
                var detail = await _service.GetByIdAsync(id.Value);
                if (detail != null) {
                    Template = detail;
                    Items.Clear();
                    foreach (var h in detail.Herbs)
                        Items.Add(new PrescriptionItemDto { HerbId = h.Id, HerbName = h.Name, Unit = h.Unit });
                } else {
                    Template = new FormulaTemplateDetailDto();
                    Items.Clear();
                }
            } else {
                Template = new FormulaTemplateDetailDto();
                Items.Clear();
            }

            switch (mode) {
                case ProfileMode.Create:
                    EditModeTitle = "新建模板信息";
                    IsEditable = true;
                    if (!Items.Any())
                        Items.Add(new PrescriptionItemDto());
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

        private void RemoveHerb() {
            if (SelectedItem != null)
                Items.Remove(SelectedItem);
        }

        private async Task SaveAsync() {
            // remove completely empty records
            for (int i = Items.Count - 1; i >= 0; i--) {
                var it = Items[i];
                if (it.HerbId == Guid.Empty && string.IsNullOrWhiteSpace(it.HerbName) && it.Quantity == 0)
                    Items.RemoveAt(i);
            }

            if (Items.Any(i => i.HerbId == Guid.Empty || string.IsNullOrWhiteSpace(i.HerbName) || i.Quantity <= 0)) {
                MessageBox.Show("有不完整数据，请确认", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Template.Herbs = Items.Select(i => new HerbDto { Id = i.HerbId, Name = i.HerbName, Unit = i.Unit }).ToList();
            bool ok;
            if (Template.Id == Guid.Empty) {
                ok = await _service.AddAsync(Template);
            } else {
                ok = await _service.UpdateAsync(Template);
            }
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
