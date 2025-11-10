namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 批量导入时的重复处理策略
    /// Epic #1962 Task 2.2: 批量导入重复处理
    /// </summary>
    public enum DuplicateStrategy
    {
        /// <summary>跳过重复项（保留原有数据）</summary>
        Skip = 0,

        /// <summary>更新现有记录（覆盖原有数据）</summary>
        Update = 1,

        /// <summary>报错并回滚整个导入操作</summary>
        Error = 2
    }
}
