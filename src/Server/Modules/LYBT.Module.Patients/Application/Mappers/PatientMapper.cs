using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Entities.Patients;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Patients.Application.Mappers;

/// <summary>
/// 患者数据映射器。Mapperly 编译时生成（A-18 P1-4 由手写静态类改造）。
/// 纯属性复制方法（ToListDto/ToDetailDto）由 Mapperly 生成；工厂方法（ToEntity）保留手写（行为等价）。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target, AutoUserMappings = false)]
public static partial class PatientMapper
{
    /// <summary>
    /// PatientInputDto 转换为 Patient 实体（创建）。
    /// 保留手写：走领域工厂 Patient.Create（校验 + Trim），Mapperly 无法表达。
    /// 不带 [UserMapping] 标记：AutoUserMappings=false 下不被 Mapperly 发现（带额外参数签名不受支持）。
    /// </summary>
    public static Patient ToEntity(PatientInputDto dto, Guid? createdBy = null) => Patient.Create(
        dto.Name,
        dto.Gender,
        dto.BirthDate,
        dto.PhoneNumber,
        dto.IdNumber,
        dto.PinYinCode,
        createdBy);

    /// <summary>
    /// Patient 实体转换为 PatientListDto（列表查询）。Mapperly 生成（属性全同名）。
    /// </summary>
    public static partial PatientListDto ToListDto(Patient entity);

    /// <summary>
    /// Patient 实体转换为 PatientDetailDto（详情查询）。Mapperly 生成（属性全同名）。
    /// </summary>
    public static partial PatientDetailDto ToDetailDto(Patient entity);
}
