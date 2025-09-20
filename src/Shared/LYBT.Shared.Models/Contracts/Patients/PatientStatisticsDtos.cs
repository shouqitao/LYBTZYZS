using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Patients
{

    #region 统计分析DTO

    /// <summary>
    /// 年龄分布统计DTO
    /// </summary>
    public class AgeDistributionDto : StatisticsDto
    {

        /// <summary>年龄段</summary>
        [DisplayName("年龄段")]
        public string AgeRange { get; set; } = string.Empty;

        /// <summary>最小年龄</summary>
        [DisplayName("最小年龄")]
        public int MinAge { get; set; }

        /// <summary>最大年龄</summary>
        [DisplayName("最大年龄")]
        public int MaxAge { get; set; }

        /// <summary>患者数量</summary>
        [DisplayName("患者数量")]
        public int PatientCount { get; set; }

        /// <summary>数量（兼容别名）</summary>
        public int Count => PatientCount;

        /// <summary>男性患者数</summary>
        [DisplayName("男性患者")]
        public int MaleCount { get; set; }

        /// <summary>女性患者数</summary>
        [DisplayName("女性患者")]
        public int FemaleCount { get; set; }

        /// <summary>占比百分比</summary>
        [DisplayName("占比")]
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// 性别分布统计DTO
    /// </summary>
    public class GenderDistributionDto : StatisticsDto
    {

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; }

        /// <summary>性别名称</summary>
        [DisplayName("性别名称")]
        public string GenderName { get; set; } = string.Empty;

        /// <summary>患者数量</summary>
        [DisplayName("患者数量")]
        public int PatientCount { get; set; }

        /// <summary>占比百分比</summary>
        [DisplayName("占比")]
        public decimal Percentage { get; set; }

        /// <summary>男性患者数</summary>
        [DisplayName("男性患者")]
        public int MaleCount { get; set; }

        /// <summary>男性百分比</summary>
        [DisplayName("男性百分比")]
        public decimal MalePercentage { get; set; }

        /// <summary>女性患者数</summary>
        [DisplayName("女性患者")]
        public int FemaleCount { get; set; }

        /// <summary>女性百分比</summary>
        [DisplayName("女性百分比")]
        public decimal FemalePercentage { get; set; }

        /// <summary>未知性别数</summary>
        [DisplayName("未知性别")]
        public int UnknownCount { get; set; }

        /// <summary>未知百分比</summary>
        [DisplayName("未知百分比")]
        public decimal UnknownPercentage { get; set; }

        /// <summary>总数</summary>
        [DisplayName("总数")]
        public new int TotalCount { get; set; }
    }

    /// <summary>
    /// 患者趋势统计DTO
    /// </summary>
    public class PatientTrendDto : StatisticsDto
    {

        /// <summary>日期</summary>
        [DisplayName("日期")]
        public DateTime Date { get; set; }

        /// <summary>年份</summary>
        [DisplayName("年份")]
        public int Year => Date.Year;

        /// <summary>月份</summary>
        [DisplayName("月份")]
        public int Month => Date.Month;

        /// <summary>月份名称</summary>
        [DisplayName("月份")]
        public string? MonthName { get; set; }

        /// <summary>新增患者数</summary>
        [DisplayName("新增患者数")]
        public int NewPatients { get; set; }

        /// <summary>复诊患者数</summary>
        [DisplayName("复诊患者数")]
        public int ReturnPatients { get; set; }

        /// <summary>总患者数</summary>
        [DisplayName("总患者数")]
        public int TotalPatients { get; set; }

        /// <summary>就诊次数</summary>
        [DisplayName("就诊次数")]
        public int VisitCount { get; set; }

        /// <summary>男性患者数</summary>
        [DisplayName("男性患者")]
        public int MaleCount { get; set; }

        /// <summary>女性患者数</summary>
        [DisplayName("女性患者")]
        public int FemaleCount { get; set; }

        /// <summary>增长率</summary>
        [DisplayName("增长率")]
        public decimal GrowthRate { get; set; }
    }

    #endregion 统计分析DTO

    #region 导入导出DTO

    /// <summary>
    /// 患者导入DTO
    /// </summary>
    public class PatientImportDto
    {

        /// <summary>姓名</summary>
        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
        [DisplayName("姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public string GenderText { get; set; } = string.Empty;

        /// <summary>年龄</summary>
        [DisplayName("年龄")]
        public int? Age { get; set; }

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public string? BirthDateText { get; set; }

        /// <summary>身份证号</summary>
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        [DisplayName("身份证号")]
        public string? IdCardNumber { get; set; }

        /// <summary>身份证号（兼容别名）</summary>
        public string? IdNumber => IdCardNumber;

        /// <summary>手机号码</summary>
        [StringLength(11, ErrorMessage = "手机号码长度不能超过11个字符")]
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>紧急联系人姓名</summary>
        [StringLength(50, ErrorMessage = "紧急联系人姓名长度不能超过50个字符")]
        [DisplayName("紧急联系人姓名")]
        public string? EmergencyContactName { get; set; }

        /// <summary>紧急联系人电话</summary>
        [StringLength(20, ErrorMessage = "紧急联系人电话长度不能超过20个字符")]
        [DisplayName("紧急联系人电话")]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 患者导入结果DTO - 继承自通用导入结果基类
    /// </summary>
    public class PatientImportResultDto : ImportResultDto
    {
        /// <summary>患者特定错误详情</summary>
        public new List<PatientImportErrorDetail> Errors { get; set; } = new();

        /// <summary>
        /// 患者导入错误详情
        /// </summary>
        public class PatientImportErrorDetail : ErrorDetail
        {
            /// <summary>行号</summary>
            public int RowNumber { get; set; }

            /// <summary>患者姓名</summary>
            public string PatientName { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// 患者导出DTO
    /// </summary>
    public class PatientExportDto
    {

        /// <summary>患者编号</summary>
        [DisplayName("患者编号")]
        public string PatientCode { get; set; } = string.Empty;

        /// <summary>姓名</summary>
        [DisplayName("姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public string Gender { get; set; } = string.Empty;

        /// <summary>年龄</summary>
        [DisplayName("年龄")]
        public int Age { get; set; }

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public string BirthDate { get; set; } = string.Empty;

        /// <summary>身份证号</summary>
        [DisplayName("身份证号")]
        public string? IdCardNumber { get; set; }

        /// <summary>手机号码</summary>
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>紧急联系人姓名</summary>
        [DisplayName("紧急联系人姓名")]
        public string? EmergencyContactName { get; set; }

        /// <summary>紧急联系人电话</summary>
        [DisplayName("紧急联系人电话")]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>首次就诊日期</summary>
        [DisplayName("首次就诊日期")]
        public string FirstVisitDate { get; set; } = string.Empty;

        /// <summary>最后就诊日期</summary>
        [DisplayName("最后就诊日期")]
        public string LastVisitDate { get; set; } = string.Empty;

        /// <summary>就诊次数</summary>
        [DisplayName("就诊次数")]
        public int VisitCount { get; set; }

        /// <summary>过敏史</summary>
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>慢性病史</summary>
        [DisplayName("慢性病史")]
        public string? ChronicDiseases { get; set; }

        /// <summary>最后就诊时间</summary>
        [DisplayName("最后就诊时间")]
        public string? LastVisitTime => LastVisitDate;
    }

    /// <summary>
    /// 患者导出查询DTO
    /// </summary>
    public class PatientExportQueryDto : ExtendedQueryDto
    {

        /// <summary>导出字段列表</summary>
        [DisplayName("导出字段")]
        public List<string> ExportFields { get; set; } = new();

        /// <summary>患者ID列表</summary>
        [DisplayName("患者ID列表")]
        public List<Guid>? PatientIds { get; set; }

        /// <summary>性别筛选</summary>
        [DisplayName("性别")]
        public Gender? Gender { get; set; }

        /// <summary>年龄范围-最小</summary>
        [DisplayName("最小年龄")]
        public int? MinAge { get; set; }

        /// <summary>年龄范围-最大</summary>
        [DisplayName("最大年龄")]
        public int? MaxAge { get; set; }

        /// <summary>是否包含已禁用</summary>
        [DisplayName("包含已禁用")]
        public new bool IncludeInactive { get; set; } = false;

        /// <summary>导出格式</summary>
        [DisplayName("导出格式")]
        public string ExportFormat { get; set; } = "xlsx";
    }

    #endregion 导入导出DTO

    #region 高级搜索DTO

    /// <summary>
    /// 患者高级搜索DTO
    /// </summary>
    public class PatientAdvancedSearchDto : ExtendedQueryDto
    {

        /// <summary>患者姓名</summary>
        [StringLength(50)]
        [DisplayName("患者姓名")]
        public string? Name { get; set; }

        /// <summary>患者编号</summary>
        [StringLength(50)]
        [DisplayName("患者编号")]
        public string? PatientCode { get; set; }

        /// <summary>身份证号</summary>
        [StringLength(18)]
        [DisplayName("身份证号")]
        public string? IdCardNumber { get; set; }

        /// <summary>手机号码</summary>
        [StringLength(11)]
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender? Gender { get; set; }

        /// <summary>年龄范围-最小</summary>
        [Range(0, 150)]
        [DisplayName("最小年龄")]
        public int? MinAge { get; set; }

        /// <summary>年龄范围-最大</summary>
        [Range(0, 150)]
        [DisplayName("最大年龄")]
        public int? MaxAge { get; set; }

        /// <summary>就诊日期范围-开始</summary>
        [DisplayName("就诊开始日期")]
        public DateTime? VisitStartDate { get; set; }

        /// <summary>就诊日期范围-结束</summary>
        [DisplayName("就诊结束日期")]
        public DateTime? VisitEndDate { get; set; }

        /// <summary>地址关键词</summary>
        [StringLength(100)]
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>过敏史关键词</summary>
        [StringLength(100)]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>慢性病史关键词</summary>
        [StringLength(100)]
        [DisplayName("慢性病史")]
        public string? ChronicDiseases { get; set; }

        /// <summary>标签ID列表</summary>
        [DisplayName("标签")]
        public List<Guid>? TagIds { get; set; }

        /// <summary>就诊次数-最小</summary>
        [Range(0, int.MaxValue)]
        [DisplayName("最小就诊次数")]
        public int? MinVisitCount { get; set; }

        /// <summary>就诊次数-最大</summary>
        [Range(0, int.MaxValue)]
        [DisplayName("最大就诊次数")]
        public int? MaxVisitCount { get; set; }
    }

    #endregion 高级搜索DTO

    #region 标签管理DTO

    /// <summary>
    /// 患者标签DTO
    /// </summary>
    public class PatientTagDto : BaseDto
    {

        /// <summary>标签名称</summary>
        [Required(ErrorMessage = "标签名称不能为空")]
        [StringLength(50, ErrorMessage = "标签名称长度不能超过50个字符")]
        [DisplayName("标签名称")]
        public string TagName { get; set; } = string.Empty;

        /// <summary>标签颜色</summary>
        [StringLength(7, ErrorMessage = "颜色代码长度不能超过7个字符")]
        [DisplayName("标签颜色")]
        public string? Color { get; set; }

        /// <summary>标签描述</summary>
        [StringLength(200, ErrorMessage = "标签描述长度不能超过200个字符")]
        [DisplayName("标签描述")]
        public string? Description { get; set; }

        /// <summary>使用次数</summary>
        [DisplayName("使用次数")]
        public int UsageCount { get; set; }

        /// <summary>排序号</summary>
        [DisplayName("排序号")]
        public int SortOrder { get; set; }

        /// <summary>是否系统标签</summary>
        [DisplayName("系统标签")]
        public bool IsSystem { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
    }

    #endregion 标签管理DTO
}
