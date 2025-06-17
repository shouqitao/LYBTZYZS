using System;
using System.Collections.Generic;
using LYBT.Common.Enums;
using LYBT.Module.DiagnosisTreatment.Models.Dtos;

namespace LYBT.Models.TreatmentTask {
    /// <summary>
    /// 治疗任务模型
    /// </summary>
    public class TreatmentTaskModel {
        public string Id { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string DoctorId { get; set; } = string.Empty;
        public string TreatmentRoomId { get; set; } = string.Empty;

        public List<TreatmentItemDto> TreatmentItems { get; set; } = new();
        public TreatmentTaskStatus Status { get; set; } = TreatmentTaskStatus.Pending;

        public DateTime CreatedTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        public string? Remarks { get; set; }
    }
}
