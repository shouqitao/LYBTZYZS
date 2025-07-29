namespace LYBT.Common.Enums.Queueing {

    /// <summary>
    /// 排队状态
    /// </summary>
    public enum QueueStatus {

        /// <summary>
        /// 等待
        /// </summary>
        Waiting = 0,

        /// <summary>
        /// 呼叫中
        /// </summary>
        Calling = 1,

        /// <summary>
        /// 就诊中
        /// </summary>
        InService = 2,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 已跳过
        /// </summary>
        Skipped = 4,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -1,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout = -2
    }
}