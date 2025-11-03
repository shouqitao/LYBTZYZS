namespace LYBT.Desktop.Infrastructure.Interfaces.Components
{
    /// <summary>
    /// 命令处理器接口 - 组件化MVVM架构核心接口
    /// Issue #1776 Task 3: 组件化基础设施搭建
    ///
    /// 职责：
    /// 1. 处理业务命令（Save、Delete、Navigate等）
    /// 2. 封装业务规则和权限检查
    /// 3. 协调ViewModel的命令执行
    ///
    /// 设计原则：
    /// - 命令模式：统一的命令执行接口
    /// - 业务封装：将业务逻辑从ViewModel抽离
    /// - 可扩展：支持自定义命令
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="commandName">命令名称（如："Save", "Delete", "Navigate"）</param>
        /// <param name="parameter">命令参数（可选）</param>
        /// <returns>命令执行是否成功</returns>
        Task<bool> ExecuteAsync(string commandName, object? parameter = null);

        /// <summary>
        /// 检查命令是否可执行
        /// </summary>
        /// <param name="commandName">命令名称</param>
        /// <returns>命令是否可执行</returns>
        bool CanExecute(string commandName);
    }
}
