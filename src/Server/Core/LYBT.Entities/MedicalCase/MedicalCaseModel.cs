using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Consultation;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.MedicalCase
{
    /// <summary>
    /// 医疗案例实体 - 继承共享基础模型，数据库映射
    /// </summary>
    [Table("MedicalCases")]
    public class MedicalCaseModel : BaseMedicalCase
    {
        /// <summary>看诊信息（后端导航属性）</summary>
        [DisplayName("看诊信息")]
        public virtual ConsultationModel? Consultation { get; set; }
    }
}