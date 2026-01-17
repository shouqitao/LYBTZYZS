namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 异步初始化接口
/// OpenSpec: refactor-frontend-srp-patterns - 泛型控件基类支持
/// 实现此接口的ViewModel可以在View加载时自动执行初始化
/// </summary>
public interface IAsyncInitializable
{
    /// <summary>
    /// 异步初始化方法
    /// </summary>
    /// <returns>初始化任务</returns>
    Task InitializeAsync();
}
