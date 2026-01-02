using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 批量获取医案详情请求DTO
    /// OpenSpec: consolidate-medicalcase-detail-queries
    /// </summary>
    public class BatchDetailQueryDto
    {
        /// <summary>
        /// 要获取的医案ID列表（最多50个）
        /// </summary>
        [Required(ErrorMessage = "ID列表不能为空")]
        [MinLength(1, ErrorMessage = "至少选择一个医案")]
        [MaxLength(50, ErrorMessage = "单次最多查询50个医案")]
        [DisplayName("ID列表")]
        public List<Guid> Ids { get; set; } = new();
    }
}
