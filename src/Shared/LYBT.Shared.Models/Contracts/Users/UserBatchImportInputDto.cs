using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Users
{
    /// <summary>
    /// 用户批量导入请求DTO
    /// Issue #2003 Task 2.10: Desktop主导批量导入模式
    /// </summary>
    public class UserBatchImportInputDto
    {
        /// <summary>用户DTO列表（≤10000条）</summary>
        public List<UserInputDto> Users { get; set; } = new();

        /// <summary>重复处理策略（Skip/Update/Error）</summary>
        public DuplicateStrategy Strategy { get; set; } = DuplicateStrategy.Skip;
    }
}
