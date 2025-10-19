namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 可验证接口 - 流程状态机使用（Epic #1494 - Task #1501）
    /// Step ViewModel实现此接口，提供验证当前步骤必填项的能力
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// 验证当前步骤数据
        /// </summary>
        /// <returns>验证通过返回true，失败返回false</returns>
        bool Validate();

        /// <summary>
        /// 获取验证错误消息
        /// </summary>
        string ValidationMessage { get; }
    }
}
