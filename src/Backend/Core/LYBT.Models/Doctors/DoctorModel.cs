using LYBT.Models.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Doctors {

    /// <summary>
    /// 医生信息实体 - 医生基础信息管理，关联用户系统，支持软删除策略
    /// </summary>
    public class DoctorModel : BaseDoctorModel {

        /// <summary>
        /// 身份证号码（后端特有，敏感信息）
        /// </summary>
        [StringLength(18)]
        [DisplayName("身份证号码")]
        public string? IdNumber { get; set; }

        /// <summary>
        /// 职称（主任医师、副主任医师、主治医师、医师等）
        /// </summary>
        [StringLength(50)]
        [DisplayName("职称")]
        public string? Title { get; set; }

        /// <summary>
        /// 科室
        /// </summary>
        [StringLength(50)]
        [DisplayName("科室")]
        public string? Department { get; set; }

        /// <summary>
        /// 工作年限
        /// </summary>
        [DisplayName("工作年限")]
        public int? YearsOfExperience { get; set; }

        /// <summary>
        /// 毕业院校
        /// </summary>
        [StringLength(100)]
        [DisplayName("毕业院校")]
        public string? GraduateSchool { get; set; }

        /// <summary>
        /// 学历（博士、硕士、本科等）
        /// </summary>
        [StringLength(20)]
        [DisplayName("学历")]
        public string? Education { get; set; }

        /// <summary>
        /// 擅长疾病（JSON数组格式存储）
        /// </summary>
        [DisplayName("擅长疾病")]
        public string? SpecializedDiseases { get; set; }

        /// <summary>
        /// 出诊时间（简单文本描述，如：周一至周五上午）
        /// </summary>
        [StringLength(200)]
        [DisplayName("出诊时间")]
        public string? ScheduleDescription { get; set; }

        /// <summary>
        /// 每日最大接诊量
        /// </summary>
        [DisplayName("每日最大接诊量")]
        public int MaxPatientsPerDay { get; set; } = 30;

        /// <summary>
        /// 平均就诊时长（分钟）
        /// </summary>
        [DisplayName("平均就诊时长")]
        public int AverageConsultationMinutes { get; set; } = 15;



        /// <summary>
        /// 累计接诊人次
        /// </summary>
        [DisplayName("累计接诊人次")]
        public int TotalPatientCount { get; set; } = 0;

        /// <summary>
        /// 照片URL
        /// </summary>
        [StringLength(500)]
        [DisplayName("照片")]
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// 排序号（用于显示顺序）
        /// </summary>
        [DisplayName("排序号")]
        public int SortOrder { get; set; } = 100;

        /// <summary>
        /// 最后操作者ID
        /// </summary>
        [DisplayName("最后操作者ID")]
        public Guid? LastOperatorId { get; set; }

        /// <summary>
        /// 最后操作者姓名
        /// </summary>
        [StringLength(50)]
        [DisplayName("最后操作者姓名")]
        public string? LastOperatorName { get; set; }

        /// <summary>
        /// 关联的用户实体（导航属性）
        /// </summary>
        [Required]
        [DisplayName("关联用户")]
        public virtual UserModel User { get; set; } = null!;
    }
}