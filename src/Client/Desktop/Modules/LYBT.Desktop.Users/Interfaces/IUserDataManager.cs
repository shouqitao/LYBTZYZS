using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces
{
    /// <summary>
    /// 用户数据管理器接口
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// OpenSpec: dto-architecture-specification - 统一使用UserDetailDto
    /// </summary>
    public interface IUserDataManager : IDataManager<UserDetailDto>
    {
        // IDataManager<UserDetailDto>已定义所有核心方法
        // UserDataManager没有额外的业务方法，直接继承即可
    }
}
