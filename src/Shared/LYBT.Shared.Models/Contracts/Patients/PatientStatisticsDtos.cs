using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Patients {

    /// <summary>
    /// 患者统计信息DTO
    /// </summary>
    public class PatientStatisticsDto {
        /// <summary>
        /// 患者总数
        /// </summary>
        public int TotalPatients { get; set; }
        
        /// <summary>
        /// 活跃患者数（启用状态）
        /// </summary>
        public int ActivePatients { get; set; }
        
        /// <summary>
        /// 非活跃患者数（禁用状态）
        /// </summary>
        public int InactivePatients { get; set; }
        
        /// <summary>
        /// 新增患者数（指定时间段内）
        /// </summary>
        public int NewPatients { get; set; }
        
        /// <summary>
        /// 男性患者数
        /// </summary>
        public int MaleCount { get; set; }
        
        /// <summary>
        /// 女性患者数
        /// </summary>
        public int FemaleCount { get; set; }
        
        /// <summary>
        /// 平均年龄
        /// </summary>
        public double AverageAge { get; set; }
        
        /// <summary>
        /// 总就诊次数
        /// </summary>
        public int TotalVisits { get; set; }
        
        /// <summary>
        /// 平均就诊次数
        /// </summary>
        public double AverageVisits { get; set; }
        
        /// <summary>
        /// 有过敏史的患者数
        /// </summary>
        public int PatientsWithAllergy { get; set; }
        
        /// <summary>
        /// 今日新增患者数
        /// </summary>
        public int TodayNewPatients { get; set; }
        
        /// <summary>
        /// 本月新增患者数
        /// </summary>
        public int MonthNewPatients { get; set; }
        
        /// <summary>
        /// 流失患者数（180天未就诊）
        /// </summary>
        public int LostPatients { get; set; }
    }

    /// <summary>
    /// 年龄分布DTO
    /// </summary>
    public class AgeDistributionDto {
        /// <summary>
        /// 年龄段
        /// </summary>
        public string AgeRange { get; set; } = string.Empty;
        
        /// <summary>
        /// 最小年龄
        /// </summary>
        public int MinAge { get; set; }
        
        /// <summary>
        /// 最大年龄
        /// </summary>
        public int MaxAge { get; set; }
        
        /// <summary>
        /// 患者数量
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// 占比
        /// </summary>
        public double Percentage { get; set; }
        
        /// <summary>
        /// 男性数量
        /// </summary>
        public int MaleCount { get; set; }
        
        /// <summary>
        /// 女性数量
        /// </summary>
        public int FemaleCount { get; set; }
    }

    /// <summary>
    /// 性别分布DTO
    /// </summary>
    public class GenderDistributionDto {
        /// <summary>
        /// 男性数量
        /// </summary>
        public int MaleCount { get; set; }
        
        /// <summary>
        /// 男性占比
        /// </summary>
        public double MalePercentage { get; set; }
        
        /// <summary>
        /// 女性数量
        /// </summary>
        public int FemaleCount { get; set; }
        
        /// <summary>
        /// 女性占比
        /// </summary>
        public double FemalePercentage { get; set; }
        
        /// <summary>
        /// 未知性别数量
        /// </summary>
        public int UnknownCount { get; set; }
        
        /// <summary>
        /// 未知性别占比
        /// </summary>
        public double UnknownPercentage { get; set; }
        
        /// <summary>
        /// 总数
        /// </summary>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 患者趋势DTO
    /// </summary>
    public class PatientTrendDto {
        /// <summary>
        /// 年份
        /// </summary>
        public int Year { get; set; }
        
        /// <summary>
        /// 月份
        /// </summary>
        public int Month { get; set; }
        
        /// <summary>
        /// 月份名称
        /// </summary>
        public string MonthName { get; set; } = string.Empty;
        
        /// <summary>
        /// 新增患者数
        /// </summary>
        public int NewPatients { get; set; }
        
        /// <summary>
        /// 就诊人次
        /// </summary>
        public int VisitCount { get; set; }
        
        /// <summary>
        /// 男性新增数
        /// </summary>
        public int MaleCount { get; set; }
        
        /// <summary>
        /// 女性新增数
        /// </summary>
        public int FemaleCount { get; set; }
        
        /// <summary>
        /// 环比增长率
        /// </summary>
        public double GrowthRate { get; set; }
    }

    /// <summary>
    /// 职业分布DTO
    /// </summary>
    public class OccupationDistributionDto {
        public string Occupation { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 地区分布DTO
    /// </summary>
    public class RegionDistributionDto {
        public string Region { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 患者活跃度DTO
    /// </summary>
    public class PatientActivityDto {
        /// <summary>
        /// 高活跃度患者（月均就诊>=3次）
        /// </summary>
        public int HighActivityCount { get; set; }
        
        /// <summary>
        /// 中活跃度患者（月均就诊1-2次）
        /// </summary>
        public int MediumActivityCount { get; set; }
        
        /// <summary>
        /// 低活跃度患者（月均就诊<1次）
        /// </summary>
        public int LowActivityCount { get; set; }
        
        /// <summary>
        /// 流失患者（180天未就诊）
        /// </summary>
        public int LostCount { get; set; }
    }
}