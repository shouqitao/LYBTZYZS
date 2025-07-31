namespace LYBT.WPF.Client.Core.Models.Enums
{
    /// <summary>
    /// 患者状态枚举
    /// </summary>
    public enum PatientStatus
    {
        /// <summary>正常</summary>
        Normal = 0,
        /// <summary>就诊中</summary>
        InConsultation = 1,
        /// <summary>已完成</summary>
        Completed = 2,
        /// <summary>已取消</summary>
        Cancelled = 3,
        /// <summary>暂停</summary>
        Suspended = 4
    }
}