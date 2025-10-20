using LYBT.Desktop.MedicalCase.Models;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 本地存储服务接口（Issue #1502 - 自动保存草稿功能）
    /// </summary>
    public interface ILocalStorageService
    {
        /// <summary>
        /// 保存草稿到本地存储
        /// </summary>
        /// <param name="state">流程草稿状态</param>
        Task SaveDraftAsync(FlowDraftState state);

        /// <summary>
        /// 从本地存储加载草稿
        /// </summary>
        /// <returns>流程草稿状态，如果不存在返回null</returns>
        Task<FlowDraftState?> LoadDraftAsync();

        /// <summary>
        /// 清除本地草稿
        /// </summary>
        Task ClearDraftAsync();
    }
}
