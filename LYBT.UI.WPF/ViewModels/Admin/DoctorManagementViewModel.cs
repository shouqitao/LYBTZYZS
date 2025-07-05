using LYBT.Common.Enums;
using LYBT.Module.Doctors.Dtos;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using Refit;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 医生管理视图模型
    /// </summary>
    public class DoctorManagementViewModel : BindableBase {
        public ObservableCollection<DoctorDto> Doctors { get; } = new();

        private DoctorDetailDto _editingDoctor = new();
        /// <summary>正在编辑的医生</summary>
        public DoctorDetailDto EditingDoctor { get => _editingDoctor; set => SetProperty(ref _editingDoctor, value); }

        private DoctorDto? _selectedDoctor;
        /// <summary>列表中选中的医生</summary>
        public DoctorDto? SelectedDoctor {
            get => _selectedDoctor;
            set {
                if (SetProperty(ref _selectedDoctor, value)) {
                    if (value != null)
                        _ = LoadSelectedDoctorAsync(value.Id);
                }
            }
        }

        private string _searchKeyword = string.Empty;
        /// <summary>搜索关键字</summary>
        public string SearchKeyword { get => _searchKeyword; set => SetProperty(ref _searchKeyword, value); }

        private string _editModeTitle = "新增医生";
        /// <summary>右侧编辑区标题</summary>
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        private bool _isEditable;
        /// <summary>是否可编辑</summary>
        public bool IsEditable { get => _isEditable; set => SetProperty(ref _isEditable, value); }

        public DelegateCommand AddDoctorCommand { get; }
        public DelegateCommand EditDoctorCommand { get; }
        public DelegateCommand SaveDoctorCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand SearchCommand { get; }

        private readonly IDoctorService _doctorService;

        public DoctorManagementViewModel(IDoctorService doctorService) {
            _doctorService = doctorService;
            AddDoctorCommand = new DelegateCommand(AddDoctor);
            EditDoctorCommand = new DelegateCommand(EditDoctor, () => SelectedDoctor != null).ObservesProperty(() => SelectedDoctor);
            SaveDoctorCommand = new DelegateCommand(async () => await SaveDoctor(), () => IsEditable).ObservesProperty(() => IsEditable);
            CancelCommand = new DelegateCommand(CancelEdit);
            SearchCommand = new DelegateCommand(async () => await LoadDoctors());
            _ = LoadDoctors();
        }

        private async Task LoadSelectedDoctorAsync(Guid id) {
            var detail = await _doctorService.GetByIdAsync(id);
            if (detail != null) {
                EditingDoctor = new DoctorDetailDto {
                    Id = detail.Id,
                    UserId = detail.UserId,
                    Birthday = detail.Birthday,
                    Title = detail.Title,
                    LicenseNumber = detail.LicenseNumber,
                    Specialty = detail.Specialty,
                    Status = detail.Status,
                    WorkStatus = detail.WorkStatus,
                    PinyinCode = detail.PinyinCode,
                    Remark = detail.Remark
                };
                EditModeTitle = "医生详情";
                IsEditable = false;
            }
        }

        private async Task LoadDoctors() {
            var list = await _doctorService.SearchAsync(SearchKeyword);
            Doctors.Clear();
            foreach (var d in list)
                Doctors.Add(d);
        }

        private void AddDoctor() {
            EditingDoctor = new DoctorDetailDto {
                UserId = Guid.Empty,
                Birthday = DateTime.Now,
                Title = DoctorTitle.Junior,
                Status = DoctorStatus.Active,
                WorkStatus = DoctorWorkStatus.Clinic,
                PinyinCode = string.Empty,
                LicenseNumber = string.Empty,
                Specialty = string.Empty,
                Remark = string.Empty
            };
            SelectedDoctor = null;
            EditModeTitle = "新增医生";
            IsEditable = true;
        }

        private void EditDoctor() {
            if (SelectedDoctor != null)
                IsEditable = true;
            EditModeTitle = "编辑医生";
        }

        private async Task SaveDoctor() {
            // 这里只校验必填项：UserId, Title, Status, Specialty
            if (EditingDoctor.UserId == Guid.Empty || string.IsNullOrWhiteSpace(EditingDoctor.Specialty)) {
                MessageBox.Show("用户ID和专长不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try {
                if (EditingDoctor.Id == Guid.Empty) {
                    var dto = new DoctorCreateDto {
                        UserId = EditingDoctor.UserId,
                        Birthday = EditingDoctor.Birthday,
                        Title = EditingDoctor.Title,
                        LicenseNumber = EditingDoctor.LicenseNumber,
                        Specialty = EditingDoctor.Specialty,
                        Status = EditingDoctor.Status,
                        WorkStatus = EditingDoctor.WorkStatus,
                        PinyinCode = EditingDoctor.PinyinCode,
                        Remark = EditingDoctor.Remark
                    };
                    var ok = await _doctorService.AddAsync(dto);
                    if (!ok) {
                        MessageBox.Show("新增医生失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                } else {
                    var dto = new DoctorEditDto {
                        Id = EditingDoctor.Id,
                        UserId = EditingDoctor.UserId,
                        Birthday = EditingDoctor.Birthday,
                        Title = EditingDoctor.Title,
                        LicenseNumber = EditingDoctor.LicenseNumber,
                        Specialty = EditingDoctor.Specialty,
                        Status = EditingDoctor.Status,
                        WorkStatus = EditingDoctor.WorkStatus,
                        PinyinCode = EditingDoctor.PinyinCode,
                        Remark = EditingDoctor.Remark
                    };
                    var ok = await _doctorService.UpdateAsync(dto);
                    if (!ok) {
                        MessageBox.Show("保存医生失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            } catch (Exception ex) {
                var msg = ex.Message;
                if (ex is ApiException apiEx && !string.IsNullOrEmpty(apiEx.Content)) {
                    try {
                        var doc = JsonDocument.Parse(apiEx.Content);
                        if (doc.RootElement.TryGetProperty("message", out var m))
                            msg = m.GetString() ?? msg;
                        else if (doc.RootElement.TryGetProperty("errors", out var errs))
                            msg = string.Join("; ", errs.EnumerateObject().SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString())));
                    } catch { }
                }
                MessageBox.Show($"操作失败：{msg}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await LoadDoctors();
            IsEditable = false;
            EditModeTitle = "医生详情";
        }

        private void CancelEdit() {
            if (SelectedDoctor != null)
                _ = LoadSelectedDoctorAsync(SelectedDoctor.Id);
            else
                EditingDoctor = new DoctorDetailDto();
            IsEditable = false;
            EditModeTitle = "医生详情";
        }
    }
}
