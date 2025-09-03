using LYBT.Shared.Models.Common;
namespace LYBT.Desktop.Core.Models.Common
{
    /// <summary>
    /// 前端可审计模型 - 暂无审计属性，保留作为类型层级
    /// </summary>
    public abstract class AuditableModel : BaseModel
    {
        // 预留用于需要审计跟踪的ViewModels
    }
}