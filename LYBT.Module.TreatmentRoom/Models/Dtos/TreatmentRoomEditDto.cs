using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.TreatmentRoom.Models.Dtos {

    /// <summary>
    /// 编辑治疗室单 DTO
    /// </summary>
    public class TreatmentRoomEditDto {

        /// <summary>治疗室单ID</summary>
        [Required(ErrorMessage = "治疗室单ID不能为空")]
        [DisplayName("治疗室单ID")]
        public Guid Id { get; set; }

        /// <summary>诊疗项目</summary>
        [Required(ErrorMessage = "诊疗项目不能为空")]
        [DisplayName("诊疗项目")]
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗次数</summary>
        [Range(1, int.MaxValue, ErrorMessage = "次数必须大于0")]
        [DisplayName("治疗次数")]
        public int Count { get; set; } = 1;

        /// <summary>治疗状态</summary>
        [DisplayName("治疗状态")]
        public string Status { get; set; } = string.Empty;

        /// <summary>治疗结束时间</summary>
        [DisplayName("治疗结束时间")]
        public DateTime EndTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}