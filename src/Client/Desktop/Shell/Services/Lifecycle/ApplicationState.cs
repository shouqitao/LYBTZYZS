namespace LYBT.Desktop.Shell.Services.Lifecycle;

/// <summary>
/// 应用程序生命周期状态枚举
/// 定义应用从启动到运行的各个阶段
/// </summary>
public enum ApplicationState
{
    /// <summary>
    /// 未启动 - 应用尚未开始初始化
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// 初始化中 - 正在初始化容器、核心服务
    /// </summary>
    Initializing = 1,

    /// <summary>
    /// 认证中 - 显示登录界面，等待用户登录
    /// </summary>
    Authenticating = 2,

    /// <summary>
    /// 就绪 - 登录成功，正在加载模块
    /// </summary>
    Ready = 3,

    /// <summary>
    /// 运行中 - 应用正常运行
    /// </summary>
    Running = 4,

    /// <summary>
    /// 关闭中 - 应用正在退出
    /// </summary>
    ShuttingDown = 5
}
