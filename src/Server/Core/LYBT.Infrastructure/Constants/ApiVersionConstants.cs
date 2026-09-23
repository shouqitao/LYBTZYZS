namespace LYBT.Infrastructure.Constants;

/// <summary>
/// API 版本相关常量（ADR-0015: URL Path Versioning，整数递增，无子版本）
/// </summary>
public static class ApiVersionConstants
{
    /// <summary>v1 —— 当前唯一对外版本（全部控制器已声明）</summary>
    public const string V1 = "1";

    /// <summary>
    /// v2 —— <b>保留值，尚无任何控制器声明</b>。
    /// ADR-0015 规定：仅在出现破坏性变更（删除/重命名端点、改变必需字段或语义）时才切 v2；
    /// 切换动作 = 对受影响的控制器添加 <c>[ApiVersion("2")]</c> 并同步路由/客户端，
    /// 未受影响的控制器继续只声明 v1（v1/v2 共存 ≥6 个月，弃用走 [Obsolete] + 过渡期后移除）。
    /// 切换清单见 docs/03-architecture/decisions/0015-api-versioning-strategy.md。
    /// </summary>
    public const string V2 = "2";
}
