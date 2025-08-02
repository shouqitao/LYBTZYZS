# AutoMapper配置修复总结

## 修复日期
2025-08-02

## 修复内容

### 1. 患者模块
- **问题**：缺少列表显示的简化DTO
- **解决方案**：
  - 创建了 `PatientDto` 用于列表显示
  - 添加了 `PatientModel` → `PatientDto` 映射
  - 保留了详细DTO用于详情页面

### 2. 药材模块
- **问题**：
  - 缺少列表显示的DTO
  - 存在本地DTO和共享DTO的引用歧义
- **解决方案**：
  - 创建了 `SharedHerbDto` 用于列表显示
  - 修复了 `IHerbService` 接口中的类型引用
  - 使用别名解决了DTO歧义问题
  - 添加了必要的AutoMapper映射配置

### 3. 医生模块
- **状态**：保持现状
- **原因**：根据渐进式迁移策略，暂不修改使用本地DTO的模块

## 技术要点

### DTO命名规范
- 列表显示：`[Entity]Dto` (如 PatientDto, HerbDto)
- 详细信息：`[Entity]DetailDto`
- 创建操作：`[Entity]CreateDto`
- 更新操作：`[Entity]UpdateDto`

### AutoMapper配置模式
```csharp
// Entity到列表DTO
CreateMap<PatientModel, PatientDto>()
    .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime));

// 使用基类映射（适用于有继承关系的模型）
CreateMap<HerbModel, SharedHerbDto>()
    .IncludeBase<BaseHerbModel, SharedHerbDto>();
```

### 解决DTO歧义
```csharp
// 使用别名
using SharedHerbDto = LYBT.Shared.Models.Contracts.Herbs.HerbDto;

// 在方法签名中使用
public async Task<List<SharedHerbDto>> GetListAsync()
```

## 影响范围
- ✅ 编译错误已修复
- ✅ AutoMapper配置完整性提升
- ✅ API功能正常运行

## 后续建议
1. 其他模块可根据需要添加类似的列表DTO
2. 逐步将本地DTO迁移到共享DTO（按需进行）
3. 保持API稳定性，避免破坏性更改