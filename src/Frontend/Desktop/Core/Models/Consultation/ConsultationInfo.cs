using System;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Models.Consultation
{
    /// <summary>
    /// 看诊信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class ConsultationInfo : BaseConsultationModel
    {
        /// <summary>患者姓名（前端显示字段）</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名（前端显示字段）</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>血压格式化显示（收缩压/舒张压）</summary>
        public string BloodPressureText =>
            (SystolicPressure.HasValue && DiastolicPressure.HasValue)
                ? $"{SystolicPressure}/{DiastolicPressure} mmHg"
                : "未测量";

        /// <summary>体温格式化显示</summary>
        public string TemperatureText =>
            Temperature.HasValue ? $"{Temperature:F1}°C" : "未测量";

        /// <summary>心率格式化显示</summary>
        public string HeartRateText =>
            HeartRate.HasValue ? $"{HeartRate} 次/分" : "未测量";

        /// <summary>诊断类型名称（前端显示字段）</summary>
        public string? DiagnosisCatalogName { get; set; }

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>看诊时间格式化（前端显示）</summary>
        public string ConsultationTimeText => ConsultationTime.ToString("yyyy-MM-dd HH:mm");

        /// <summary>看诊时长描述</summary>
        public string DurationText =>
            Duration.HasValue ? $"{Duration} 分钟" : "未记录";

        /// <summary>中医四诊是否完整</summary>
        public bool IsTCMComplete =>
            !string.IsNullOrWhiteSpace(Inspection) &&
            !string.IsNullOrWhiteSpace(AuscultationOlfaction) &&
            !string.IsNullOrWhiteSpace(Inquiry) &&
            !string.IsNullOrWhiteSpace(Palpation);

        /// <summary>生命体征是否完整</summary>
        public bool IsVitalSignsComplete =>
            Temperature.HasValue &&
            SystolicPressure.HasValue &&
            DiastolicPressure.HasValue &&
            HeartRate.HasValue;

        /// <summary>诊断是否完整</summary>
        public bool IsDiagnosisComplete =>
            !string.IsNullOrWhiteSpace(Diagnosis) &&
            (!string.IsNullOrWhiteSpace(TCMDiagnosis) || !string.IsNullOrWhiteSpace(WesternDiagnosis));

        /// <summary>状态描述</summary>
        public string StatusText => Status == CommonStatus.Enabled ? "有效" : "无效";

        /// <summary>患者年龄（前端显示字段）</summary>
        public int? PatientAge { get; set; }

        /// <summary>患者性别（前端显示字段）</summary>
        public string? PatientGender { get; set; }

        /// <summary>患者联系电话（前端显示字段）</summary>
        public string? PatientPhone { get; set; }

        /// <summary>症状描述（前端字段）</summary>
        public string? Symptoms { get; set; }
    }

}