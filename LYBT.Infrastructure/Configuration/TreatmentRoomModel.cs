using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration {

    /// <summary>
    /// 治疗室实体模型
    /// </summary>
    public class TreatmentRoomModel {

        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [DisplayName("主键ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 治疗室编号
        /// </summary>
        [Required, StringLength(20)]
        [DisplayName("治疗室编号")]
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>
        /// 治疗室名称
        /// </summary>
        [Required, StringLength(50)]
        [DisplayName("治疗室名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 治疗室类型
        /// </summary>
        [StringLength(50)]
        [DisplayName("治疗室类型")]
        public string? RoomType { get; set; }

        /// <summary>
        /// 楼层
        /// </summary>
        [DisplayName("楼层")]
        public int? Floor { get; set; }

        /// <summary>
        /// 位置描述
        /// </summary>
        [StringLength(200)]
        [DisplayName("位置描述")]
        public string? Location { get; set; }

        /// <summary>
        /// 容纳人数
        /// </summary>
        [DisplayName("容纳人数")]
        public int? Capacity { get; set; }

        /// <summary>
        /// 设备配置
        /// </summary>
        [StringLength(1000)]
        [DisplayName("设备配置")]
        public string? Equipment { get; set; }

        /// <summary>
        /// 负责医生ID
        /// </summary>
        [DisplayName("负责医生ID")]
        public Guid? ResponsibleDoctorId { get; set; }

        /// <summary>
        /// 负责医生姓名
        /// </summary>
        [StringLength(50)]
        [DisplayName("负责医生姓名")]
        public string? ResponsibleDoctorName { get; set; }

        /// <summary>
        /// 使用状态（Available/Occupied/Maintenance）
        /// </summary>
        [StringLength(20)]
        [DisplayName("使用状态")]
        public string Status { get; set; } = "Available";

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 排序序号
        /// </summary>
        [DisplayName("排序序号")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [DisplayName("更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建者ID
        /// </summary>
        [DisplayName("创建者ID")]
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// 更新者ID
        /// </summary>
        [DisplayName("更新者ID")]
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}