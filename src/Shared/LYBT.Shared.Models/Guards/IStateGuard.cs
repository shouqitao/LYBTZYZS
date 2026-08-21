namespace LYBT.Shared.Models.Guards;

/// <summary>
/// 状态守卫标记接口（T1.2）
/// 统一 MedicalCase / Registration 等聚合的状态机校验入口
/// 实现位于各模块 Guards 目录（MedicalCaseStateGuard / RegistrationStateGuard）
/// </summary>
/// <typeparam name="T">聚合根实体类型</typeparam>
public interface IStateGuard<T> where T : class
{
}
