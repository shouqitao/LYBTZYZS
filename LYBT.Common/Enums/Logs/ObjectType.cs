using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.Common.Enums.Logs {
    /// <summary>
    /// 操作对象类型枚举
    /// </summary>
    public enum ObjectType {
        /// <summary>
        /// 用户
        /// </summary>
        [Description("用户")]
        User = 1,

        /// <summary>
        /// 患者
        /// </summary>
        [Description("患者")]
        Patient = 2,

        /// <summary>
        /// 病历
        /// </summary>
        [Description("病历")]
        Record = 3,

        /// <summary>
        /// 药方
        /// </summary>
        [Description("药方")]
        Prescription = 4,

        /// <summary>
        /// 未知
        /// </summary>
        [Description("未知")]
        Unknown = 99
    }
}
