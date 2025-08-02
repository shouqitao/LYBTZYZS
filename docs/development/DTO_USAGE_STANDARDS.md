# DTO使用标准规范

## 创建日期
2025-08-02

## 1. 概述

本文档定义了LYBT中医诊所管理系统中数据传输对象（DTO）的使用标准，确保前后端通信的一致性和代码的可维护性。

## 2. DTO分层架构

### 2.1 共享DTO（Shared DTOs）
**位置**: `src/Shared/LYBT.Shared.Models/Contracts/[ModuleName]/`

**用途**:
- 前后端共享的契约定义
- API控制器的参数和返回值
- 跨模块通信的数据结构

**命名规范**:
- `[Entity]DetailDto` - 详细信息DTO（包含所有字段）
- `[Entity]CreateDto` - 创建操作DTO
- `[Entity]UpdateDto` - 更新操作DTO
- `[Entity]PagedQueryDto` - 分页查询DTO
- `[Entity]QueryDto` - 普通查询DTO

### 2.2 本地DTO（Local DTOs）
**位置**: `src/Backend/Core/LYBT.Models/[ModuleName]/Dtos/`

**用途**:
- 仅在后端内部使用
- 服务层之间的数据传递
- 特定业务逻辑的临时数据结构

**使用原则**:
- 新开发优先使用共享DTO
- 本地DTO仅用于后端内部特殊需求
- 逐步迁移现有本地DTO到共享DTO

## 3. 标准实施方案

### 3.1 推荐方案：渐进式迁移
考虑到系统已有大量本地DTO，建议采用渐进式迁移策略：

1. **保持现状稳定**
   - 现有功能正常的模块暂不修改
   - 避免大规模重构带来的风险

2. **新功能使用共享DTO**
   - 所有新开发的功能直接使用共享DTO
   - 新模块不再创建本地DTO

3. **按需迁移**
   - 当模块需要修改时，顺带迁移到共享DTO
   - 优先迁移前端频繁调用的接口

### 3.2 实施步骤

#### 第一阶段：标准制定（当前）
- ✅ 制定DTO使用标准
- ✅ 文档化决策和规范

#### 第二阶段：新功能实施
- 新API统一使用共享DTO
- 服务接口直接引用共享DTO
- AutoMapper配置映射共享DTO到实体

#### 第三阶段：渐进迁移（按需）
- 修改模块时评估迁移成本
- 高频使用的API优先迁移
- 保持向后兼容性

## 4. 技术实现指南

### 4.1 控制器层
```csharp
// ✅ 推荐：直接使用共享DTO
[HttpPost("add")]
public async Task<IActionResult> Add([FromBody] SharedHerbCreateDto dto) {
    var result = await _herbService.AddAsync(dto);
    return Ok(ApiResponse<bool>.Success(result));
}

// ❌ 避免：控制器中进行DTO转换
[HttpPost("add")]
public async Task<IActionResult> Add([FromBody] SharedHerbCreateDto sharedDto) {
    var localDto = MapToLocalDto(sharedDto); // 避免这种转换
    // ...
}
```

### 4.2 服务接口层
```csharp
// ✅ 推荐：服务接口使用共享DTO
public interface IHerbService {
    Task<HerbDetailDto> GetByIdAsync(Guid id);
    Task<PaginatedResult<HerbDetailDto>> GetPagedAsync(HerbPagedQueryDto query);
    Task<bool> AddAsync(HerbCreateDto dto);
}

// ⚠️ 过渡期：可以保留必要的本地DTO
public interface IHerbService {
    Task<List<HerbDto>> GetListAsync(); // 内部使用的简化DTO
}
```

### 4.3 AutoMapper配置
```csharp
public class HerbMappingProfile : Profile {
    public HerbMappingProfile() {
        // 共享DTO到实体的映射
        CreateMap<HerbCreateDto, HerbModel>();
        CreateMap<HerbUpdateDto, HerbModel>();
        CreateMap<HerbModel, HerbDetailDto>();
        
        // 如需要，保留本地DTO映射
        CreateMap<HerbModel, HerbDto>(); // 内部使用
    }
}
```

## 5. 迁移优先级

### 高优先级（建议立即迁移）
1. **用户认证模块** - 已完成，使用共享DTO
2. **患者管理模块** - 高频使用，建议迁移
3. **药材管理模块** - 核心业务，建议迁移

### 中优先级（按需迁移）
1. **医生管理模块**
2. **处方管理模块**
3. **挂号管理模块**

### 低优先级（暂时保持）
1. **系统配置模块**
2. **同步服务模块**
3. **内部工具模块**

## 6. 注意事项

### 6.1 版本兼容性
- 修改共享DTO时需考虑前端兼容性
- 使用版本控制确保平滑升级
- 提供详细的变更日志

### 6.2 性能考虑
- 共享DTO应包含必要字段，避免过度设计
- 大数据量查询考虑使用精简DTO
- 合理使用AutoMapper投影优化查询

### 6.3 安全性
- 敏感信息不应出现在共享DTO中
- 使用适当的数据验证特性
- 控制DTO字段的可见性

## 7. 最佳实践

1. **单一职责**：每个DTO专注于特定用途
2. **明确命名**：DTO名称清晰表达其用途
3. **文档完善**：为DTO字段添加XML注释
4. **验证完整**：使用数据注解进行输入验证
5. **避免循环引用**：DTO之间避免相互引用

## 8. 决策记录

**决策**：采用渐进式迁移策略，新功能使用共享DTO，现有功能按需迁移。

**理由**：
- 降低系统风险
- 保持开发效率
- 逐步提升代码质量
- 易于团队接受和实施

## 9. 后续行动

1. 将此标准纳入开发规范
2. 在代码审查中检查DTO使用
3. 定期评估迁移进度
4. 收集团队反馈并优化标准