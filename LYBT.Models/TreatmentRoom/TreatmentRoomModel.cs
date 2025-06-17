using LYBT.Common.Enums;

namespace LYBT.Models.TreatmentRoom {
    /// <summary>
    /// 诊疗室基础信息
    /// </summary>
    public class TreatmentRoomModel {
        public string Id { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public string? BoundDoctorId { get; set; }
        public TreatmentRoomStatus Status { get; set; } = TreatmentRoomStatus.Idle;
        public string? Description { get; set; }
    }
}
