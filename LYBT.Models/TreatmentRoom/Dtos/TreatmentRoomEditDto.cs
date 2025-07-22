using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.TreatmentRoom.Dtos {

    /// <summary>
    /// 编辑治疗室单 DTO
    /// </summary>
    public class TreatmentRoomEditDto {

        /// <summary>治疗室单ID</summary>
        [Required(ErrorMessage = "治疗室单ID不能为空")]
        [DisplayName("治疗室单ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>诊疗项目</summary>
        [Required(ErrorMessage = "诊疗项目不能为空")]
        [DisplayName("诊疗项目")]
/// <summary>
/// TreatmentItem 属性。
/// </summary>
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗次数</summary>
        [Range(1, int.MaxValue, ErrorMessage = "次数必须大于0")]
        [DisplayName("治疗次数")]
/// <summary>
/// Count 属性。
/// </summary>
        public int Count { get; set; } = 1;

        /// <summary>治疗状态</summary>
        [DisplayName("治疗状态")]
/// <summary>
/// Status 属性。
/// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>治疗结束时间</summary>
        [DisplayName("治疗结束时间")]
/// <summary>
/// EndTime 属性。
/// </summary>
        public DateTime EndTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
