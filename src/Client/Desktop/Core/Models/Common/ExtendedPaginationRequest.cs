namespace LYBT.Desktop.Core.Models.Common
{
    /// <summary>
    /// 扩展分页请求别名 - UltraThink重构：统一到PagedQueryBaseDto
    /// Extensions字典已内置在基类中
    /// </summary>
    public class ExtendedPaginationRequest : PaginationRequest
    {
        /// <summary>
        /// 扩展数据访问器（兼容性）
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> ExtensionData 
        { 
            get => Extensions; 
            set => Extensions = value; 
        }
    }
}