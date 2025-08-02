using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Herbs
{
    /// <summary>
    /// 更新库存DTO
    /// </summary>
    public class UpdateStockDto
    {
        /// <summary>操作类型（1:入库 2:出库 3:盘点）</summary>
        [Required(ErrorMessage = "操作类型不能为空")]
        [Range(1, 3, ErrorMessage = "操作类型必须为1-3")]
        public int OperationType { get; set; }

        /// <summary>数量</summary>
        [Required(ErrorMessage = "数量不能为空")]
        [Range(0.01, 99999.99, ErrorMessage = "数量必须在0.01-99999.99之间")]
        public decimal Quantity { get; set; }

        /// <summary>备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        public string? Remark { get; set; }
    }
}