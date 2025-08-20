using System;
using LYBT.Shared.Models.Core;

namespace LYBT.Desktop.Core.Models.Patients
{
    /// <summary>
    /// 患者信息 - UltraThink架构纯数据模型
    /// 不包含任何UI相关属性和逻辑，符合分离关注点原则
    /// </summary>
    public class PatientInfoClean : BasePatient
    {
        // 继承自BasePatient的所有数据属性
        // 不再包含任何UI相关属性和逻辑
    }
}