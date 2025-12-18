namespace LYBT.Shared.Models.Contracts.Users;

/// <summary>
/// 用户统计DTO - record类型，不可变数据
/// OpenSpec: refactor-dto-simplification - 统计类型使用record定义
/// </summary>
/// <param name="TotalCount">用户总数</param>
/// <param name="ActiveCount">活跃用户数</param>
/// <param name="DisabledCount">禁用用户数</param>
/// <param name="DoctorCount">医生数量</param>
/// <param name="AdminCount">管理员数量</param>
/// <param name="TodayLoginCount">今日登录用户数</param>
public record UserStatistics(
    int TotalCount,
    int ActiveCount,
    int DisabledCount,
    int DoctorCount,
    int AdminCount,
    int TodayLoginCount
);
