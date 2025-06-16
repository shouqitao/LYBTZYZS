using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.Common.Enums.Patient {
    /// <summary>
    /// 患者状态枚举
    /// </summary>
    public enum PatientStatus {
        [Description("激活")]
        Active = 0,

        [Description("禁用")]
        Disabled = 1
    }
}
