using LYBT.Shared.Models.Core;

namespace LYBT.WPF.Client.Core.Models.DiagnosisTreatment {
    /// <summary>
    /// 诊疗信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class DiagnosisTreatmentInfo : BaseDiagnosisTreatmentModel {
        /// <summary>患者姓名（前端显示字段）</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名（前端显示字段）</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>诊断类型名称（前端显示字段）</summary>
        public string DiagnosisCatalogName { get; set; } = string.Empty;

        /// <summary>治疗项目列表（前端展示）</summary>
        public List<TreatmentItemInfo> Treatments { get; set; } = new();

        /// <summary>药方信息（前端展示）</summary>
        public FormulaInfo? Formula { get; set; }

        /// <summary>总费用（前端计算字段）</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>是否已完成（前端状态字段）</summary>
        public bool IsCompleted { get; set; }

        /// <summary>是否已支付（前端业务字段）</summary>
        public bool IsPaid { get; set; }
    }
}