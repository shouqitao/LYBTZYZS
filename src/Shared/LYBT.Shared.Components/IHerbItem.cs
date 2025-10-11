namespace LYBT.Shared.Components
{
    /// <summary>
    /// 药材项目接口 - 用于共享组件的泛型约束
    /// </summary>
    public interface IHerbItem
    {
        /// <summary>
        /// 药材ID
        /// </summary>
        Guid HerbId { get; }

        /// <summary>
        /// 药材名称
        /// </summary>
        string HerbName { get; }

        /// <summary>
        /// 剂量
        /// </summary>
        decimal Dosage { get; }

        /// <summary>
        /// 单位
        /// </summary>
        string Unit { get; }

        /// <summary>
        /// 数量（克重）
        /// </summary>
        decimal Quantity { get; }

        /// <summary>
        /// 单价
        /// </summary>
        decimal UnitPrice { get; }
    }
}
