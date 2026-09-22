namespace LYBT.Desktop.Auth.Models;

/// <summary>
/// 初始化向导步骤（B-07）。
/// </summary>
public enum InitializationWizardStep
{
    /// <summary>欢迎 + 连接模式选择（本地全栈 / 远程服务器）</summary>
    Welcome = 1,

    /// <summary>模式相关配置（本地：数据库连接；远程：服务器地址）</summary>
    Connection = 2,

    /// <summary>诊所信息（名称/科室/地址/电话）</summary>
    Clinic = 3,

    /// <summary>初始管理员账号创建</summary>
    Administrator = 4,

    /// <summary>配置校验与完成</summary>
    Finish = 5
}
