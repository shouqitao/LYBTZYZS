using LYBT.Common.Enums;

namespace LYBT.Models.Pharmacy {

    /// <summary>
    /// 药房任务模型
    /// </summary>
    public class PharmacyModel {

        /// <summary>抓药任务ID</summary>
        public Guid TaskId { get; set; }

        /// <summary>关联处方ID</summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>药材明细</summary>
        public List<HerbModel> Herbs { get; set; } = new();

        /// <summary>是否代煎</summary>
        public bool NeedDecoction { get; set; }

        /// <summary>状态：待抓药、抓药中、已抓药、已取药</summary>
        public PharmacyStatus Status { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>开方医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>主键ID</summary>
        public Guid Id { get; set; }

        /// <summary>抓药时间</summary>
        public DateTime DispenseTime { get; set; }

        /// <summary>操作人ID</summary>
        public Guid OperatorId { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}