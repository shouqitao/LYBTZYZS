using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 看诊信息DTO - UltraThink v2.0简化版
    /// 与Consultation实体对齐，专注中医四诊
    /// </summary>
    public class ConsultationDto : StatusDto, IRemarkable
    {
        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid UserId { get; set; }

        /// <summary>医生ID（兼容性别名）</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId 
        { 
            get => UserId; 
            set => UserId = value; 
        }

        /// <summary>看诊时间</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationTime { get; set; } = DateTime.Now;

        /// <summary>医生姓名（展示用）</summary>
        [DisplayName("医生姓名")]
        public string? DoctorName { get; set; }

        /// <summary>主诉</summary>
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>望诊记录</summary>
        [StringLength(500, ErrorMessage = "望诊记录长度不能超过500个字符")]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊记录</summary>
        [StringLength(500, ErrorMessage = "闻诊记录长度不能超过500个字符")]
        [DisplayName("闻诊")]
        public string? Auscultation { get; set; }

        /// <summary>问诊记录</summary>
        [StringLength(500, ErrorMessage = "问诊记录长度不能超过500个字符")]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊记录</summary>
        [StringLength(500, ErrorMessage = "切诊记录长度不能超过500个字符")]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>初步诊断</summary>
        [StringLength(500, ErrorMessage = "初步诊断长度不能超过500个字符")]
        [DisplayName("初步诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>是否完成</summary>
        [DisplayName("是否完成")]
        public bool IsCompleted 
        { 
            get => Status == CommonStatus.Enabled;
        }
    }

    /// <summary>
    /// 看诊详情DTO - 继承审计基础DTO + 备注接口
    /// 用于看诊详情的展示和传输
    /// </summary>
    public class ConsultationDetailDto : TimestampDto, IRemarkable
    {
        /// <summary>医疗案例ID</summary>
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>用户ID（兼容旧属性）</summary>
        public Guid UserId => DoctorId;

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>看诊时间</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationTime { get; set; }

        /// <summary>主诉</summary>
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>望诊记录</summary>
        [StringLength(500, ErrorMessage = "望诊记录长度不能超过500个字符")]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊记录</summary>
        [StringLength(500, ErrorMessage = "闻诊记录长度不能超过500个字符")]
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊记录</summary>
        [StringLength(500, ErrorMessage = "问诊记录长度不能超过500个字符")]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊记录</summary>
        [StringLength(500, ErrorMessage = "切诊记录长度不能超过500个字符")]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>诊断结果</summary>
        [Required(ErrorMessage = "诊断结果不能为空")]
        [StringLength(500, ErrorMessage = "诊断结果长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>医嘱</summary>
        [StringLength(1000, ErrorMessage = "医嘱长度不能超过1000个字符")]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>看诊开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>看诊结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>看诊状态</summary>
        [DisplayName("状态")]
        public ConsultationStatus Status { get; set; } = ConsultationStatus.InProgress;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>是否完成</summary>
        [DisplayName("是否完成")]
        public bool IsCompleted { get; set; } = false;
    }

    /// <summary>
    /// 看诊创建DTO - 前后端共享API契约
    /// 用于创建新看诊记录的请求模型
    /// </summary>
    public class ConsultationCreateDto
    {
        /// <summary>医疗案例ID</summary>
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>主诉</summary>
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>看诊开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>患者姓名(展示用)</summary>
        [DisplayName("患者姓名")]
        public string? PatientName { get; set; }

        /// <summary>医生姓名(展示用)</summary>
        [DisplayName("医生姓名")]
        public string? DoctorName { get; set; }

        /// <summary>创建时间(展示用)</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 看诊更新DTO - 继承基础DTO + 备注接口
    /// 用于更新看诊记录的请求模型
    /// </summary>
    public class ConsultationUpdateDto : BaseDto, IRemarkable
    {
        /// <summary>主诉</summary>
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>望诊结果</summary>
        [StringLength(500, ErrorMessage = "望诊结果长度不能超过500个字符")]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊结果</summary>
        [StringLength(500, ErrorMessage = "闻诊结果长度不能超过500个字符")]
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊结果</summary>
        [StringLength(500, ErrorMessage = "问诊结果长度不能超过500个字符")]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊结果</summary>
        [StringLength(500, ErrorMessage = "切诊结果长度不能超过500个字符")]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>舌诊结果</summary>
        [StringLength(500, ErrorMessage = "舌诊结果长度不能超过500个字符")]
        [DisplayName("舌诊")]
        public string? TongueInspection { get; set; }

        /// <summary>脉诊结果</summary>
        [StringLength(500, ErrorMessage = "脉诊结果长度不能超过500个字符")]
        [DisplayName("脉诊")]
        public string? PulseCondition { get; set; }

        /// <summary>辨证分析</summary>
        [StringLength(800, ErrorMessage = "辨证分析长度不能超过800个字符")]
        [DisplayName("辨证分析")]
        public string? PatternDifferentiation { get; set; }

        /// <summary>中医辨证</summary>
        [StringLength(500, ErrorMessage = "中医辨证长度不能超过500个字符")]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>诊断结果</summary>
        [StringLength(500, ErrorMessage = "诊断结果长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(500, ErrorMessage = "治疗原则长度不能超过500个字符")]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(1000, ErrorMessage = "医嘱长度不能超过1000个字符")]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        /// <summary>是否完成</summary>
        [DisplayName("是否完成")]
        public bool? IsCompleted { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        #region 详细四诊属性（用于客户端四诊界面）

        /// <summary>面色</summary>
        [StringLength(200, ErrorMessage = "面色长度不能超过200个字符")]
        [DisplayName("面色")]
        public string? Complexion { get; set; }

        /// <summary>神态</summary>
        [StringLength(200, ErrorMessage = "神态长度不能超过200个字符")]
        [DisplayName("神态")]
        public string? Spirit { get; set; }

        /// <summary>体型</summary>
        [StringLength(200, ErrorMessage = "体型长度不能超过200个字符")]
        [DisplayName("体型")]
        public string? BodyShape { get; set; }

        /// <summary>舌质</summary>
        [StringLength(200, ErrorMessage = "舌质长度不能超过200个字符")]
        [DisplayName("舌质")]
        public string? TongueBody { get; set; }

        /// <summary>舌苔</summary>
        [StringLength(200, ErrorMessage = "舌苔长度不能超过200个字符")]
        [DisplayName("舌苔")]
        public string? TongueCoating { get; set; }

        /// <summary>声音</summary>
        [StringLength(200, ErrorMessage = "声音长度不能超过200个字符")]
        [DisplayName("声音")]
        public string? Voice { get; set; }

        /// <summary>呼吸</summary>
        [StringLength(200, ErrorMessage = "呼吸长度不能超过200个字符")]
        [DisplayName("呼吸")]
        public string? Breath { get; set; }

        /// <summary>咳嗽</summary>
        [StringLength(200, ErrorMessage = "咳嗽长度不能超过200个字符")]
        [DisplayName("咳嗽")]
        public string? Cough { get; set; }

        /// <summary>寒热</summary>
        [StringLength(200, ErrorMessage = "寒热长度不能超过200个字符")]
        [DisplayName("寒热")]
        public string? ColdHeat { get; set; }

        /// <summary>汗出</summary>
        [StringLength(200, ErrorMessage = "汗出长度不能超过200个字符")]
        [DisplayName("汗出")]
        public string? Sweat { get; set; }

        /// <summary>饮食</summary>
        [StringLength(200, ErrorMessage = "饮食长度不能超过200个字符")]
        [DisplayName("饮食")]
        public string? Appetite { get; set; }

        /// <summary>睡眠</summary>
        [StringLength(200, ErrorMessage = "睡眠长度不能超过200个字符")]
        [DisplayName("睡眠")]
        public string? Sleep { get; set; }

        /// <summary>二便</summary>
        [StringLength(200, ErrorMessage = "二便长度不能超过200个字符")]
        [DisplayName("二便")]
        public string? StoolUrine { get; set; }

        /// <summary>脉象</summary>
        [StringLength(200, ErrorMessage = "脉象长度不能超过200个字符")]
        [DisplayName("脉象")]
        public string? Pulse { get; set; }

        /// <summary>脉率</summary>
        [StringLength(200, ErrorMessage = "脉率长度不能超过200个字符")]
        [DisplayName("脉率")]
        public string? PulseRate { get; set; }

        /// <summary>脉力</summary>
        [StringLength(200, ErrorMessage = "脉力长度不能超过200个字符")]
        [DisplayName("脉力")]
        public string? PulseStrength { get; set; }

        /// <summary>气味</summary>
        [StringLength(200, ErrorMessage = "气味长度不能超过200个字符")]
        [DisplayName("气味")]
        public string? Odor { get; set; }

        /// <summary>头身</summary>
        [StringLength(200, ErrorMessage = "头身长度不能超过200个字符")]
        [DisplayName("头身")]
        public string? HeadBody { get; set; }

        /// <summary>胸腹</summary>
        [StringLength(200, ErrorMessage = "胸腹长度不能超过200个字符")]
        [DisplayName("胸腹")]
        public string? ChestAbdomen { get; set; }

        /// <summary>月经</summary>
        [StringLength(200, ErrorMessage = "月经长度不能超过200个字符")]
        [DisplayName("月经")]
        public string? Menstruation { get; set; }

        /// <summary>脉律</summary>
        [StringLength(200, ErrorMessage = "脉律长度不能超过200个字符")]
        [DisplayName("脉律")]
        public string? PulseRhythm { get; set; }

        /// <summary>脉形</summary>
        [StringLength(200, ErrorMessage = "脉形长度不能超过200个字符")]
        [DisplayName("脉形")]
        public string? PulseShape { get; set; }

        /// <summary>左脉</summary>
        [StringLength(200, ErrorMessage = "左脉长度不能超过200个字符")]
        [DisplayName("左脉")]
        public string? LeftPulse { get; set; }

        /// <summary>右脉</summary>
        [StringLength(200, ErrorMessage = "右脉长度不能超过200个字符")]
        [DisplayName("右脉")]
        public string? RightPulse { get; set; }

        /// <summary>中医证候</summary>
        [StringLength(500, ErrorMessage = "中医证候长度不能超过500个字符")]
        [DisplayName("证候")]
        public string? TCMSyndrome { get; set; }

        #endregion
    }

    /// <summary>
    /// 看诊验证结果DTO
    /// </summary>
    public class ConsultationValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
    }
}