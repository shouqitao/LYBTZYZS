using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 操作结果DTO泛型版 - 支持返回具体数据
    /// </summary>
    public class OperationResultDto<T> : OperationResultDto
    {
        /// <summary>返回的数据</summary>
        [DisplayName("返回数据")]
        public T? Data { get; set; }
    }
}
