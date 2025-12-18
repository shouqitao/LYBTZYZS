using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 编辑处方DTO - 用于API接口
    /// OpenSpec: optimize-entity-data-flow - 保留以兼容现有API
    /// </summary>
    public class PrescriptionEditDto : IIdentifiable<Guid>, IRemarkable
    {
        /// <inheritdoc/>
        [Required(ErrorMessage = "处方ID不能为空")]
        [DisplayName("处方ID")]
        public Guid Id { get; set; }

        /// <summary>诊断</summary>
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>剂数</summary>
        [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
        [DisplayName("剂数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>用法</summary>
        [StringLength(200, ErrorMessage = "用法长度不能超过200个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(500, ErrorMessage = "用药建议不能超过500个字符")]
        [DisplayName("医嘱")]
        public string? Advice { get; set; }

        /// <summary>总价格</summary>
        [Range(0, double.MaxValue, ErrorMessage = "总价格必须大于等于0")]
        [DisplayName("总价格")]
        public decimal TotalPrice { get; set; }

        /// <summary>折扣</summary>
        [Range(0, 1, ErrorMessage = "折扣必须在0-1之间")]
        [DisplayName("折扣")]
        public decimal Discount { get; set; } = 1.0m;

        /// <inheritdoc/>
        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>处方项目</summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemInputDto> Items { get; set; } = new();
    }
}
