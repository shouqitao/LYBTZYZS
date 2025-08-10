using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Patients
{

    /// <summary>
    /// 患者档案信息实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class PatientModel : BasePatient
    {

        /// <summary>
        /// 禁用原因（后端专用，软删除时记录原因）
        /// </summary>
        [StringLength(128)]
        [DisplayName("禁用原因")]
        public string? DisableReason { get; set; }

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