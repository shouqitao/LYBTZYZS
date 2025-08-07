using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Models.Consultation
{
    /// <summary>
    /// 看诊实体 - 继承共享基础模型，数据库映射
    /// </summary>
    [Table("Consultations")]
    public class ConsultationModel : BaseConsultationModel
    {
        // 所有字段已在BaseConsultationModel中定义
        // 后端特有字段可在此添加（如导航属性）
    }
}