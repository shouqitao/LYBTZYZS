using LYBT.Common.Enums;
using LYBT.Module.Doctors.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.ViewModels.Main;
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

        private DoctorDto? _selectedDoctor;
        /// <summary>列表中选中的医生</summary>
        public DoctorDto? SelectedDoctor {
            get => _selectedDoctor;
            set {
                if (SetProperty(ref _selectedDoctor, value)) {
                    if (value != null)
                        _ = UpdateDoctorProfileViewModel(value.Id);
                    IsEditable = false; // 选中后不可编辑
                }
            }
        }

        private string _searchKeyword = string.Empty;
        /// <summary>搜索关键字</summary>
        public string SearchKeyword { get => _searchKeyword; set => SetProperty(ref _searchKeyword, value); }

        private string _editModeTitle = "新增医生档案";
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
        public DoctorProfileViewModel DoctorProfileViewModel { get; }

        public DoctorManagementViewModel(IDoctorService doctorService) {
            _doctorService = doctorService;
            DoctorProfileViewModel = new DoctorProfileViewModel(_doctorService, null, null); // 依赖注入可根据实际情况调整
            DoctorProfileViewModel.CancelAction = CancelEdit;
            AddDoctorCommand = new DelegateCommand(AddDoctor);
            EditDoctorCommand = new DelegateCommand(EditDoctor, () => SelectedDoctor != null).ObservesProperty(() => SelectedDoctor);
            SaveDoctorCommand = new DelegateCommand(async () => await SaveDoctor(), () => IsEditable).ObservesProperty(() => IsEditable);
            CancelCommand = new DelegateCommand(CancelEdit);
            SearchCommand = new DelegateCommand(async () => await LoadDoctors());
            _ = LoadDoctors();
        }

        /// <summary>
        /// 选中左侧医生时，右侧档案区自动显示详情且只读
        /// </summary>
        private async Task UpdateDoctorProfileViewModel(Guid doctorId) {
            var detail = await _doctorService.GetByIdAsync(doctorId);
            if (detail != null) {
                DoctorProfileViewModel.Doctor = detail;
                DoctorProfileViewModel.EditModeTitle = "医生详情";
                DoctorProfileViewModel.IsEditable = false;
            }
        }

        private async Task LoadDoctors() {
            var list = await _doctorService.SearchAsync(SearchKeyword);
            Doctors.Clear();
            foreach (var d in list)
                Doctors.Add(d);
        }

        /// <summary>
        /// 新增医生档案
        /// </summary>
        private void AddDoctor() {
            var newDoctor = new DoctorDetailDto {
                UserId = Guid.Empty,
                Gender = Gender.Unknown,
                Birthday = DateTime.Now,
                Title = DoctorTitle.Junior,
                Status = DoctorStatus.Active,
                WorkStatus = DoctorWorkStatus.Clinic,
                PinyinCode = string.Empty,
                LicenseNumber = string.Empty,
                Specialty = string.Empty,
                ContactNumber = string.Empty,
                Remark = string.Empty
            };
            DoctorProfileViewModel.Doctor = newDoctor;
            DoctorProfileViewModel.EditModeTitle = "新增医生档案";
            DoctorProfileViewModel.IsEditable = true;
            SelectedDoctor = null;
        }

        /// <summary>
        /// 编辑医生档案
        /// </summary>
        private void EditDoctor() {
            if (SelectedDoctor != null) {
                DoctorProfileViewModel.IsEditable = true;
                DoctorProfileViewModel.EditModeTitle = "编辑医生档案";
            }
        }

        /// <summary>
        /// 保存医生档案
        /// </summary>
        private async Task SaveDoctor() {
            // 这里只校验必填项：UserId, Title, Status, Specialty
            var editing = DoctorProfileViewModel.Doctor;
            if (editing.UserId == Guid.Empty || string.IsNullOrWhiteSpace(editing.Specialty)) {
                MessageBox.Show("用户ID和专长不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try {
                bool ok;
                if (editing.Id == Guid.Empty) {
                    ok = await _doctorService.AddAsync(editing);
                } else {
                    ok = await _doctorService.UpdateAsync(editing);
                }
                if (!ok) {
                    MessageBox.Show(editing.Id == Guid.Empty ? "新增医生失败" : "保存医生失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
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
            DoctorProfileViewModel.IsEditable = false;
            DoctorProfileViewModel.EditModeTitle = "医生详情";
        }

        /// <summary>
        /// 取消编辑
        /// </summary>
        private void CancelEdit() {
            if (SelectedDoctor != null)
                _ = UpdateDoctorProfileViewModel(SelectedDoctor.Id);
            else
                DoctorProfileViewModel.Doctor = new DoctorDetailDto();
            DoctorProfileViewModel.IsEditable = false;
            DoctorProfileViewModel.EditModeTitle = "医生详情";
        }
    }
}