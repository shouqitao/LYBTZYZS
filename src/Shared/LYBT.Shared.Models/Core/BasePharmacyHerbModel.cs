using System.ComponentModel;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 药房与药材关联基础模型 - 前后端共享
    /// </summary>
    public abstract class BasePharmacyHerbModel {

        /// <summary>药房单ID</summary>
        [DisplayName("药房单ID")]
        public Guid PharmacyId { get; set; }

        /// <summary>药材ID</summary>
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }
    }
}