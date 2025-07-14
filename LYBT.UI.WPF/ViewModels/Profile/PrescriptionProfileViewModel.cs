using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Module.Prescriptions.Dtos;
using LYBT.UI.WPF.Interfaces;

namespace LYBT.UI.WPF.ViewModels.Profile {
    /// <summary>
    /// 处方详情与编辑
    /// </summary>
    public class PrescriptionProfileViewModel : BindableBase {
        private readonly IPrescriptionService _service;

        private PrescriptionDetailDto _prescription = new();
        public PrescriptionDetailDto Prescription {
            get => _prescription;
            set => SetProperty(ref _prescription, value);
        }

        public ObservableCollection<PrescriptionItemDto> Items { get; } = new();

        private PrescriptionItemDto? _selectedItem;
        public PrescriptionItemDto? SelectedItem {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private string _editModeTitle = "新增处方";
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
        public DelegateCommand AddItemCommand { get; }
        public DelegateCommand<object?> RemoveItemCommand { get; }

        public Action? CancelAction { get; set; }

        public PrescriptionProfileViewModel(IPrescriptionService service) {
            _service = service;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
            AddItemCommand = new DelegateCommand(AddItem);
            RemoveItemCommand = new DelegateCommand<object?>(param => RemoveItem(param as PrescriptionItemDto));
        }

        public async Task LoadAsync(Guid? id = null) {
            if (id.HasValue && id.Value != Guid.Empty) {
                var detail = await _service.GetByIdAsync(id.Value);
                if (detail != null) {
                    Prescription = detail;
                    Items.Clear();
                    foreach (var it in detail.Items)
                        Items.Add(it);
                    EditModeTitle = "编辑处方";
                }
            } else {
                Prescription = new PrescriptionDetailDto();
                Items.Clear();
                EditModeTitle = "新增处方";
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
            else
                CancelAction?.Invoke();
        }

        private void Cancel() => CancelAction?.Invoke();
    }
}
