using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;

namespace LYBT.Models.Prescriptions {

    /// <summary>
    /// 表示PrescriptionDetailDto。
    /// </summary>
    public class PrescriptionDetailDto {

        [DisplayName("Id")]
        public Guid Id { get; set; }

        [DisplayName("PatientId")]
        public Guid PatientId { get; set; }

        [DisplayName("DoctorId")]
        public Guid DoctorId { get; set; }

        [DisplayName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [DisplayName("Diagnosis")]
        public string? Diagnosis { get; set; }

        [DisplayName("Remark")]
        public string? Remark { get; set; }

        [DisplayName("Status")]
        public PrescriptionStatus Status { get; set; }

        [DisplayName("Items")]
        public List<PrescriptionItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 表示PrescriptionItemDto。
    /// </summary>
    public class PrescriptionItemDto {

        [DisplayName("Id")]
        public Guid Id { get; set; }

        [DisplayName("HerbId")]
        public Guid HerbId { get; set; }

        [DisplayName("HerbName")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("Quantity")]
        public decimal Quantity { get; set; }

        [DisplayName("Unit")]
        public string? Unit { get; set; }

        [DisplayName("Usage")]
        public string? Usage { get; set; }
    }
}