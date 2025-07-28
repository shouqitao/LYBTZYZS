using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.DiagnosisTreatment.Models {

    /// <summary>
    /// 中医四诊信息结构化模型
    /// </summary>
    public class TCMDiagnosisModel {

        /// <summary>
        /// 诊断ID（主键）
        /// </summary>
        [Key]
        [DisplayName("诊断ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 诊疗记录ID（外键）
        /// </summary>
        [Required]
        [DisplayName("诊疗记录ID")]
        public Guid DiagnosisTreatmentId { get; set; }

        /// <summary>
        /// 关联的诊疗记录
        /// </summary>
        public virtual DiagnosisTreatmentModel DiagnosisTreatment { get; set; } = null!;

        #region 四诊信息

        /// <summary>
        /// 望诊信息
        /// </summary>
        [DisplayName("望诊信息")]
        public TCMInspectionModel? Inspection { get; set; }

        /// <summary>
        /// 闻诊信息  
        /// </summary>
        [DisplayName("闻诊信息")]
        public TCMAuscultationModel? Auscultation { get; set; }

        /// <summary>
        /// 问诊信息
        /// </summary>
        [DisplayName("问诊信息")]
        public TCMInquiryModel? Inquiry { get; set; }

        /// <summary>
        /// 切诊信息
        /// </summary>
        [DisplayName("切诊信息")]
        public TCMPalpationModel? Palpation { get; set; }

        #endregion

        #region 中医诊断

        /// <summary>
        /// 中医证候诊断
        /// </summary>
        [StringLength(500)]
        [DisplayName("中医证候诊断")]
        public string? TCMSyndrome { get; set; }

        /// <summary>
        /// 西医诊断（参考）
        /// </summary>
        [StringLength(500)]
        [DisplayName("西医诊断")]
        public string? WesternDiagnosis { get; set; }

        /// <summary>
        /// 治法（如疏肝解郁、健脾益气等）
        /// </summary>
        [StringLength(200)]
        [DisplayName("治法")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>
        /// 病机分析
        /// </summary>
        [StringLength(1000)]
        [DisplayName("病机分析")]
        public string? PathogenesisAnalysis { get; set; }

        #endregion

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [DisplayName("更新时间")]
        public DateTime UpdatedTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 望诊信息模型
    /// </summary>
    public class TCMInspectionModel {
        /// <summary>
        /// 面色（如苍白、潮红、萎黄等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("面色")]
        public string? FacialColor { get; set; }

        /// <summary>
        /// 精神状态（如精神、萎靡、烦躁等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("精神状态")]
        public string? MentalState { get; set; }

        /// <summary>
        /// 形体（如肥胖、消瘦、正常等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("形体")]
        public string? BodyType { get; set; }

        /// <summary>
        /// 舌象描述（舌质、舌苔）
        /// </summary>
        [StringLength(200)]
        [DisplayName("舌象")]
        public string? TongueCondition { get; set; }

        /// <summary>
        /// 其他望诊所见
        /// </summary>
        [StringLength(500)]
        [DisplayName("其他望诊")]
        public string? OtherObservations { get; set; }
    }

    /// <summary>
    /// 闻诊信息模型
    /// </summary>
    public class TCMAuscultationModel {
        /// <summary>
        /// 声音（如语声低微、声音洪亮等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("声音")]
        public string? Voice { get; set; }

        /// <summary>
        /// 呼吸（如气喘、气短等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("呼吸")]
        public string? Breathing { get; set; }

        /// <summary>
        /// 气味（如口臭、体味等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("气味")]
        public string? Odor { get; set; }

        /// <summary>
        /// 其他闻诊所闻
        /// </summary>
        [StringLength(300)]
        [DisplayName("其他闻诊")]
        public string? OtherSounds { get; set; }
    }

    /// <summary>
    /// 问诊信息模型
    /// </summary>
    public class TCMInquiryModel {
        /// <summary>
        /// 寒热（如恶寒、发热、寒热往来等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("寒热")]
        public string? ColdHeat { get; set; }

        /// <summary>
        /// 汗出（如自汗、盗汗、无汗等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("汗出")]
        public string? Sweating { get; set; }

        /// <summary>
        /// 头身胸腹（如头痛、胸闷、腹胀等）
        /// </summary>
        [StringLength(200)]
        [DisplayName("头身胸腹")]
        public string? HeadBodyChestAbdomen { get; set; }

        /// <summary>
        /// 二便（大小便情况）
        /// </summary>
        [StringLength(200)]
        [DisplayName("二便")]
        public string? BowelBladder { get; set; }

        /// <summary>
        /// 饮食（如食欲、口渴等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("饮食")]
        public string? DietAppetite { get; set; }

        /// <summary>
        /// 睡眠（如失眠、多梦等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("睡眠")]
        public string? Sleep { get; set; }

        /// <summary>
        /// 妇科（月经、带下等，仅适用于女性）
        /// </summary>
        [StringLength(200)]
        [DisplayName("妇科")]
        public string? Gynecology { get; set; }

        /// <summary>
        /// 情志（情绪状态）
        /// </summary>
        [StringLength(100)]
        [DisplayName("情志")]
        public string? Emotional { get; set; }

        /// <summary>
        /// 起病情况及诱因
        /// </summary>
        [StringLength(300)]
        [DisplayName("起病情况")]
        public string? OnsetCondition { get; set; }
    }

    /// <summary>
    /// 切诊信息模型
    /// </summary>
    public class TCMPalpationModel {
        /// <summary>
        /// 脉象（如浮、沉、迟、数、弦、滑等）
        /// </summary>
        [StringLength(100)]
        [DisplayName("脉象")]
        public string? PulseCondition { get; set; }

        /// <summary>
        /// 脉位（浮、中、沉）
        /// </summary>
        [StringLength(50)]
        [DisplayName("脉位")]
        public string? PulsePosition { get; set; }

        /// <summary>
        /// 脉率（次/分）
        /// </summary>
        [DisplayName("脉率")]
        public int? PulseRate { get; set; }

        /// <summary>
        /// 脉力（有力、无力）
        /// </summary>
        [StringLength(50)]
        [DisplayName("脉力")]
        public string? PulseStrength { get; set; }

        /// <summary>
        /// 按诊（腹诊、背诊等触诊所得）
        /// </summary>
        [StringLength(300)]
        [DisplayName("按诊")]
        public string? Palpation { get; set; }
    }
}