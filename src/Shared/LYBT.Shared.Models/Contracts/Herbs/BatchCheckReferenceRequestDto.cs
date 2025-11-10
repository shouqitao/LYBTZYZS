namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 批量检查引用请求DTO
    /// Epic #1962 Task 4.3: Controller接收的批量检查引用请求
    /// </summary>
    public class BatchCheckReferenceRequestDto
    {
        /// <summary>药材ID列表（≤100条，BR-006）</summary>
        public List<Guid> HerbIds { get; set; } = new();
    }
}
