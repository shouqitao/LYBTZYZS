using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;
using LYBT.WPF.Client.Core.Models.TreatmentRoom;

namespace LYBT.WPF.Client.Core.Models.Records
{
    /// <summary>
    /// 病历信息
    /// </summary>
    public class RecordInfo
    {
        /// <summary>病历ID</summary>
        public Guid Id { get; set; }

        /// <summary>记录ID</summary>
        public Guid RecordId { get; set; }

        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>患者信息</summary>
        public PatientInfo Patient { get; set; } = new PatientInfo();

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>诊断</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>诊断文本</summary>
        public string DiagnosisText { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        public string? PresentIllness { get; set; }

        /// <summary>治疗建议</summary>
        public string? TreatmentAdvice { get; set; }

        /// <summary>处方摘要</summary>
        public string PrescriptionSummary { get; set; } = string.Empty;

        /// <summary>治疗摘要</summary>
        public string TreatmentSummary { get; set; } = string.Empty;

        /// <summary>处方ID</summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>关联的处方模板ID</summary>
        public Guid? FormulaTemplateId { get; set; }

        /// <summary>关联的理疗项目ID列表</summary>
        public List<Guid>? TreatmentRoomIds { get; set; }

        /// <summary>是否共享</summary>
        public bool IsShared { get; set; }

        /// <summary>就诊时间</summary>
        public DateTime VisitTime { get; set; }

        /// <summary>病历记录时间</summary>
        public DateTime RecordTime { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>状态</summary>
        public string Status { get; set; } = "InProgress"; // InProgress, Completed

        /// <summary>处方项目列表</summary>
        public ObservableCollection<PrescriptionItem> Prescription { get; set; } = new ObservableCollection<PrescriptionItem>();

        /// <summary>总金额</summary>
        public decimal TotalAmount
        {
            get
            {
                decimal total = 0;
                foreach (var item in Prescription)
                {
                    total += item.SubTotal;
                }
                return total;
            }
        }
    }

    /// <summary>
    /// 处方项目
    /// </summary>
    public class PrescriptionItem
    {
        /// <summary>药材信息</summary>
        public HerbInfo Herb { get; set; } = new HerbInfo();

        /// <summary>剂量</summary>
        public decimal Dosage { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = "g";

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>用法</summary>
        public string Usage { get; set; } = string.Empty;

        /// <summary>小计</summary>
        public decimal SubTotal => Dosage * UnitPrice;

        /// <summary>显示文本</summary>
        public string DisplayText => $"{Herb.Name} {Dosage}{Unit}";
    }
}