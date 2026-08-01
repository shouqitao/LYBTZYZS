namespace LYBT.Desktop.Auth.Models;

/// <summary>
/// 连接测试状态 (UI 显示用枚举)
/// 由 FirstRunSetupViewModel 与 ServerConfigViewModel 共享
/// </summary>
public enum ConnectionTestStatus
{
    Idle,
    Testing,
    Success,
    Failed
}
