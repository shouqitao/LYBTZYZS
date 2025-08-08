using Prism.Mvvm;
using System;

namespace LYBT.WPF.Client.Registration.Models
{
    /// <summary>
    /// 挂号信息模型
    /// </summary>
    public class RegistrationInfo : BindableBase
    {
        private Guid _id;
        /// <summary>
        /// 挂号ID（即医疗案例ID）
        /// </summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _registrationNumber = string.Empty;
        /// <summary>
        /// 挂号单号
        /// </summary>
        public string RegistrationNumber
        {
            get => _registrationNumber;
            set => SetProperty(ref _registrationNumber, value);
        }

        private Guid _patientId;
        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId
        {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
        }

        private string _patientName = string.Empty;
        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private string? _patientPhone;
        /// <summary>
        /// 患者电话
        /// </summary>
        public string? PatientPhone
        {
            get => _patientPhone;
            set => SetProperty(ref _patientPhone, value);
        }

        private string? _patientGender;
        /// <summary>
        /// 患者性别
        /// </summary>
        public string? PatientGender
        {
            get => _patientGender;
            set => SetProperty(ref _patientGender, value);
        }

        private int? _patientAge;
        /// <summary>
        /// 患者年龄
        /// </summary>
        public int? PatientAge
        {
            get => _patientAge;
            set => SetProperty(ref _patientAge, value);
        }

        private Guid _doctorId;
        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId
        {
            get => _doctorId;
            set => SetProperty(ref _doctorId, value);
        }

        private string _doctorName = string.Empty;
        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName
        {
            get => _doctorName;
            set => SetProperty(ref _doctorName, value);
        }

        private string _department = string.Empty;
        /// <summary>
        /// 科室
        /// </summary>
        public string Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        private DateTime _registrationTime = DateTime.Now;
        /// <summary>
        /// 挂号时间
        /// </summary>
        public DateTime RegistrationTime
        {
            get => _registrationTime;
            set => SetProperty(ref _registrationTime, value);
        }

        private DateTime? _appointmentTime;
        /// <summary>
        /// 预约时间（可选）
        /// </summary>
        public DateTime? AppointmentTime
        {
            get => _appointmentTime;
            set => SetProperty(ref _appointmentTime, value);
        }

        private string _status = "Registered";
        /// <summary>
        /// 挂号状态：Registered-已挂号，InConsultation-看诊中，Completed-已完成，Cancelled-已取消
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string? _statusText;
        /// <summary>
        /// 状态文本
        /// </summary>
        public string? StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private int _queueNumber;
        /// <summary>
        /// 排队号
        /// </summary>
        public int QueueNumber
        {
            get => _queueNumber;
            set => SetProperty(ref _queueNumber, value);
        }

        private int _waitingCount;
        /// <summary>
        /// 前面等待人数
        /// </summary>
        public int WaitingCount
        {
            get => _waitingCount;
            set => SetProperty(ref _waitingCount, value);
        }

        private decimal _registrationFee = 20m;
        /// <summary>
        /// 挂号费
        /// </summary>
        public decimal RegistrationFee
        {
            get => _registrationFee;
            set => SetProperty(ref _registrationFee, value);
        }

        private bool _isPaid;
        /// <summary>
        /// 是否已支付
        /// </summary>
        public bool IsPaid
        {
            get => _isPaid;
            set => SetProperty(ref _isPaid, value);
        }

        private string? _paymentMethod;
        /// <summary>
        /// 支付方式
        /// </summary>
        public string? PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        private string? _remark;
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private bool _isSelected;
        /// <summary>
        /// 是否选中（用于列表）
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 根据状态获取状态文本
        /// </summary>
        public void UpdateStatusText()
        {
            StatusText = Status switch
            {
                "Registered" => "已挂号",
                "InConsultation" => "看诊中",
                "Completed" => "已完成",
                "Cancelled" => "已取消",
                _ => "未知"
            };
        }

        /// <summary>
        /// 是否可以取消
        /// </summary>
        public bool CanCancel => Status == "Registered";

        /// <summary>
        /// 是否可以开始看诊
        /// </summary>
        public bool CanStartConsultation => Status == "Registered" && IsPaid;
    }
}