using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 处方项目DTO - 扁平化设计
    /// OpenSpec: refactor-dto-simplification - 移除继承，直接定义Id字段
    /// </summary>
    public class PrescriptionItemDto
    {
        /// <summary>唯一标识符</summary>
        [DisplayName("ID")]
        public Guid Id { get; set; }

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
