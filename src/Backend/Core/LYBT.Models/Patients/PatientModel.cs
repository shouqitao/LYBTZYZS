using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Patients {

    /// <summary>
    /// 患者档案信息实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class PatientModel : BasePatientModel {

        /// <summary>
        /// 患者档案状态（后端专用，支持软删除策略）
        /// </summary>
        [Required]
        [DisplayName("患者档案状态")]
        public PatientStatus Status { get; set; } = PatientStatus.Active;

        /// <summary>
        /// 禁用原因（后端专用，软删除时记录原因）
        /// </summary>
        [StringLength(128)]
        [DisplayName("禁用原因")]
        public string? DisableReason { get; set; }

        /// <summary>
        /// 最后就诊时间（后端业务字段）
        /// </summary>
        [DisplayName("最后就诊时间")]
        public DateTime? LastVisitTime { get; set; }

        /// <summary>
        /// 就诊次数（后端统计字段）
        /// </summary>
        [DisplayName("就诊次数")]
        public int VisitCount { get; set; }

        /// <summary>
        /// 创建者ID（后端审计字段）
        /// </summary>
        [DisplayName("创建者ID")]
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// 更新者ID（后端审计字段）
        /// </summary>
        [DisplayName("更新者ID")]
        public Guid? UpdatedBy { get; set; }
    }
}