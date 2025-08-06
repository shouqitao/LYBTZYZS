using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Queueing {

    /// <summary>
    /// 排队实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class QueueingModel : BaseQueueingModel {
        
        /// <summary>
        /// 关联的挂号ID
        /// </summary>
        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }
        
        /// <summary>
        /// 排队号（按医生当天递增）
        /// </summary>
        [DisplayName("排队号")]
        public int QueueNumber { get; set; }
        
        /// <summary>
        /// 预计就诊时间
        /// </summary>
        [DisplayName("预计就诊时间")]
        public DateTime? EstimatedTime { get; set; }
        
        /// <summary>
        /// 实际就诊时间
        /// </summary>
        [DisplayName("实际就诊时间")]
        public DateTime? ActualTime { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }
    }
}