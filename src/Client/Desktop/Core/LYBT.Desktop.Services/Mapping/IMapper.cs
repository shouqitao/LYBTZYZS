namespace LYBT.Desktop.Services.Mapping
{
    /// <summary>
    /// 对象映射器接口 - AutoMapper的别名接口
    /// 用于解决业务模块的依赖问题
    /// </summary>
    public interface IMapper : AutoMapper.IMapper
    {
        // 继承AutoMapper.IMapper的所有功能
        // 这个接口只是为了提供一个本地命名空间的类型
    }
}
