using System.Collections.Generic;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务定义
    /// </summary>
    /// <typeparam name="TContext">事务上下文类型</typeparam>
    public class TransactionDefinition<TContext>
    {
        /// <summary>
        /// 事务步骤集合
        /// </summary>
        public IEnumerable<ITransactionStep<TContext>> Steps { get; set; } = new List<ITransactionStep<TContext>>();

        /// <summary>
        /// 事务上下文
        /// </summary>
        public TContext Context { get; set; }

        /// <summary>
        /// 事务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 事务描述
        /// </summary>
        public string Description { get; set; }
    }
}
