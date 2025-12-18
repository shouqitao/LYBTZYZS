using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 特价设置请求
    /// </summary>
    public class SpecialPriceRequest
    {

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "特价必须大于0")]
        public decimal SpecialPrice { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string? Description { get; set; }


    }

}
