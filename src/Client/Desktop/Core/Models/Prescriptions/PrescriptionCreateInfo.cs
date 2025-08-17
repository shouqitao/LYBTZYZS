using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方创建信息模型
    /// UltraThink四层架构：Info层，用于UI创建操作的数据收集和验证
    /// </summary>
    public class PrescriptionCreateInfo : BindableBase
    {
        #region 基础信息
        
        private Guid _patientId;
        [Required(ErrorMessage = "患者不能为空")]
        public Guid PatientId
        {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
        }
        
        private string _patientName = string.Empty;
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value ?? string.Empty);
        }
        
        private Guid _doctorId;
        [Required(ErrorMessage = "医生不能为空")]
        public Guid DoctorId
        {
            get => _doctorId;
            set => SetProperty(ref _doctorId, value);
        }
        
        private string _doctorName = string.Empty;
        [Required(ErrorMessage = "医生姓名不能为空")]
        [StringLength(50, ErrorMessage = "医生姓名长度不能超过50个字符")]
        public string DoctorName
        {
            get => _doctorName;
            set => SetProperty(ref _doctorName, value ?? string.Empty);
        }
        
        private Guid? _medicalCaseId;
        public Guid? MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }
        
        #endregion
        
        #region 处方信息
        
        private string? _diagnosis;
        [StringLength(200, ErrorMessage = "诊断结果长度不能超过200个字符")]
        public string? Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }
        
        private string? _chiefComplaint;
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        public string? ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }
        
        private string? _symptoms;
        [StringLength(1000, ErrorMessage = "症状描述长度不能超过1000个字符")]
        public string? Symptoms
        {
            get => _symptoms;
            set => SetProperty(ref _symptoms, value);
        }
        
        private int _dosageCount = 7;
        [Range(1, 30, ErrorMessage = "服药剂数必须在1-30之间")]
        public int DosageCount
        {
            get => _dosageCount;
            set => SetProperty(ref _dosageCount, value);
        }
        
        #endregion
        
        #region 用药指导
        
        private string? _usage;
        [StringLength(500, ErrorMessage = "用法用量长度不能超过500个字符")]
        public string? Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }
        
        private string? _advice;
        [StringLength(1000, ErrorMessage = "医嘱长度不能超过1000个字符")]
        public string? Advice
        {
            get => _advice;
            set => SetProperty(ref _advice, value);
        }
        
        private string? _dosageForm;
        [StringLength(50, ErrorMessage = "剂型长度不能超过50个字符")]
        public string? DosageForm
        {
            get => _dosageForm;
            set => SetProperty(ref _dosageForm, value);
        }
        
        private string? _preparation;
        [StringLength(200, ErrorMessage = "制备方法长度不能超过200个字符")]
        public string? Preparation
        {
            get => _preparation;
            set => SetProperty(ref _preparation, value);
        }
        
        #endregion
        
        #region 处方项目
        
        private List<PrescriptionItemCreateInfo> _items = new();
        public List<PrescriptionItemCreateInfo> Items
        {
            get => _items;
            set => SetProperty(ref _items, value ?? new List<PrescriptionItemCreateInfo>());
        }
        
        #endregion
        
        #region 状态信息
        
        private PrescriptionStatus _status = PrescriptionStatus.Draft;
        public PrescriptionStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        
        private bool _isUrgent;
        public bool IsUrgent
        {
            get => _isUrgent;
            set => SetProperty(ref _isUrgent, value);
        }
        
        #endregion
        
        #region 备注信息
        
        private string? _remark;
        [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }
        
        private string? _internalNotes;
        [StringLength(500, ErrorMessage = "内部备注长度不能超过500个字符")]
        public string? InternalNotes
        {
            get => _internalNotes;
            set => SetProperty(ref _internalNotes, value);
        }
        
        #endregion
        
        #region UI辅助属性
        
        private bool _isSubmitting;
        public bool IsSubmitting
        {
            get => _isSubmitting;
            set => SetProperty(ref _isSubmitting, value);
        }
        
        private bool _hasValidationErrors;
        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }
        
        /// <summary>状态显示文本</summary>
        public string StatusText => GetStatusText();
        
        /// <summary>紧急程度显示文本</summary>
        public string UrgencyText => IsUrgent ? "紧急" : "普通";
        
        /// <summary>剂数显示文本</summary>
        public string DosageText => $"{DosageCount}剂";
        
        /// <summary>药材数量</summary>
        public int HerbCount => Items?.Count ?? 0;
        
        /// <summary>总金额</summary>
        public decimal TotalAmount => Items?.Sum(x => x.Subtotal) ?? 0;
        
        /// <summary>是否可以提交</summary>
        public bool CanSubmit => !IsSubmitting && PatientId != Guid.Empty && DoctorId != Guid.Empty && Items.Any();
        
        /// <summary>是否有药材</summary>
        public bool HasHerbs => Items.Any();
        
        #endregion
        
        #region 构造函数
        
        public PrescriptionCreateInfo()
        {
            // 默认值设置
            DosageCount = 7;
            Status = PrescriptionStatus.Draft;
            DosageForm = "汤剂";
            Usage = "水煎服，日一剂，分早晚温服";
            
            // 属性变更监听
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(PatientId) || e.PropertyName == nameof(DoctorId) || 
                    e.PropertyName == nameof(IsSubmitting) || e.PropertyName == nameof(Items))
                {
                    RaisePropertyChanged(nameof(CanSubmit));
                }
                if (e.PropertyName == nameof(Status))
                {
                    RaisePropertyChanged(nameof(StatusText));
                }
                if (e.PropertyName == nameof(IsUrgent))
                {
                    RaisePropertyChanged(nameof(UrgencyText));
                }
                if (e.PropertyName == nameof(DosageCount))
                {
                    RaisePropertyChanged(nameof(DosageText));
                }
                if (e.PropertyName == nameof(Items))
                {
                    RaisePropertyChanged(nameof(HerbCount));
                    RaisePropertyChanged(nameof(TotalAmount));
                    RaisePropertyChanged(nameof(HasHerbs));
                }
            };
        }
        
        #endregion
        
        #region 业务方法
        
        /// <summary>
        /// 重置表单
        /// </summary>
        public void Reset()
        {
            PatientId = Guid.Empty;
            PatientName = string.Empty;
            DoctorId = Guid.Empty;
            DoctorName = string.Empty;
            MedicalCaseId = null;
            Diagnosis = null;
            ChiefComplaint = null;
            Symptoms = null;
            DosageCount = 7;
            Usage = "水煎服，日一剂，分早晚温服";
            Advice = null;
            DosageForm = "汤剂";
            Preparation = null;
            Items.Clear();
            Status = PrescriptionStatus.Draft;
            IsUrgent = false;
            Remark = null;
            InternalNotes = null;
            IsSubmitting = false;
            HasValidationErrors = false;
        }
        
        /// <summary>
        /// 设置患者信息
        /// </summary>
        public void SetPatientInfo(Guid patientId, string patientName)
        {
            PatientId = patientId;
            PatientName = patientName;
        }
        
        /// <summary>
        /// 设置医生信息
        /// </summary>
        public void SetDoctorInfo(Guid doctorId, string doctorName)
        {
            DoctorId = doctorId;
            DoctorName = doctorName;
        }
        
        /// <summary>
        /// 添加药材
        /// </summary>
        public void AddHerb(PrescriptionItemCreateInfo herbItem)
        {
            if (herbItem != null)
            {
                Items.Add(herbItem);
                RaisePropertyChanged(nameof(Items));
            }
        }
        
        /// <summary>
        /// 移除药材
        /// </summary>
        public void RemoveHerb(PrescriptionItemCreateInfo herbItem)
        {
            if (herbItem != null && Items.Contains(herbItem))
            {
                Items.Remove(herbItem);
                RaisePropertyChanged(nameof(Items));
            }
        }
        
        /// <summary>
        /// 清空药材
        /// </summary>
        public void ClearHerbs()
        {
            Items.Clear();
            RaisePropertyChanged(nameof(Items));
        }
        
        /// <summary>
        /// 从验方模板加载
        /// </summary>
        public void LoadFromTemplate(FormulaTemplateInfo template)
        {
            if (template == null) return;
            
            ClearHerbs();
            foreach (var herb in template.Herbs)
            {
                var item = new PrescriptionItemCreateInfo
                {
                    HerbName = herb.HerbName,
                    Quantity = herb.Quantity,
                    Unit = herb.Unit,
                    Usage = herb.Usage
                };
                Items.Add(item);
            }
            RaisePropertyChanged(nameof(Items));
        }
        
        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (PatientId == Guid.Empty)
                return (false, "请选择患者");
                
            if (string.IsNullOrWhiteSpace(PatientName))
                return (false, "患者姓名不能为空");
                
            if (DoctorId == Guid.Empty)
                return (false, "请选择医生");
                
            if (string.IsNullOrWhiteSpace(DoctorName))
                return (false, "医生姓名不能为空");
                
            if (!Items.Any())
                return (false, "处方必须包含至少一味药材");
                
            if (DosageCount <= 0)
                return (false, "服药剂数必须大于0");
                
            // 验证药材项目
            foreach (var item in Items)
            {
                var itemValidation = item.Validate();
                if (!itemValidation.IsValid)
                {
                    return (false, $"药材 '{item.HerbName}': {itemValidation.ErrorMessage}");
                }
            }
                
            return (true, null);
        }
        
        /// <summary>
        /// 计算总金额
        /// </summary>
        public void CalculateTotalAmount()
        {
            foreach (var item in Items)
            {
                item.CalculateSubtotal();
            }
            RaisePropertyChanged(nameof(TotalAmount));
        }
        
        /// <summary>
        /// 复制现有处方信息
        /// </summary>
        public void CopyFrom(PrescriptionInfo existingPrescription)
        {
            PatientId = existingPrescription.PatientId;
            PatientName = existingPrescription.PatientName;
            DoctorId = existingPrescription.UserId; // UserId对应DoctorId
            DoctorName = existingPrescription.DoctorName;
            MedicalCaseId = existingPrescription.MedicalCaseId;
            Diagnosis = existingPrescription.Diagnosis;
            DosageCount = existingPrescription.DosageCount;
            Usage = existingPrescription.Usage;
            Advice = existingPrescription.Advice;
            DosageForm = existingPrescription.DosageForm;
            
            // 复制药材项目
            Items.Clear();
            foreach (var item in existingPrescription.Items)
            {
                var createItem = new PrescriptionItemCreateInfo
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Usage = item.Usage,
                    Remark = item.Remark
                };
                createItem.CalculateSubtotal();
                Items.Add(createItem);
            }
            RaisePropertyChanged(nameof(Items));
        }
        
        #endregion
        
        #region 私有方法
        
        private string GetStatusText()
        {
            return Status switch
            {
                PrescriptionStatus.Draft => "草稿",
                PrescriptionStatus.Completed => "已完成",
                _ => "未知状态"
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// 处方项目创建信息（扩展版）
    /// </summary>
    public class PrescriptionItemCreateInfo : BindableBase
    {
        private Guid _herbId;
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }
        
        private string _herbName = string.Empty;
        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value ?? string.Empty);
        }
        
        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set 
            { 
                if (SetProperty(ref _quantity, value))
                {
                    CalculateSubtotal();
                }
            }
        }
        
        private string _unit = string.Empty;
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value ?? string.Empty);
        }
        
        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set 
            { 
                if (SetProperty(ref _unitPrice, value))
                {
                    CalculateSubtotal();
                }
            }
        }
        
        private decimal _subtotal;
        public decimal Subtotal
        {
            get => _subtotal;
            set => SetProperty(ref _subtotal, value);
        }
        
        private string? _usage;
        public string? Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }
        
        private string? _remark;
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }
        
        public void CalculateSubtotal()
        {
            Subtotal = Quantity * UnitPrice;
        }
        
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (HerbId == Guid.Empty)
                return (false, "请选择中药材");
                
            if (string.IsNullOrWhiteSpace(HerbName))
                return (false, "中药材名称不能为空");
                
            if (Quantity <= 0)
                return (false, "用量必须大于0");
                
            if (UnitPrice < 0)
                return (false, "单价不能为负数");
                
            if (string.IsNullOrWhiteSpace(Unit))
                return (false, "单位不能为空");
                
            return (true, null);
        }
    }
}