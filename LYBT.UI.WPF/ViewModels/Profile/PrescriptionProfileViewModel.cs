using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Module.Prescriptions.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.Common.Enums;

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

        public PrescriptionProfileViewModel(IPrescriptionService service) {
            _service = service;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
            AddItemCommand = new DelegateCommand(AddItem);
            RemoveItemCommand = new DelegateCommand<object?>(param => RemoveItem(param as PrescriptionItemDto));
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
                    EditModeTitle = "新增处方";
                    IsEditable = true;
                    break;
                case ProfileMode.Edit:
                    EditModeTitle = "编辑处方";
                    IsEditable = true;
                    break;
                default:
                    EditModeTitle = "处方详情";
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
                var dto = new PrescriptionCreateDto {
                    PatientId = Prescription.PatientId,
                    DoctorId = Prescription.DoctorId,
                    Diagnosis = Prescription.Diagnosis,
                    Remark = Prescription.Remark,
                    Status = Prescription.Status,
                    Items = Items.Select(i => new PrescriptionItemCreateDto {
                        HerbId = i.HerbId,
                        HerbName = i.HerbName,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Usage = i.Usage
                    }).ToList()
                };
                ok = await _service.AddAsync(dto);
            } else {
                var dto = new PrescriptionEditDto {
                    Id = Prescription.Id,
                    PatientId = Prescription.PatientId,
                    DoctorId = Prescription.DoctorId,
                    Diagnosis = Prescription.Diagnosis,
                    Remark = Prescription.Remark,
                    Status = Prescription.Status,
                    Items = Items.Select(i => new PrescriptionItemCreateDto {
                        HerbId = i.HerbId,
                        HerbName = i.HerbName,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Usage = i.Usage
                    }).ToList()
                };
                ok = await _service.UpdateAsync(dto);
            }
            if (!ok)
                MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            else {
                Mode = ProfileMode.View;
                IsEditable = false;
                EditModeTitle = "处方详情";
                CancelAction?.Invoke();
            }
        }

        private void Cancel() {
            Mode = ProfileMode.View;
            IsEditable = false;
            EditModeTitle = "处方详情";
            CancelAction?.Invoke();
        }
    }
}
