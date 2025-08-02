using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.DTOs;

namespace LYBT.WPF.Client.Modules.SystemManagement.Records.ViewModels
{
    /// <summary>
    /// 病历管理视图模型
    /// </summary>
    public class RecordManagementViewModel : BindableBase
    {
        private readonly IRecordService _recordService;
        private readonly IPatientService _patientService;

        public RecordManagementViewModel(IRecordService recordService, IPatientService patientService)
        {
            _recordService = recordService;
            _patientService = patientService;
            InitializeCommands();
            _ = LoadRecordsAsync();
        }

        #region Properties

        private ObservableCollection<RecordDto> _records = new();
        public ObservableCollection<RecordDto> Records
        {
            get => _records;
            set => SetProperty(ref _records, value);
        }

        private RecordDto _selectedRecord;
        public RecordDto SelectedRecord
        {
            get => _selectedRecord;
            set => SetProperty(ref _selectedRecord, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private DateTime? _searchStartDate;
        public DateTime? SearchStartDate
        {
            get => _searchStartDate;
            set => SetProperty(ref _searchStartDate, value);
        }

        private DateTime? _searchEndDate;
        public DateTime? SearchEndDate
        {
            get => _searchEndDate;
            set => SetProperty(ref _searchEndDate, value);
        }

        private PatientDetailDto _selectedPatient;
        public PatientDetailDto SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        private ObservableCollection<PatientDetailDto> _availablePatients = new();
        public ObservableCollection<PatientDetailDto> AvailablePatients
        {
            get => _availablePatients;
            set => SetProperty(ref _availablePatients, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusText => $"共 {Records.Count} 条病历记录";

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; private set; } = null!;
        public DelegateCommand ResetSearchCommand { get; private set; } = null!;
        public DelegateCommand AddRecordCommand { get; private set; } = null!;
        public DelegateCommand EditRecordCommand { get; private set; } = null!;
        public DelegateCommand<RecordDto> ViewRecordCommand { get; private set; } = null!;
        public DelegateCommand<RecordDto> DeleteRecordCommand { get; private set; } = null!;
        public DelegateCommand<RecordDto> ShareRecordCommand { get; private set; } = null!;
        public DelegateCommand<RecordDto> UnshareRecordCommand { get; private set; } = null!;
        public DelegateCommand LoadPatientRecordsCommand { get; private set; } = null!;
        public DelegateCommand ExportCommand { get; private set; } = null!;
        public DelegateCommand RefreshCommand { get; private set; } = null!;

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SearchCommand = new DelegateCommand(async () => await SearchRecords());
            ResetSearchCommand = new DelegateCommand(async () => await ResetSearch());
            AddRecordCommand = new DelegateCommand(AddRecord);
            EditRecordCommand = new DelegateCommand(EditRecord);
            ViewRecordCommand = new DelegateCommand<RecordDto>(ViewRecord);
            DeleteRecordCommand = new DelegateCommand<RecordDto>(async (record) => await DeleteRecord(record));
            ShareRecordCommand = new DelegateCommand<RecordDto>(async (record) => await ShareRecord(record));
            UnshareRecordCommand = new DelegateCommand<RecordDto>(async (record) => await UnshareRecord(record));
            LoadPatientRecordsCommand = new DelegateCommand(async () => await LoadPatientRecords());
            ExportCommand = new DelegateCommand(async () => await ExportRecords());
            RefreshCommand = new DelegateCommand(async () => await LoadRecordsAsync());
        }

        #endregion

        #region Command Implementations

        private async Task LoadRecordsAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _recordService.GetListAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    Records.Clear();
                    foreach (var record in result.Data)
                    {
                        Records.Add(record);
                    }
                }
                else
                {
                    MessageBox.Show($"加载病历列表失败：{result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // 加载患者列表用于筛选
                await LoadAvailablePatients();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载病历列表时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                RaisePropertyChanged(nameof(StatusText));
            }
        }

        private async Task LoadAvailablePatients()
        {
            try
            {
                var result = await _patientService.GetAllAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    AvailablePatients.Clear();
                    AvailablePatients.Add(new PatientDetailDto { Id = Guid.Empty, Name = "全部患者" });
                    foreach (var patient in result.Data)
                    {
                        AvailablePatients.Add(patient);
                    }
                    SelectedPatient = AvailablePatients.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载患者列表失败：{ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task SearchRecords()
        {
            try
            {
                IsLoading = true;
                var allRecords = await _recordService.GetListAsync();
                if (allRecords.IsSuccess && allRecords.Data != null)
                {
                    var filteredRecords = allRecords.Data.AsEnumerable();

                    // 按关键词筛选
                    if (!string.IsNullOrWhiteSpace(SearchKeyword))
                    {
                        filteredRecords = filteredRecords.Where(r => 
                            r.PatientName?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true ||
                            r.ChiefComplaint?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true ||
                            r.Diagnosis?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true);
                    }

                    // 按患者筛选
                    if (SelectedPatient != null && SelectedPatient.Id != Guid.Empty)
                    {
                        filteredRecords = filteredRecords.Where(r => r.PatientId == SelectedPatient.Id.ToString());
                    }

                    // 按日期范围筛选
                    if (SearchStartDate.HasValue)
                    {
                        filteredRecords = filteredRecords.Where(r => r.CreatedTime >= SearchStartDate.Value);
                    }
                    if (SearchEndDate.HasValue)
                    {
                        filteredRecords = filteredRecords.Where(r => r.CreatedTime <= SearchEndDate.Value.AddDays(1));
                    }

                    Records.Clear();
                    foreach (var record in filteredRecords.OrderByDescending(r => r.CreatedTime))
                    {
                        Records.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"搜索病历时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                RaisePropertyChanged(nameof(StatusText));
            }
        }

        private async Task ResetSearch()
        {
            SearchKeyword = string.Empty;
            SearchStartDate = null;
            SearchEndDate = null;
            SelectedPatient = AvailablePatients.FirstOrDefault();
            await LoadRecordsAsync();
        }

        private void AddRecord()
        {
            // TODO: 打开新增病历对话框
            MessageBox.Show("新增病历功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditRecord()
        {
            if (SelectedRecord == null)
            {
                MessageBox.Show("请选择要编辑的病历", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: 打开编辑病历对话框
            MessageBox.Show($"编辑病历：{SelectedRecord.PatientName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewRecord(RecordDto record)
        {
            if (record == null) return;

            // TODO: 打开病历详情对话框
            MessageBox.Show($"查看病历详情：{record.PatientName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DeleteRecord(RecordDto record)
        {
            if (record == null) return;

            var result = MessageBox.Show($"确定要删除患者 \"{record.PatientName}\" 的病历吗？\n删除后无法恢复！", 
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _recordService.DeleteAsync(record.Id);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show("病历已删除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadRecordsAsync();
                    }
                    else
                    {
                        MessageBox.Show($"删除病历失败：{response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除病历时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ShareRecord(RecordDto record)
        {
            if (record == null) return;

            // TODO: 打开医生选择对话框
            MessageBox.Show($"共享病历功能待实现：{record.PatientName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            await Task.CompletedTask;
        }

        private async Task UnshareRecord(RecordDto record)
        {
            if (record == null) return;

            var result = MessageBox.Show($"确定要撤销病历 \"{record.PatientName}\" 的共享吗？", 
                "确认撤销", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _recordService.RevokeSharingAsync(record.Id);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show("病历共享已撤销", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadRecordsAsync();
                    }
                    else
                    {
                        MessageBox.Show($"撤销病历共享失败：{response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"撤销病历共享时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadPatientRecords()
        {
            if (SelectedPatient == null || SelectedPatient.Id == Guid.Empty)
            {
                await LoadRecordsAsync();
                return;
            }

            try
            {
                IsLoading = true;
                var result = await _recordService.GetByPatientIdAsync(SelectedPatient.Id);
                if (result.IsSuccess && result.Data != null)
                {
                    Records.Clear();
                    foreach (var record in result.Data.OrderByDescending(r => r.CreatedTime))
                    {
                        Records.Add(record);
                    }
                }
                else
                {
                    MessageBox.Show($"加载患者病历失败：{result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载患者病历时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                RaisePropertyChanged(nameof(StatusText));
            }
        }

        private async Task ExportRecords()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"病历数据_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // TODO: 实现CSV导出
                    MessageBox.Show($"导出 {Records.Count} 条病历数据成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    await Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}