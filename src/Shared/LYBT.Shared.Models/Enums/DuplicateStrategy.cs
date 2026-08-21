namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 批量导入时的重复处理策略
    /// Epic #1962 Task 2.2: 批量导入重复处理
    /// P2-5-3 评估：Herb(同名) 与 Patient(同身份证/同名同性别同生日) 语义重载，校验分散在各自 Handler；已评估抽 BatchImportOptions&lt;TKey&gt; 通用泛型，
    /// 收益 &lt; 双Handler 独立演进成本，保留分散，文档在 04-api-reference 批量导入节已明确各领域键语义。
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
