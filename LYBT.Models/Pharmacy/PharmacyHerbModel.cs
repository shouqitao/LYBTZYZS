using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Pharmacy {
    /// <summary>
    /// 药房与药材多对多关联实体
    /// </summary>
    public class PharmacyHerbModel {
        /// <summary>药房单ID</summary>
        [DisplayName("药房单ID")]
        public Guid PharmacyId { get; set; }

        /// <summary>药材ID</summary>
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }
    }
}
