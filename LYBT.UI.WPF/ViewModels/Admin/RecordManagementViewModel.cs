using LYBT.Module.Records.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 病历管理视图模型
    /// </summary>
    public class RecordManagementViewModel : BindableBase {
        private readonly IRecordService _recordService;

        private int _pageIndex = 1;
        public int PageIndex {
            get => _pageIndex;
            set => SetProperty(ref _pageIndex, value);
        }

        private int _totalCount;
        public int TotalCount {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int PageSize { get; set; } = 20;

        /// <summary>病历列表</summary>
        public ObservableCollection<RecordDto> Records { get; } = new();

        private RecordDto? _selectedRecord;
        /// <summary>列表中选中的病历</summary>
        public RecordDto? SelectedRecord {
            get => _selectedRecord;
            set {
                if (SetProperty(ref _selectedRecord, value)) {
                    if (value != null)
                        _ = LoadDetailAsync(value.Id);
                }
            }
        }

        private RecordDetailDto _editingRecord = new();
        /// <summary>右侧编辑区绑定的病历详情</summary>
        public RecordDetailDto EditingRecord {
            get => _editingRecord;
            set => SetProperty(ref _editingRecord, value);
        }

        private bool _isEditable;
        /// <summary>是否可编辑</summary>
        public bool IsEditable {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

        private string _editModeTitle = "病历详情";
        /// <summary>右侧编辑区标题</summary>
        public string EditModeTitle {
            get => _editModeTitle;
            set => SetProperty(ref _editModeTitle, value);
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ShareCommand { get; }
        public DelegateCommand RevokeShareCommand { get; }

        public RecordManagementViewModel(IRecordService recordService) {
            _recordService = recordService;
            RefreshCommand = new DelegateCommand(async () => await LoadAsync());
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedRecord != null).ObservesProperty(() => SelectedRecord);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => IsEditable).ObservesProperty(() => IsEditable);
            CancelCommand = new DelegateCommand(CancelEdit);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => SelectedRecord != null).ObservesProperty(() => SelectedRecord);
            ShareCommand = new DelegateCommand(async () => await ShareAsync(), () => SelectedRecord != null).ObservesProperty(() => SelectedRecord);
            RevokeShareCommand = new DelegateCommand(async () => await RevokeAsync(), () => SelectedRecord != null).ObservesProperty(() => SelectedRecord);
            _ = LoadAsync();
        }

        private async Task LoadAsync() {
            var list = await _recordService.GetListAsync();
            Records.Clear();
            foreach (var r in list)
                Records.Add(r);
        }

        private async Task LoadDetailAsync(Guid id) {
            var detail = await _recordService.GetByIdAsync(id);
            if (detail != null) {
                EditingRecord = detail;
                IsEditable = false;
                EditModeTitle = "病历详情";
            }
        }

        private void Add() {
            EditingRecord = new RecordDetailDto { RecordTime = DateTime.Now };
            SelectedRecord = null;
            IsEditable = true;
            EditModeTitle = "新增病历";
        }

        private void Edit() {
            if (SelectedRecord != null) {
                IsEditable = true;
                EditModeTitle = "编辑病历";
            }
        }

        private async Task SaveAsync() {
            bool ok;
            if (EditingRecord.Id == Guid.Empty) {
                var dto = new RecordCreateDto {
                    PatientId = EditingRecord.PatientId,
                    RegistrationId = EditingRecord.RegistrationId,
                    Diagnosis = EditingRecord.Diagnosis,
                    ChiefComplaint = EditingRecord.ChiefComplaint,
                    PresentIllness = EditingRecord.PresentIllness,
                    TreatmentAdvice = EditingRecord.TreatmentAdvice,
                    PrescriptionId = EditingRecord.PrescriptionId,
                    DiagnosisResults = EditingRecord.DiagnosisResults.ToList(),
                    HerbalFormula = EditingRecord.HerbalFormula,
                    TreatmentPlans = EditingRecord.TreatmentPlans,
                    IsShared = EditingRecord.IsShared,
                    SharedToDoctorIds = EditingRecord.SharedToDoctorIds,
                    CreatedBy = EditingRecord.CreatedBy,
                    CreatedTime = EditingRecord.CreatedTime,
                    RecordTime = EditingRecord.RecordTime
                };
                ok = await _recordService.AddAsync(dto);
            } else {
                var dto = new RecordEditDto {
                    Id = EditingRecord.Id,
                    Diagnosis = EditingRecord.Diagnosis,
                    ChiefComplaint = EditingRecord.ChiefComplaint,
                    PresentIllness = EditingRecord.PresentIllness,
                    TreatmentAdvice = EditingRecord.TreatmentAdvice,
                    PrescriptionId = EditingRecord.PrescriptionId,
                    DiagnosisResults = EditingRecord.DiagnosisResults.ToList(),
                    HerbalFormula = EditingRecord.HerbalFormula,
                    TreatmentPlans = EditingRecord.TreatmentPlans,
                    IsShared = EditingRecord.IsShared,
                    SharedToDoctorIds = EditingRecord.SharedToDoctorIds,
                    RecordTime = EditingRecord.RecordTime
                };
                ok = await _recordService.UpdateAsync(dto);
            }
            if (!ok)
                MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            await LoadAsync();
            IsEditable = false;
            EditModeTitle = "病历详情";
        }

        private void CancelEdit() {
            if (SelectedRecord != null)
                _ = LoadDetailAsync(SelectedRecord.Id);
            else {
                EditingRecord = new RecordDetailDto();
                IsEditable = false;
                EditModeTitle = "病历详情";
            }
        }

        private async Task DeleteAsync() {
            if (SelectedRecord == null)
                return;
            if (MessageBox.Show("确定删除该病历吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                var ok = await _recordService.DeleteAsync(SelectedRecord.Id);
                if (!ok)
                    MessageBox.Show("删除失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadAsync();
            }
        }

        private async Task ShareAsync() {
            if (SelectedRecord == null)
                return;
            var ok = await _recordService.MarkAsSharedAsync(SelectedRecord.Id, new List<string>());
            if (!ok)
                MessageBox.Show("共享失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            await LoadAsync();
        }

        private async Task RevokeAsync() {
            if (SelectedRecord == null)
                return;
            var ok = await _recordService.RevokeSharingAsync(SelectedRecord.Id);
            if (!ok)
                MessageBox.Show("取消共享失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            await LoadAsync();
        }
    }
}
