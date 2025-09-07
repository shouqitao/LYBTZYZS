using System.ComponentModel;

namespace LYBT.Entities.Common
{

    /// <summary>
    /// 药材项基础接口 - 定义药材在不同场景下的通用属性
    /// </summary>
    public interface IHerbItem
    {

        /// <summary>
        /// 药材ID（关联药材库）
        /// </summary>
        [DisplayName("药材ID")]
        Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        [DisplayName("药材名称")]
        string HerbName { get; set; }

        /// <summary>
        /// 剂量（实际用量）
        /// </summary>
        [DisplayName("剂量")]
        decimal Quantity { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        [DisplayName("单位")]
        string Unit { get; set; }
    }
}
