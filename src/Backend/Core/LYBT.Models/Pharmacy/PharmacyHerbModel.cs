using LYBT.Models.Herbs;
using LYBT.Shared.Models.Core;
using System.ComponentModel;

namespace LYBT.Models.Pharmacy {

    /// <summary>
    /// 药房与药材多对多关联实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class PharmacyHerbModel : BasePharmacyHerbModel {
        // 所有字段已在BasePharmacyHerbModel中定义
        
        /// <summary>
        /// 导航属性 - 药房
        /// </summary>
        public virtual PharmacyModel? Pharmacy { get; set; }
        
        /// <summary>
        /// 导航属性 - 药材
        /// </summary>
        public virtual HerbModel? Herb { get; set; }
        
        /// <summary>
        /// 数量
        /// </summary>
        public decimal Quantity { get; set; }
        
        /// <summary>
        /// 单位
        /// </summary>
        public string? Unit { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}