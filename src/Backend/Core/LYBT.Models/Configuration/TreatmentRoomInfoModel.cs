using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Configuration {

    /// <summary>
    /// 治疗室基本信息模型
    /// </summary>
    public class TreatmentRoomInfoModel {

        /// <summary>
        /// 主键ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 房间号
        /// </summary>
        [Required]
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>
        /// 房间名称
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 房间类型
        /// </summary>
        public string? RoomType { get; set; }

        /// <summary>
        /// 位置信息
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// 设备信息
        /// </summary>
        public string? Equipment { get; set; }

        /// <summary>
        /// 负责医生ID
        /// </summary>
        public Guid? ResponsibleDoctorId { get; set; }

        /// <summary>
        /// 负责医生姓名
        /// </summary>
        public string? ResponsibleDoctorName { get; set; }

        /// <summary>
        /// 房间状态
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}