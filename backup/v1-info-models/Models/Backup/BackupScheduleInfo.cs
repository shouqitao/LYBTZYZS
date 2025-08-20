using LYBT.Shared.Models.Contracts.Common;
using System;

namespace LYBT.Desktop.Core.Models.Backup
{
    /// <summary>
    /// 备份计划信息
    /// </summary>
    public class BackupScheduleInfo
    {
        /// <summary>计划ID</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>计划名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>备份类型</summary>
        public BackupType BackupType { get; set; }

        /// <summary>计划类型</summary>
        public ScheduleType ScheduleType { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; }

        /// <summary>执行时间（每天的具体时间）</summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>星期几执行（周计划使用）</summary>
        public DayOfWeek? DayOfWeek { get; set; }

        /// <summary>每月几号执行（月计划使用）</summary>
        public int? DayOfMonth { get; set; }

        /// <summary>保留备份数量</summary>
        public int RetentionCount { get; set; } = 7;

        /// <summary>上次执行时间</summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>下次执行时间</summary>
        public DateTime? NextExecutionTime { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>备份路径</summary>
        public string BackupPath { get; set; } = string.Empty;

        /// <summary>备份说明</summary>
        public string? Description { get; set; }

        #region 显示属性

        /// <summary>计划类型名称</summary>
        public string ScheduleTypeName => GetScheduleTypeName();

        /// <summary>执行时间显示</summary>
        public string ExecutionTimeDisplay => GetExecutionTimeDisplay();

        /// <summary>状态显示</summary>
        public string StatusDisplay => IsEnabled ? "已启用" : "已禁用";

        /// <summary>状态颜色</summary>
        public string StatusColor => IsEnabled ? "#28A745" : "#6C757D";

        #endregion

        #region 私有方法

        private string GetScheduleTypeName()
        {
            return ScheduleType switch
            {
                ScheduleType.Daily => "每日",
                ScheduleType.Weekly => "每周",
                ScheduleType.Monthly => "每月",
                _ => "未知"
            };
        }

        private string GetExecutionTimeDisplay()
        {
            var timeStr = ExecutionTime.ToString(@"hh\:mm");

            return ScheduleType switch
            {
                ScheduleType.Daily => $"每天 {timeStr}",
                ScheduleType.Weekly => $"每周{GetDayOfWeekName()} {timeStr}",
                ScheduleType.Monthly => $"每月{DayOfMonth}号 {timeStr}",
                _ => timeStr
            };
        }

        private string GetDayOfWeekName()
        {
            return DayOfWeek switch
            {
                System.DayOfWeek.Monday => "一",
                System.DayOfWeek.Tuesday => "二",
                System.DayOfWeek.Wednesday => "三",
                System.DayOfWeek.Thursday => "四",
                System.DayOfWeek.Friday => "五",
                System.DayOfWeek.Saturday => "六",
                System.DayOfWeek.Sunday => "日",
                _ => ""
            };
        }

        #endregion
    }

    /// <summary>
    /// 计划类型枚举
    /// </summary>
    public enum ScheduleType
    {
        /// <summary>每日</summary>
        Daily = 0,

        /// <summary>每周</summary>
        Weekly = 1,

        /// <summary>每月</summary>
        Monthly = 2
    }
}