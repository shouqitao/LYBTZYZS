using LYBT.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Pharmacy.Dtos {

    /// <summary>
    /// 编辑药房单 DTO
    /// </summary>
    public class PharmacyEditDto {

        /// <summary>药房单ID</summary>
        [Required(ErrorMessage = "药房单ID不能为空")]
        [DisplayName("药房单ID")]
        public Guid Id { get; set; }

        /// <summary>药房操作员ID</summary>
        [Required(ErrorMessage = "药房操作员ID不能为空")]
        [DisplayName("药房操作员ID")]
        public Guid OperatorId { get; set; }

        /// <summary>抓药时间</summary>
        [DisplayName("抓药时间")]
        public DateTime DispenseTime { get; set; }

        /// <summary>药房状态</summary>
        [DisplayName("药房状态")]
        public PharmacyStatus Status { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}