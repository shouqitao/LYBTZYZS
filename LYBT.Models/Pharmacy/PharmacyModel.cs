using LYBT.Common.Enums;

namespace LYBT.Models.Pharmacy {

    /// <summary>
    /// 药房任务模型
    /// </summary>
    public class PharmacyModel {
        public Guid TaskId { get; set; }        // 抓药任务ID
        public Guid PrescriptionId { get; set; }    // 关联处方ID
        public Guid PatientId { get; set; }
        public List<HerbModel> Herbs { get; set; } = new(); // 药材明细
        public bool NeedDecoction { get; set; }       // 是否代煎
        public PharmacyStatus Status { get; set; }            // 状态：待抓药、抓药中、已抓药、已取药
        public DateTime CreateTime { get; set; }
        public Guid DoctorId { get; internal set; }
        public Guid Id { get; set; }
        public DateTime DispenseTime { get; set; }
        public Guid OperatorId { get; set; }
        public string? Remark { get; set; }
    }


}