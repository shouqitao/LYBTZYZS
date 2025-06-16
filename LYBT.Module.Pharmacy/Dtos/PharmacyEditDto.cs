using LYBT.Common.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Pharmacy.Dtos {
    /// <summary>
    /// 编辑药房单 DTO
    /// </summary>
    public class PharmacyEditDto {
        /// <summary>药房单ID</summary>
        [Required(ErrorMessage = "药房单ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>药房操作员ID</summary>
        [Required(ErrorMessage = "药房操作员ID不能为空")]
        public string OperatorId { get; set; } = string.Empty;

        /// <summary>抓药时间</summary>
        public DateTime DispenseTime { get; set; }

        /// <summary>药房状态</summary>
        public PharmacyStatus Status { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
