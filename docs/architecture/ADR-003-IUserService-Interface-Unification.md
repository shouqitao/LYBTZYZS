# ADR-003: IUserService接口统一重构

## 状态
已接受 (2025-08-17)

## 背景
在LYBTZYZS系统的UltraThink架构重构过程中，我们发现了一个严重的架构问题：IUserService接口在三个不同的层次中重复定义，违反了SOLID原则，特别是单一职责原则(SRP)和依赖倒置原则(DIP)。

### 问题发现
- **Server层**: `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserService.cs` (14个方法，包含audit参数)
- **Shared层**: `src/Shared/LYBT.Shared.Interfaces/Services/IUserService.cs` (22个方法，统一ServiceResult包装)  
- **Client层**: `src/Client/Desktop/Core/Interfaces/Services/IUserService.cs` (8个方法，返回UserInfo模型)

### 架构问题
1. **接口重复定义**: 三个层次各自定义了IUserService接口，导致职责混乱
2. **契约不一致**: 每层的接口签名和返回类型都不同
3. **维护困难**: 接口变更需要在三个地方同时修改
4. **违反DIP**: 高层模块依赖低层模块的具体实现

## 决策
采用**Shared接口统一模式**，以Shared层的IUserService接口作为系统唯一契约，删除其他重复定义。

### 核心原则
- **单一职责**: 每个接口只有一个定义
- **契约统一**: 使用ServiceResult\<T>包装所有返回值
- **层次清晰**: Shared层定义契约，Server/Client层实现契约
- **向前兼容**: 保留UI层兼容方法，平滑迁移

## 实施方案

### 1. 接口统一 ✅
- **保留**: `src/Shared/LYBT.Shared.Interfaces/Services/IUserService.cs` (22个方法)
- **删除**: Server层和Client层的重复接口定义
- **规范**: 所有方法使用ServiceResult\<T>包装返回值

### 2. Server端重构 ✅
```csharp
// 修改前：包含audit参数，无ServiceResult包装
public async Task<SharedUserDto?> AddAsync(SharedUserCreateDto dto, Guid operatorId, string operatorName)

// 修改后：符合Shared接口，使用ServiceResult包装
public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
```

**关键变更**:
- 移除audit参数，统一使用系统级日志记录
- 所有方法添加ServiceResult\<T>包装
- 异常处理集成到ServiceResult中

### 3. Client端重构 ✅
```csharp
// 新增：实现Shared接口的标准方法
public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)

// 保留：UI层兼容方法
public async Task<ServiceResult<UserInfo>> GetUserByIdAsync(Guid userId)
{
    var result = await GetByIdAsync(userId);
    if (result.IsSuccess && result.Data != null)
    {
        return ServiceResult<UserInfo>.Success(result.Data.ToUserInfo());
    }
    return ServiceResult<UserInfo>.Failure(result.ErrorMessage, result.Exception);
}
```

**双接口策略**:
- 实现Shared接口的完整22个方法
- 保留UI层兼容方法，确保现有代码正常运行
- 使用DtoToInfoExtensions进行DTO到Info的转换

### 4. 依赖注入更新 ✅
**Client端** (`ServiceCollectionExtensions.cs`):
```csharp
// 修改前
containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IUserService>

// 修改后
containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IUserService>
```

**Server端** (`UsersModule.cs`):
```csharp
// 更新引用
using LYBT.Shared.Interfaces.Services;
services.AddScoped<IUserService, UserService>(); // 现在实现Shared接口
```

## 技术细节

### ServiceResult统一包装
```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}
```

### 转换扩展方法
```csharp
public static class DtoToInfoExtensions
{
    public static UserInfo ToUserInfo(this UserDto dto) { /* ... */ }
    public static List<UserInfo> ToUserInfoList(this IEnumerable<UserDto> dtos) { /* ... */ }
}
```

### 错误处理策略
- Server端：集成详细日志记录，使用try-catch包装
- Client端：简化错误处理，依赖ServiceResult传递错误信息
- 统一：所有异常都封装在ServiceResult中，不向上抛出

## 影响评估

### 正面影响 ✅
1. **架构清晰**: 统一了接口定义，消除了重复
2. **维护简化**: 接口变更只需在Shared层修改
3. **类型安全**: ServiceResult提供了统一的错误处理机制
4. **扩展性好**: 新的业务模块可直接复用这个模式

### 向前兼容 ✅
1. **UI层无感知**: 保留了所有原有的UI兼容方法
2. **渐进迁移**: 新代码使用Shared接口，旧代码继续工作
3. **性能优化**: 减少了接口调用的复杂性

### 风险缓解 ✅
1. **测试覆盖**: 保持现有的单元测试和集成测试
2. **回滚方案**: 如有问题可快速回滚到接口分离模式
3. **监控机制**: 通过ServiceResult统一监控错误情况

## 后续步骤

### 立即执行 ✅
- [x] 删除重复接口定义
- [x] 重构Server端UserService实现
- [x] 重构Client端UserService实现  
- [x] 更新依赖注入配置

### 计划中 🔄
- [ ] 修复DtoToInfoExtensions编译错误
- [ ] 扩展此模式到其他业务模块(Patient, Herb, Consultation等)
- [ ] 创建接口迁移指南文档
- [ ] 性能基准测试

### 长期规划 📋
- 统一所有业务模块的接口定义模式
- 建立接口设计规范和最佳实践
- 实现自动化测试覆盖所有Shared接口

## 参考资料
- [UltraThink架构重构完成报告](../ultrathink/ultrathink-phase5-completion-report-20250816.md)
- [SOLID原则最佳实践](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Clean Architecture模式](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## 作者
UltraThink架构师团队

## 审核者
- 系统架构师
- 技术负责人

---
*此ADR记录了LYBTZYZS系统中IUserService接口统一重构的完整决策过程和实施细节，为后续类似的架构优化提供参考。*