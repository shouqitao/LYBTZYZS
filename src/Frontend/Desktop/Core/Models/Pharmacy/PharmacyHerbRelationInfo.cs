using LYBT.Shared.Models.Core;
using System;

namespace LYBT.WPF.Client.Core.Models.Pharmacy {
    /// <summary>
    /// 药房与药材关联信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class PharmacyHerbRelationInfo : BasePharmacyHerbModel {
        /// <summary>药材名称（前端显示字段）</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>药材规格（前端显示字段）</summary>
        public string HerbSpecification { get; set; } = string.Empty;

        /// <summary>药材单位（前端显示字段）</summary>
        public string HerbUnit { get; set; } = string.Empty;

        /// <summary>药材价格（前端显示字段）</summary>
        public decimal HerbPrice { get; set; }

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }
    }
}