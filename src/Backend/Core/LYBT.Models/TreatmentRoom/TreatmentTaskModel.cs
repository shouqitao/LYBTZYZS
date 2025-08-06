using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.TreatmentRoom {

    /// <summary>
    /// 治疗任务实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class TreatmentTaskModel : BaseTreatmentRoomModel {
        // 所有字段已在BaseTreatmentRoomModel中定义
        // 注：这个模型用于记录治疗任务的执行情况
        
        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid MedicalCaseId { get; set; }
        
        /// <summary>
        /// 治疗师ID
        /// </summary>
        public Guid? TherapistId { get; set; }
        
        /// <summary>
        /// 治疗室ID
        /// </summary>
        public Guid? RoomId { get; set; }
        
        /// <summary>
        /// 开始时间（可空）
        /// </summary>
        public new DateTime? StartTime { get; set; }
        
        /// <summary>
        /// 结束时间（可空）
        /// </summary>
        public new DateTime? EndTime { get; set; }
    }
    
    /// <summary>
    /// 治疗任务状态枚举
    /// </summary>
    public enum TreatmentTaskStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// 进行中
        /// </summary>
        InProgress = 1,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 3
    }
}