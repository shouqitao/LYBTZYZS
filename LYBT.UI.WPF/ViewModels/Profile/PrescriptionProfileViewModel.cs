using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Module.Prescriptions.Dtos;
using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.Common.Enums;

namespace LYBT.UI.WPF.ViewModels.Profile {
    /// <summary>
    /// 处方详情与编辑
    /// </summary>
    public class PrescriptionProfileViewModel : BindableBase {
        private readonly IPrescriptionService _service;
        private readonly IHerbService _herbService;

        private PrescriptionDetailDto _prescription = new();
        public PrescriptionDetailDto Prescription {
            get => _prescription;
            set => SetProperty(ref _prescription, value);
        }

        public ObservableCollection<PrescriptionItemDto> Items { get; } = new();
        public ObservableCollection<HerbDto> AllHerbs { get; } = new();

        private PrescriptionItemDto? _selectedItem;
        public PrescriptionItemDto? SelectedItem {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private string _editModeTitle = "处方详细信息";
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
        public DelegateCommand AddItemCommand { get; }
        public DelegateCommand<object?> RemoveItemCommand { get; }

        public Action? CancelAction { get; set; }

        public PrescriptionProfileViewModel(IPrescriptionService service, IHerbService herbService) {
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
                if (detail != null) {
                    Prescription = detail;
                    Items.Clear();
                    foreach (var it in detail.Items)
                        Items.Add(it);
                } else {
                    Prescription = new PrescriptionDetailDto();
                    Items.Clear();
                }
            } else {
                Prescription = new PrescriptionDetailDto();
                Items.Clear();
            }

            switch (mode) {
                case ProfileMode.Create:
                    EditModeTitle = "新建处方信息";
                    IsEditable = true;
                    break;
                case ProfileMode.Edit:
                    EditModeTitle = "编辑处方信息";
                    IsEditable = true;
                    break;
                default:
                    EditModeTitle = "处方详细信息";
                    IsEditable = false;
                    break;
            }
        }

        private void AddItem() {
            Items.Add(new PrescriptionItemDto());
        }

        private void RemoveItem(PrescriptionItemDto? item) {
            if (item != null)
                Items.Remove(item);
        }

        private async Task SaveAsync() {
            Prescription.Items = Items.ToList();
            bool ok;
            if (Prescription.Id == Guid.Empty) {
                ok = await _service.AddAsync(Prescription);
            } else {
                ok = await _service.UpdateAsync(Prescription);
            }
            if (!ok)
                MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            else {
                Mode = ProfileMode.View;
                IsEditable = false;
                EditModeTitle = "处方详细信息";
                CancelAction?.Invoke();
            }
        }

        private void Cancel() {
            Mode = ProfileMode.View;
            IsEditable = false;
            EditModeTitle = "处方详细信息";
            CancelAction?.Invoke();
        }

        private async Task LoadHerbsAsync() {
            var list = await _herbService.GetListAsync();
            AllHerbs.Clear();
            foreach (var h in list)
                AllHerbs.Add(h);
        }
    }
}
