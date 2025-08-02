using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Records
{
    /// <summary>
    /// 病例统计DTO
    /// </summary>
    public class RecordStatisticsDto
    {
        /// <summary>总病例数</summary>
        public int TotalRecords { get; set; }

        /// <summary>今日新增</summary>
        public int TodayRecords { get; set; }

        /// <summary>本周新增</summary>
        public int WeekRecords { get; set; }

        /// <summary>本月新增</summary>
        public int MonthRecords { get; set; }

        /// <summary>按科室统计</summary>
        public Dictionary<string, int> DepartmentStatistics { get; set; } = new();

        /// <summary>按医生统计</summary>
        public Dictionary<string, int> DoctorStatistics { get; set; } = new();

        /// <summary>按诊断统计（前10）</summary>
        public List<DiagnosisStatistic> TopDiagnosis { get; set; } = new();

        /// <summary>按患者性别统计</summary>
        public Dictionary<string, int> GenderStatistics { get; set; } = new();

        /// <summary>按患者年龄段统计</summary>
        public Dictionary<string, int> AgeGroupStatistics { get; set; } = new();
    }

    /// <summary>
    /// 诊断统计
    /// </summary>
    public class DiagnosisStatistic
    {
        /// <summary>诊断名称</summary>
        public string DiagnosisName { get; set; } = string.Empty;

        /// <summary>病例数量</summary>
        public int Count { get; set; }

        /// <summary>占比</summary>
        public decimal Percentage { get; set; }
    }
}