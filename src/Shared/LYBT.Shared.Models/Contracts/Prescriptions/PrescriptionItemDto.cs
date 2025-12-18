using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 处方项目DTO - 继承基础DTO提供ID字段
    /// </summary>
    public class PrescriptionItemDto : BaseDto, IRemarkable
    {
        [DisplayName("中药材ID")]
        public Guid HerbId { get; set; }

        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        [DisplayName("剂量")]
        public int Dosage { get; set; }

        [DisplayName("总价")]
        public decimal TotalPrice { get; set; }

        [DisplayName("总重量")]
        public decimal TotalWeight { get; set; }

        [DisplayName("小计金额")]
        public decimal Subtotal { get; set; }

        [DisplayName("用法说明")]
        public string? Usage { get; set; }

        [DisplayName("煎法")]
        public Enums.DecocteMethod DecocteMethod { get; set; } = Enums.DecocteMethod.Default;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        /// <summary>
        /// 备注(兼容旧代码)
        /// </summary>
        [DisplayName("备注")]
        public string? Notes { get => Remark; set => Remark = value; }
    }
}
