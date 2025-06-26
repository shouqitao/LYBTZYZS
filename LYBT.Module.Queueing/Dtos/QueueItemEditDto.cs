using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Queueing.Dtos {

    /// <summary>
    /// 编辑排队信息 DTO
    /// </summary>
    public class QueueingEditDto {

        /// <summary>排队ID</summary>
        [Required(ErrorMessage = "排队ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>排队类型</summary>
        [Required(ErrorMessage = "排队类型不能为空")]
        public string QueueType { get; set; } = "普通";

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}