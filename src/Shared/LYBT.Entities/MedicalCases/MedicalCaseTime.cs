using LYBT.Shared.Models.Primitives;

namespace LYBT.Entities.MedicalCases;

/// <summary>
/// 医案时间边界（P1-10）
/// 实现已下沉到 <see cref="ClinicTime"/>（Shared.Models），本类型保留实体域入口，供实体/服务调用。
/// 运营日界 = 诊所本地时间（默认 Asia/Shanghai），非 UTC——避免北京 00-08 时经 UTC 跨日误锁 Completed 医案。
/// 时区可通过环境变量 LYBT_CLINIC_TIMEZONE 覆盖（如 "Asia/Shanghai"）。
/// </summary>
public static class MedicalCaseTime
{
    /// <summary>UTC 时间 → 诊所本地日期</summary>
    public static DateTime ClinicLocalDate(DateTime utc) => ClinicTime.ClinicLocalDate(utc);
}

