namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 可验证接口 - 数据验证
    /// OpenSpec: simplify-workspace-architecture - Item类直接实现此接口
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// 验证数据
        /// </summary>
        /// <returns>验证通过返回true，失败返回false</returns>
        bool Validate();

        /// <summary>
        /// 验证错误消息
        /// </summary>
        string ValidationMessage { get; set; }
    }
}
