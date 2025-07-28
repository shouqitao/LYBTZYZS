using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 中医四诊信息DTO（用于创建和编辑）
    /// </summary>
    public class TCMDiagnosisDto {

        /// <summary>
        /// 诊断ID
        /// </summary>
        [DisplayName("诊断ID")]
        public Guid? Id { get; set; }

        /// <summary>
        /// 诊疗记录ID
        /// </summary>
        [Required(ErrorMessage = "诊疗记录ID不能为空")]
        [DisplayName("诊疗记录ID")]
        public Guid DiagnosisTreatmentId { get; set; }

        #region 四诊信息

        /// <summary>
        /// 望诊信息
        /// </summary>
        [DisplayName("望诊信息")]
        public TCMInspectionDto? Inspection { get; set; }

        /// <summary>
        /// 闻诊信息  
        /// </summary>
        [DisplayName("闻诊信息")]
        public TCMAuscultationDto? Auscultation { get; set; }

        /// <summary>
        /// 问诊信息
        /// </summary>
        [DisplayName("问诊信息")]
        public TCMInquiryDto? Inquiry { get; set; }

        /// <summary>
        /// 切诊信息
        /// </summary>
        [DisplayName("切诊信息")]
        public TCMPalpationDto? Palpation { get; set; }

        #endregion

        #region 中医诊断

        /// <summary>
        /// 中医证候诊断
        /// </summary>
        [StringLength(500, ErrorMessage = "中医证候诊断不能超过500个字符")]
        [DisplayName("中医证候诊断")]
        public string? TCMSyndrome { get; set; }

        /// <summary>
        /// 西医诊断（参考）
        /// </summary>
        [StringLength(500, ErrorMessage = "西医诊断不能超过500个字符")]
        [DisplayName("西医诊断")]
        public string? WesternDiagnosis { get; set; }

        /// <summary>
        /// 治法
        /// </summary>
        [StringLength(200, ErrorMessage = "治法不能超过200个字符")]
        [DisplayName("治法")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>
        /// 病机分析
        /// </summary>
        [StringLength(1000, ErrorMessage = "病机分析不能超过1000个字符")]
        [DisplayName("病机分析")]
        public string? PathogenesisAnalysis { get; set; }

        #endregion
    }

    /// <summary>
    /// 望诊信息DTO
    /// </summary>
    public class TCMInspectionDto {
        /// <summary>
        /// 面色
        /// </summary>
        [StringLength(100, ErrorMessage = "面色描述不能超过100个字符")]
        [DisplayName("面色")]
        public string? FacialColor { get; set; }

        /// <summary>
        /// 精神状态
        /// </summary>
        [StringLength(100, ErrorMessage = "精神状态描述不能超过100个字符")]
        [DisplayName("精神状态")]
        public string? MentalState { get; set; }

        /// <summary>
        /// 形体
        /// </summary>
        [StringLength(100, ErrorMessage = "形体描述不能超过100个字符")]
        [DisplayName("形体")]
        public string? BodyType { get; set; }

        /// <summary>
        /// 舌象描述
        /// </summary>
        [StringLength(200, ErrorMessage = "舌象描述不能超过200个字符")]
        [DisplayName("舌象")]
        public string? TongueCondition { get; set; }

        /// <summary>
        /// 其他望诊所见
        /// </summary>
        [StringLength(500, ErrorMessage = "其他望诊所见不能超过500个字符")]
        [DisplayName("其他望诊")]
        public string? OtherObservations { get; set; }
    }

    /// <summary>
    /// 闻诊信息DTO
    /// </summary>
    public class TCMAuscultationDto {
        /// <summary>
        /// 声音
        /// </summary>
        [StringLength(100, ErrorMessage = "声音描述不能超过100个字符")]
        [DisplayName("声音")]
        public string? Voice { get; set; }

        /// <summary>
        /// 呼吸
        /// </summary>
        [StringLength(100, ErrorMessage = "呼吸描述不能超过100个字符")]
        [DisplayName("呼吸")]
        public string? Breathing { get; set; }

        /// <summary>
        /// 气味
        /// </summary>
        [StringLength(100, ErrorMessage = "气味描述不能超过100个字符")]
        [DisplayName("气味")]
        public string? Odor { get; set; }

        /// <summary>
        /// 其他闻诊所闻
        /// </summary>
        [StringLength(300, ErrorMessage = "其他闻诊所闻不能超过300个字符")]
        [DisplayName("其他闻诊")]
        public string? OtherSounds { get; set; }
    }

    /// <summary>
    /// 问诊信息DTO
    /// </summary>
    public class TCMInquiryDto {
        /// <summary>
        /// 寒热
        /// </summary>
        [StringLength(100, ErrorMessage = "寒热描述不能超过100个字符")]
        [DisplayName("寒热")]
        public string? ColdHeat { get; set; }

        /// <summary>
        /// 汗出
        /// </summary>
        [StringLength(100, ErrorMessage = "汗出描述不能超过100个字符")]
        [DisplayName("汗出")]
        public string? Sweating { get; set; }

        /// <summary>
        /// 头身胸腹
        /// </summary>
        [StringLength(200, ErrorMessage = "头身胸腹描述不能超过200个字符")]
        [DisplayName("头身胸腹")]
        public string? HeadBodyChestAbdomen { get; set; }

        /// <summary>
        /// 二便
        /// </summary>
        [StringLength(200, ErrorMessage = "二便描述不能超过200个字符")]
        [DisplayName("二便")]
        public string? BowelBladder { get; set; }

        /// <summary>
        /// 饮食
        /// </summary>
        [StringLength(100, ErrorMessage = "饮食描述不能超过100个字符")]
        [DisplayName("饮食")]
        public string? DietAppetite { get; set; }

        /// <summary>
        /// 睡眠
        /// </summary>
        [StringLength(100, ErrorMessage = "睡眠描述不能超过100个字符")]
        [DisplayName("睡眠")]
        public string? Sleep { get; set; }

        /// <summary>
        /// 妇科
        /// </summary>
        [StringLength(200, ErrorMessage = "妇科描述不能超过200个字符")]
        [DisplayName("妇科")]
        public string? Gynecology { get; set; }

        /// <summary>
        /// 情志
        /// </summary>
        [StringLength(100, ErrorMessage = "情志描述不能超过100个字符")]
        [DisplayName("情志")]
        public string? Emotional { get; set; }

        /// <summary>
        /// 起病情况及诱因
        /// </summary>
        [StringLength(300, ErrorMessage = "起病情况描述不能超过300个字符")]
        [DisplayName("起病情况")]
        public string? OnsetCondition { get; set; }
    }

    /// <summary>
    /// 切诊信息DTO
    /// </summary>
    public class TCMPalpationDto {
        /// <summary>
        /// 脉象
        /// </summary>
        [StringLength(100, ErrorMessage = "脉象描述不能超过100个字符")]
        [DisplayName("脉象")]
        public string? PulseCondition { get; set; }

        /// <summary>
        /// 脉位
        /// </summary>
        [StringLength(50, ErrorMessage = "脉位描述不能超过50个字符")]
        [DisplayName("脉位")]
        public string? PulsePosition { get; set; }

        /// <summary>
        /// 脉率（次/分）
        /// </summary>
        [Range(30, 200, ErrorMessage = "脉率应在30-200次/分之间")]
        [DisplayName("脉率")]
        public int? PulseRate { get; set; }

        /// <summary>
        /// 脉力
        /// </summary>
        [StringLength(50, ErrorMessage = "脉力描述不能超过50个字符")]
        [DisplayName("脉力")]
        public string? PulseStrength { get; set; }

        /// <summary>
        /// 按诊
        /// </summary>
        [StringLength(300, ErrorMessage = "按诊描述不能超过300个字符")]
        [DisplayName("按诊")]
        public string? Palpation { get; set; }
    }

    /// <summary>
    /// 中医四诊信息详情DTO（用于查看）
    /// </summary>
    public class TCMDiagnosisDetailDto : TCMDiagnosisDto {
        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [DisplayName("更新时间")]
        public DateTime UpdatedTime { get; set; }
    }
}