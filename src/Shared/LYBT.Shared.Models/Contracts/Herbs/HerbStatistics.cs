namespace LYBT.Shared.Models.Contracts.Herbs;

/// <summary>
/// 药材统计 - record类型
/// OpenSpec: refactor-dto-simplification - 简化为record
/// </summary>
public record HerbStatistics(
    int TotalCount,
    int AvailableCount,
    int NearExpiryCount,
    int OriginCount
);

/// <summary>
/// 药材主页统计 - record类型
/// OpenSpec: refactor-dto-simplification
/// </summary>
public record HerbMainStatistics(
    int TotalCount,
    int AvailableCount,
    int RecentAddedCount
);
