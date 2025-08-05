using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.DiagnosisTreatment;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Records.ViewModels
{
    /// <summary>
    /// 新增病历对话框视图模型
    /// </summary>
    public class AddRecordDialogViewModel : BindableBase
    {
        private readonly IRecordService _recordService;
        private readonly IPatientService _patientService;

        #region 属性

        private ObservableCollection<PatientDetailDto> _patients = new();
        private PatientDetailDto? _selectedPatient;
        private string _diagnosis = string.Empty;
        private string _chiefComplaint = string.Empty;
        private string _presentIllness = string.Empty;
        private string _treatmentAdvice = string.Empty;
        private string _diagnosisResultsText = string.Empty;
        private bool _isShared = false;
        private DateTime _recordTime = DateTime.Now;
        private bool _isLoading = false;

        /// <summary>患者列表</summary>
        public ObservableCollection<PatientDetailDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        /// <summary>选中的患者</summary>
        public PatientDetailDto? SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        /// <summary>诊断内容</summary>
        public string Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }

        /// <summary>主诉</summary>
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        /// <summary>现病史</summary>
        public string PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }

        /// <summary>诊疗建议</summary>
        public string TreatmentAdvice
        {
            get => _treatmentAdvice;
            set => SetProperty(ref _treatmentAdvice, value);
        }

        /// <summary>辨证结果（文本，逗号分隔）</summary>
        public string DiagnosisResultsText
        {
            get => _diagnosisResultsText;
            set => SetProperty(ref _diagnosisResultsText, value);
        }

        /// <summary>是否共享</summary>
        public bool IsShared
        {
            get => _isShared;
            set => SetProperty(ref _isShared, value);
        }

        /// <summary>病历时间</summary>
        public DateTime RecordTime
        {
            get => _recordTime;
            set => SetProperty(ref _recordTime, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        public Action<bool>? CloseDialogCallback { get; set; }

        public AddRecordDialogViewModel(IRecordService recordService, IPatientService patientService)
        {
            _recordService = recordService;
            _patientService = patientService;

            SaveCommand = new DelegateCommand(ExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 加载患者列表
            _ = LoadPatientsAsync();
        }

        private async System.Threading.Tasks.Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _patientService.GetAllAsync();
                
                if (result.IsSuccess && result.Data != null)
                {
                    Patients.Clear();
                    foreach (var patient in result.Data.Where(p => p.IsActive))
                    {
                        Patients.Add(patient);
                    }
                }
                else
                {
                    MessageBox.Show($"加载患者列表失败：{result.ErrorMessage}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载患者列表失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteSave()
        {
            // 验证必填字段
            if (SelectedPatient == null)
            {
                MessageBox.Show("请选择患者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Diagnosis))
            {
                MessageBox.Show("请输入诊断内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                MessageBox.Show("请输入主诉", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 解析辨证结果
                var diagnosisResults = string.IsNullOrWhiteSpace(DiagnosisResultsText) 
                    ? new System.Collections.Generic.List<string>()
                    : DiagnosisResultsText.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                var dto = new RecordCreateDto
                {
                    PatientId = SelectedPatient.Id,
                    RegistrationId = Guid.NewGuid(), // TODO: 实际应该关联真实的挂号记录
                    Diagnosis = Diagnosis,
                    ChiefComplaint = ChiefComplaint,
                    PresentIllness = PresentIllness,
                    TreatmentAdvice = TreatmentAdvice,
                    DiagnosisResults = diagnosisResults,
                    IsShared = IsShared,
                    RecordTime = RecordTime,
                    CreateTime = DateTime.Now,
                    CreatedBy = null // TODO: 获取当前登录医生ID
                };

                var result = await _recordService.AddAsync(dto);
                if (result.IsSuccess)
                {
                    MessageBox.Show("病历保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseDialogCallback?.Invoke(true);
                }
                else
                {
                    MessageBox.Show($"保存失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            CloseDialogCallback?.Invoke(false);
        }
    }
}