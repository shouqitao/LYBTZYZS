using System.ComponentModel;
namespace LYBT.Module.TreatmentRoom.Dtos {

    /// <summary>
    /// 治疗室单列表 DTO
    /// </summary>
    public class TreatmentRoomDto {

        /// <summary>治疗室单ID</summary>
        [DisplayName("治疗室单ID")]
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>诊疗项目</summary>
        [DisplayName("诊疗项目")]
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗状态</summary>
        [DisplayName("治疗状态")]
        public int Status { get; set; }

        /// <summary>治疗开始时间</summary>
        [DisplayName("治疗开始时间")]
        public DateTime StartTime { get; set; }
    }
}