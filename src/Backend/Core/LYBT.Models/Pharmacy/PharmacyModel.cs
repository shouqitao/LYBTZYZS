using LYBT.Models.Herbs;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Pharmacy {

    /// <summary>
    /// 药房任务实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class PharmacyModel : BasePharmacyModel {

        /// <summary>
        /// 药材列表（后端导航属性）
        /// </summary>
        [Required]
        [DisplayName("药材列表")]
        public List<HerbModel> Herbs { get; set; } = new();
    }
}