using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Models.MedicalCase;
using LYBT.Models.Patients;
using LYBT.Models.Users;

namespace LYBT.Models.Consultation
{
    /// <summary>
    /// 看诊实体 - 继承共享基础模型，数据库映射
    /// </summary>
    [Table("Consultations")]
    public class ConsultationModel : BaseConsultation
    {
        // 导航属性
        /// <summary>
        /// 患者信息
        /// </summary>
        public virtual PatientModel? Patient { get; set; }

        /// <summary>
        /// 医生信息
        /// </summary>
        public virtual UserModel? User { get; set; }

        /// <summary>
        /// 医疗案例
        /// </summary>
        public virtual MedicalCaseModel? MedicalCase { get; set; }
    }
}