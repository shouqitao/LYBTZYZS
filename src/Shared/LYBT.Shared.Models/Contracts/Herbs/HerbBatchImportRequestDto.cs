using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 药材批量导入请求DTO
    /// Epic #1962 Task 2.3: Controller接收的批量导入请求
    /// </summary>
    public class HerbBatchImportInputDto
    {
        /// <summary>药材DTO列表（≤10000条，BR-006）</summary>
        public List<HerbInputDto> Herbs { get; set; } = new();

        /// <summary>重复处理策略（Skip/Update/Error）</summary>
        public DuplicateStrategy Strategy { get; set; } = DuplicateStrategy.Skip;
    }
}
