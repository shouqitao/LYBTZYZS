namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 可保存接口 - 流程状态机使用（Epic #1494 - Task #1501）
    /// Step ViewModel实现此接口，提供保存当前步骤数据的能力
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// 保存当前步骤数据
        /// </summary>
        /// <returns>保存成功返回true，失败返回false</returns>
        Task<bool> SaveAsync();

        /// <summary>
        /// 静默保存（不显示验证错误对话框）
        /// OpenSpec: clarify-cancel-consultation-logic - 取消前保存使用
        /// </summary>
        /// <returns>保存成功返回true，失败返回false</returns>
        Task<bool> SaveSilentlyAsync() => SaveAsync(); // 默认实现
    }
}
