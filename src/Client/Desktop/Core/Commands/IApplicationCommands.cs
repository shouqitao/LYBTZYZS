using Prism.Commands;

namespace LYBT.Desktop.Core.Commands
{
    /// <summary>
    /// 全局应用程序命令接口
    /// 定义跨模块的全局命令，支持CompositeCommand协调
    /// </summary>
    public interface IApplicationCommands
    {
        /// <summary>
        /// 全局保存命令 - 多个模块可以响应
        /// 快捷键: Ctrl+S
        /// </summary>
        CompositeCommand SaveAllCommand { get; }

        /// <summary>
        /// 全局刷新命令
        /// 快捷键: F5
        /// </summary>
        CompositeCommand RefreshAllCommand { get; }

        /// <summary>
        /// 全局验证命令
        /// 验证所有活动模块的数据有效性
        /// </summary>
        CompositeCommand ValidateAllCommand { get; }

        /// <summary>
        /// 全局打印命令
        /// 快捷键: Ctrl+P
        /// </summary>
        CompositeCommand PrintCommand { get; }

        /// <summary>
        /// 全局导出命令
        /// 导出当前活动数据
        /// </summary>
        CompositeCommand ExportCommand { get; }

        /// <summary>
        /// 工作台切换命令
        /// 切换不同的工作台视图
        /// </summary>
        CompositeCommand SwitchWorkbenchCommand { get; }

        /// <summary>
        /// 全局关闭命令
        /// 关闭所有打开的编辑器
        /// </summary>
        CompositeCommand CloseAllCommand { get; }

        /// <summary>
        /// 全局撤销命令
        /// 快捷键: Ctrl+Z
        /// </summary>
        CompositeCommand UndoCommand { get; }

        /// <summary>
        /// 全局重做命令
        /// 快捷键: Ctrl+Y
        /// </summary>
        CompositeCommand RedoCommand { get; }
    }
}