namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 药材角色（君臣佐使）
    /// </summary>
    public enum HerbRole
    {
        /// <summary>
        /// 未指定
        /// </summary>
        None = 0,

        /// <summary>
        /// 君药 — 主药，针对主症
        /// </summary>
        Sovereign = 1,

        /// <summary>
        /// 臣药 — 辅助君药
        /// </summary>
        Minister = 2,

        /// <summary>
        /// 佐药 — 治疗次要症状或调和诸药
        /// </summary>
        Assistant = 3,

        /// <summary>
        /// 使药 — 引经药，引导药力到达病所
        /// </summary>
        Guide = 4
    }
}
