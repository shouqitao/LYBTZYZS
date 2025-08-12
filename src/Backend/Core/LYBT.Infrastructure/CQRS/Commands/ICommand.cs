using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace LYBT.Infrastructure.CQRS.Commands
{
    /// <summary>
    /// 命令接口 - CQRS模式Command端
    /// UltraThink重构：实现读写分离，优化写操作性能
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    public interface ICommand<TResult> : IRequest<TResult>
    {
    }

    /// <summary>
    /// 无返回值命令接口
    /// </summary>
    public interface ICommand : IRequest
    {
    }

    /// <summary>
    /// 命令处理器接口
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
    }

    /// <summary>
    /// 无返回值命令处理器接口
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
        where TCommand : ICommand
    {
    }

    /// <summary>
    /// 命令基类 - 提供通用属性
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    public abstract record CommandBase<TResult> : ICommand<TResult>
    {
        /// <summary>
        /// 命令ID - 用于追踪和去重
        /// </summary>
        public string CommandId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// 执行用户ID
        /// </summary>
        public Guid? UserId { get; init; }

        /// <summary>
        /// 相关性ID - 用于分布式追踪
        /// </summary>
        public string CorrelationId { get; init; }

        /// <summary>
        /// 命令元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// 无返回值命令基类
    /// </summary>
    public abstract record CommandBase : ICommand
    {
        /// <summary>
        /// 命令ID - 用于追踪和去重
        /// </summary>
        public string CommandId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// 执行用户ID
        /// </summary>
        public Guid? UserId { get; init; }

        /// <summary>
        /// 相关性ID - 用于分布式追踪
        /// </summary>
        public string CorrelationId { get; init; }

        /// <summary>
        /// 命令元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// 命令执行结果
    /// </summary>
    /// <typeparam name="TData">数据类型</typeparam>
    public class CommandResult<TData>
    {
        public bool IsSuccess { get; init; }
        public TData Data { get; init; }
        public string ErrorMessage { get; init; }
        public Dictionary<string, string[]> ValidationErrors { get; init; } = new();
        public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
        public string CommandId { get; init; }

        /// <summary>
        /// 成功结果
        /// </summary>
        public static CommandResult<TData> Success(TData data, string commandId = null)
        {
            return new CommandResult<TData>
            {
                IsSuccess = true,
                Data = data,
                CommandId = commandId
            };
        }

        /// <summary>
        /// 失败结果
        /// </summary>
        public static CommandResult<TData> Failure(string errorMessage, string commandId = null)
        {
            return new CommandResult<TData>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                CommandId = commandId
            };
        }

        /// <summary>
        /// 验证失败结果
        /// </summary>
        public static CommandResult<TData> ValidationFailure(Dictionary<string, string[]> validationErrors, string commandId = null)
        {
            return new CommandResult<TData>
            {
                IsSuccess = false,
                ValidationErrors = validationErrors,
                CommandId = commandId
            };
        }
    }

    /// <summary>
    /// 无数据命令执行结果
    /// </summary>
    public class CommandResult
    {
        public bool IsSuccess { get; init; }
        public string ErrorMessage { get; init; }
        public Dictionary<string, string[]> ValidationErrors { get; init; } = new();
        public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
        public string CommandId { get; init; }

        /// <summary>
        /// 成功结果
        /// </summary>
        public static CommandResult Success(string commandId = null)
        {
            return new CommandResult
            {
                IsSuccess = true,
                CommandId = commandId
            };
        }

        /// <summary>
        /// 失败结果
        /// </summary>
        public static CommandResult Failure(string errorMessage, string commandId = null)
        {
            return new CommandResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                CommandId = commandId
            };
        }

        /// <summary>
        /// 验证失败结果
        /// </summary>
        public static CommandResult ValidationFailure(Dictionary<string, string[]> validationErrors, string commandId = null)
        {
            return new CommandResult
            {
                IsSuccess = false,
                ValidationErrors = validationErrors,
                CommandId = commandId
            };
        }
    }
}