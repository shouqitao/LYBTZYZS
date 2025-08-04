using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 药房任务基础模型 - 前后端共享核心字段
    /// 包含所有通用的药房任务信息字段，各层可基于此模型扩展
    /// </summary>
    public class BasePharmacyModel {

        /// <summary>药房任务唯一标识</summary>
        [DisplayName("任务ID")]
        public Guid Id { get; set; }

        /// <summary>任务编号</summary>
        [DisplayName("任务编号")]
        public Guid TaskId { get; set; }

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>是否需要代煎</summary>
        [DisplayName("是否需要代煎")]
        public bool NeedDecoction { get; set; }

        /// <summary>任务状态</summary>
        [DisplayName("任务状态")]
        public PharmacyStatus Status { get; set; }

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>操作员ID</summary>
        [DisplayName("操作员ID")]
        public Guid OperatorId { get; set; }

        /// <summary>配药时间</summary>
        [DisplayName("配药时间")]
        public DateTime? DispenseTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}