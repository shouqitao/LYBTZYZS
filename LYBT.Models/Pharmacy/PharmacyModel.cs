using LYBT.Common.Enums;

namespace LYBT.Models.Pharmacy {

    /// <summary>
    /// 药房任务模型
    /// </summary>
    public class PharmacyModel {
        public string TaskId { get; set; } = string.Empty;       // 抓药任务ID
        public string PrescriptionId { get; set; } = string.Empty;   // 关联处方ID
        public string PatientId { get; set; } = string.Empty;
        public List<HerbModel> Herbs { get; set; } = new(); // 药材明细
        public bool NeedDecoction { get; set; }       // 是否代煎
        public PharmacyStatus Status { get; set; }            // 状态：待抓药、抓药中、已抓药、已取药
        public DateTime CreateTime { get; set; }
        public string DoctorId { get; internal set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime DispenseTime { get; set; }
        public string OperatorId { get; set; } = string.Empty;
        public string? Remark { get; set; }
    }


}