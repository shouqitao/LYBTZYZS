using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.TreatmentRoom.Dtos {

    /// <summary>
    /// 编辑治疗室单 DTO
    /// </summary>
    public class TreatmentRoomEditDto {

        /// <summary>治疗室单ID</summary>
        [Required(ErrorMessage = "治疗室单ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>诊疗项目</summary>
        [Required(ErrorMessage = "诊疗项目不能为空")]
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗次数</summary>
        [Range(1, int.MaxValue, ErrorMessage = "次数必须大于0")]
        public int Count { get; set; } = 1;

        /// <summary>治疗状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>治疗结束时间</summary>
        public DateTime EndTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}