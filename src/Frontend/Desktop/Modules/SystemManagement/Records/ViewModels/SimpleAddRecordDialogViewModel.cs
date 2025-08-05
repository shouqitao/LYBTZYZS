using System;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Shared.Models.Contracts.Records;

using LYBT.WPF.Client.Core.Interfaces.Services;
namespace LYBT.WPF.Client.Modules.SystemManagement.Records.ViewModels
{
    /// <summary>
    /// 简化版新增病历对话框视图模型
    /// </summary>
    public class SimpleAddRecordDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        #region 属性

        private string _patientName = string.Empty;
        private string _patientId = string.Empty;
        private string _chiefComplaint = string.Empty;
        private string _presentIllness = string.Empty;
        private string _diagnosis = string.Empty;
        private string _treatmentPlan = string.Empty;
        private string _remark = string.Empty;
        private DateTime _recordDate = DateTime.Now;

        /// <summary>患者姓名</summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        /// <summary>患者ID或病历号</summary>
        public string PatientId
        {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
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

        /// <summary>诊断</summary>
        public string Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }

        /// <summary>治疗方案</summary>
        public string TreatmentPlan
        {
            get => _treatmentPlan;
            set => SetProperty(ref _treatmentPlan, value);
        }

        /// <summary>备注</summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>就诊日期</summary>
        public DateTime RecordDate
        {
            get => _recordDate;
            set => SetProperty(ref _recordDate, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 回调

        public Action<bool>? CloseDialogCallback { get; set; }

        #endregion

        public SimpleAddRecordDialogViewModel(ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            SaveCommand = new DelegateCommand(ExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        private void ExecuteSave()
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(PatientName))
            {
                _commonDialogService.ShowWarningAsync("请输入患者姓名", "提示").GetAwaiter().GetResult();
                return;
            }

            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                _commonDialogService.ShowWarningAsync("请输入主诉", "提示").GetAwaiter().GetResult();
                return;
            }

            if (string.IsNullOrWhiteSpace(Diagnosis))
            {
                _commonDialogService.ShowWarningAsync("请输入诊断", "提示").GetAwaiter().GetResult();
                return;
            }

            try
            {
                // 创建病历数据
                var recordData = new RecordCreateDto
                {
                    PatientId = Guid.NewGuid(), // 实际应用中应该选择真实的患者ID
                    RegistrationId = Guid.NewGuid(), // 实际应用中应该使用真实的挂号ID
                    CreatedBy = Guid.NewGuid(), // 实际应用中应该使用当前登录医生的ID
                    ChiefComplaint = ChiefComplaint,
                    PresentIllness = PresentIllness ?? string.Empty,
                    Diagnosis = Diagnosis,
                    TreatmentAdvice = TreatmentPlan ?? string.Empty,
                    RecordTime = RecordDate,
                    IsShared = false
                };

                // 显示成功消息（实际应该调用API保存）
                _commonDialogService.ShowInformationAsync($"病历信息已记录：\n\n" +
                    $"患者：{PatientName}\n" +
                    $"主诉：{ChiefComplaint}\n" +
                    $"诊断：{Diagnosis}\n" +
                    $"就诊时间：{RecordDate:yyyy-MM-dd HH:mm}\n\n" +
                    $"注意：当前为演示版本，数据未实际保存到数据库。", "保存成功").GetAwaiter().GetResult();

                CloseDialogCallback?.Invoke(true);
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存病历失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteCancel()
        {
            CloseDialogCallback?.Invoke(false);
        }
    }
}