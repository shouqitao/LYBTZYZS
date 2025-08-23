using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 患者分页查询DTO - 继承完整查询基类 + 编码接口
    /// 用于患者档案的分页查询和筛选
    /// </summary>
    public class PatientPagedQueryDto : ExtendedQueryDto, ICodeable
    {
        /// <summary>姓名关键词</summary>
        [DisplayName("姓名关键词")]
        public string? Name { get; set; }

        /// <summary>手机号关键词</summary>
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>证件号关键词</summary>
        [DisplayName("证件号")]
        public string? IDNumber { get; set; }

        /// <summary>性别筛选</summary>
        [DisplayName("性别")]
        public Gender? Gender { get; set; }

        /// <summary>年龄范围-最小值</summary>
        [DisplayName("最小年龄")]
        public int? MinAge { get; set; }

        /// <summary>年龄范围-最大值</summary>
        [DisplayName("最大年龄")]
        public int? MaxAge { get; set; }

        /// <summary>地址关键词</summary>
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>职业关键词</summary>
        [DisplayName("职业")]
        public string? Profession { get; set; }

        /// <summary>按拼音码搜索</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>按五笔码搜索</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }
    }

    /// <summary>
    /// 分配医生DTO - 前后端共享API契约
    /// 用于为患者分配主治医生的请求模型
    /// </summary>
    public class AssignDoctorDto
    {
        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>分配原因</summary>
        [StringLength(200, ErrorMessage = "分配原因长度不能超过200个字符")]
        [DisplayName("分配原因")]
        public string? Reason { get; set; }

        /// <summary>分配时间</summary>
        [DisplayName("分配时间")]
        public DateTime AssignTime { get; set; } = DateTime.Now;

        /// <summary>是否设为主治医生</summary>
        [DisplayName("是否主治医生")]
        public bool IsPrimary { get; set; } = true;
    }

    /// <summary>
    /// 患者就诊历史DTO
    /// </summary>
    public class PatientVisitHistoryDto
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int TotalVisits { get; set; }
        public DateTime? FirstVisitDate { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public List<VisitRecordDto> VisitRecords { get; set; } = new();

        /// <summary>
        /// 平均就诊间隔（天）
        /// </summary>
        public double AverageVisitInterval
        {
            get
            {
                if (TotalVisits <= 1 || !FirstVisitDate.HasValue || !LastVisitDate.HasValue)
                    return 0;
                return (LastVisitDate.Value - FirstVisitDate.Value).TotalDays / (TotalVisits - 1);
            }
        }
    }

    /// <summary>
    /// 就诊记录DTO
    /// </summary>
    public class VisitRecordDto
    {
        public Guid Id { get; set; }
        public DateTime VisitDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public decimal? Cost { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// 患者档案管理DTO
    /// </summary>
    public class PatientProfileManagementDto
    {
        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>档案状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>最后更新时间</summary>
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>更新者</summary>
        public string? UpdatedBy { get; set; }

        /// <summary>档案完整度评分（0-100）</summary>
        public int CompletenessScore { get; set; }

        /// <summary>缺失字段列表</summary>
        public List<string> MissingFields { get; set; } = new();

        /// <summary>风险评级</summary>
        public string? RiskLevel { get; set; }
    }

    /// <summary>
    /// 患者统计DTO
    /// </summary>
    public class PatientStatisticsDto
    {
        /// <summary>患者总数</summary>
        public int TotalPatients { get; set; }

        /// <summary>活跃患者数（近3个月有就诊记录）</summary>
        public int ActivePatients { get; set; }

        /// <summary>非活跃患者数</summary>
        public int InactivePatients { get; set; }

        /// <summary>新增患者数（当月）</summary>
        public int NewPatients { get; set; }

        /// <summary>今日新增患者数</summary>
        public int TodayNewPatients { get; set; }

        /// <summary>当月新增患者数</summary>
        public int MonthNewPatients => NewPatients;

        /// <summary>男性患者数</summary>
        public int MalePatients { get; set; }

        /// <summary>男性患者数（兼容别名）</summary>
        public int MaleCount => MalePatients;

        /// <summary>女性患者数</summary>
        public int FemalePatients { get; set; }

        /// <summary>女性患者数（兼容别名）</summary>
        public int FemaleCount => FemalePatients;

        /// <summary>平均年龄</summary>
        public decimal AverageAge { get; set; }

        /// <summary>总就诊次数</summary>
        public int TotalVisits { get; set; }

        /// <summary>平均就诊次数</summary>
        public decimal AverageVisits { get; set; }

        /// <summary>有过敏史的患者数</summary>
        public int PatientsWithAllergy { get; set; }

        /// <summary>流失患者数（超6个月未就诊）</summary>
        public int LostPatients { get; set; }

        /// <summary>年龄分布统计</summary>
        public Dictionary<string, int> AgeGroupDistribution { get; set; } = new();

        /// <summary>地区分布统计</summary>
        public Dictionary<string, int> RegionDistribution { get; set; } = new();

        /// <summary>常见疾病统计</summary>
        public Dictionary<string, int> CommonDiseases { get; set; } = new();

        /// <summary>统计时间</summary>
        public DateTime StatisticsTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 患者年龄分布DTO
    /// </summary>
    public class PatientAgeDistributionDto
    {
        public string AgeGroup { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 患者就诊趋势DTO
    /// </summary>
    public class PatientVisitTrendDto
    {
        public DateTime Date { get; set; }
        public int TotalVisits { get; set; }
        public int NewPatients { get; set; }
        public int ReturnPatients { get; set; }
        public decimal AverageCost { get; set; }
    }

    /// <summary>
    /// 患者搜索结果DTO
    /// </summary>
    public class PatientSearchResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? IDNumber { get; set; }
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public int VisitCount { get; set; }

        /// <summary>匹配度得分（用于搜索排序）</summary>
        public double MatchScore { get; set; }

        /// <summary>匹配的字段</summary>
        public List<string> MatchedFields { get; set; } = new();
    }
}