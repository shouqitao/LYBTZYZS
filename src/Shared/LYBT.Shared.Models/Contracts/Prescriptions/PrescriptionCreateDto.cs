using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 创建处方DTO - 用于API接口
    /// OpenSpec: optimize-entity-data-flow - 保留以兼容现有API
    /// </summary>
    public class PrescriptionCreateDto : IRemarkable
    {
        /// <summary>处方编号</summary>
        [DisplayName("处方编号")]
        [StringLength(50)]
        public string? PrescriptionNumber { get; set; }

        /// <summary>诊断</summary>
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>剂数</summary>
        [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
        [DisplayName("剂数")]
        public int Quantity { get; set; } = 7;

        /// <summary>用法说明</summary>
        [StringLength(200, ErrorMessage = "用法说明不能超过200个字符")]
        [DisplayName("用法说明")]
        public string? Usage { get; set; }

        /// <summary>用药建议</summary>
        [StringLength(500, ErrorMessage = "用药建议不能超过500个字符")]
        [DisplayName("用药建议")]
        public string? Advice { get; set; }

        /// <summary>验方来源</summary>
        [StringLength(100, ErrorMessage = "方剂来源不能超过100个字符")]
        [DisplayName("方剂来源")]
        public string? FormulaSource { get; set; }

        /// <summary>总金额</summary>
        [Range(0, double.MaxValue, ErrorMessage = "总金额必须大于等于0")]
        [DisplayName("总金额")]
        public decimal TotalAmount { get; set; }

        /// <inheritdoc/>
        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>处方项目</summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemInputDto> Items { get; set; } = new();
    }
}
