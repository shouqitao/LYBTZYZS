using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 报表趋势聚合粒度。
    /// </summary>
    public enum ReportGranularity
    {
        /// <summary>按日</summary>
        [Description("按日")]
        Day = 0,

        /// <summary>按周</summary>
        [Description("按周")]
        Week = 1,

        /// <summary>按月</summary>
        [Description("按月")]
        Month = 2
    }
}
