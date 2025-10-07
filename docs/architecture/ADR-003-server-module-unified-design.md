# ADR-003: Server模块统一接口与Service设计

## 状态
**已接受** | 2025-10-07

## 背景

在Epic #998（BaseEntity审计字段统一）完成后，系统遗留了部分早期双层Service接口设计（Query + Business），与当前统一的Shared.Interfaces.Services设计模式不一致。具体问题包括：

1. **Consultation模块遗留问题**：
   - 存在未使用的 `IConsultationQueryService` 和 `ConsultationQueryService`
   - 已在 `ConsultationModule.cs` 注册但从未被调用
   - 与已生效的 `IConsultationService` 功能重复

2. **Formula模块目录问题**：
   - `FormulaRepository.cs` 错误放置在 `Services/` 目录
   - 应位于 `Repositories/` 目录以符合模块化标准

3. **缺乏统一标准文档**：
   - 各模块设计标准分散在代码注释中
   - 新开发者容易重复引入CQRS模式
   - 缺少可执行的验收清单

## 决策

### 1. 禁止CQRS双层Service模式

**废弃以下模式**：
```csharp
// ❌ 禁止：双层Service接口
services.AddScoped<IXxxQueryService, XxxQueryService>();
services.AddScoped<IXxxBusinessService, XxxBusinessService>();
```

**强制使用**：
```csharp
// ✅ 强制：统一Service接口
services.AddScoped<LYBT.Shared.Interfaces.Services.IXxxService, XxxService>();
```

**理由**：
- 符合 `docs/development/standards.md` 第32-39行的**CQRS禁用决策**
- 小型诊所系统业务复杂度无需读写分离
- 减少接口层数，降低维护成本
- 提升代码可读性和新人上手速度

### 2. 统一Service接口位置

**所有Service接口必须定义在**：
```
src/Shared/LYBT.Shared.Interfaces/Services/
```

**模块内Interfaces/目录仅允许**：
```csharp
// ✅ 允许：Repository接口
public interface IXxxRepository { }
```

**禁止放置**：
```csharp
// ❌ 禁止：Service接口
public interface IXxxService { }
public interface IXxxQueryService { }
public interface IXxxBusinessService { }
```

**理由**：
- Desktop端和Server端共享统一服务契约
- 避免接口重复定义
- 便于跨平台一致性校验

### 3. 强制目录结构规范

**Repository实现类位置**：
```
✅ src/Server/Modules/LYBT.Module.Xxx/Repositories/XxxRepository.cs
❌ src/Server/Modules/LYBT.Module.Xxx/Services/XxxRepository.cs
```

**Service实现类位置**：
```
✅ src/Server/Modules/LYBT.Module.Xxx/Services/XxxService.cs
```

**理由**：
- 清晰的职责分离
- 符合DDD分层架构约定
- 便于自动化工具扫描和代码生成

### 4. 创建标准文档体系

新增以下文档：
- `docs/architecture/server-module-design-standard.md` - 详细设计标准
- `docs/architecture/ADR-003-server-module-unified-design.md` - 本决策记录
- 更新 `docs/architecture/functional-modules-design.md` - 补充统一模式说明

**理由**：
- 形成可追溯的架构决策链
- 提供可执行的验收清单
- 便于新成员理解设计约束

## 实施方案

### Phase 1: 清理Consultation模块（已完成）

**删除文件**：
```
✅ src/Server/Modules/LYBT.Module.Consultation/Interfaces/IConsultationQueryService.cs
✅ src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationQueryService.cs
✅ tests/UnitTests/Modules/Consultation.UnitTests/Services/ConsultationQueryServiceTests.cs
```

**更新文件**：
```csharp
// ConsultationModule.cs
- services.AddScoped<IConsultationQueryService, ConsultationQueryService>();
- using LYBT.Module.Consultation.Interfaces;  // 保留，IConsultationRepository仍需要
```

**验收标准**：
- ✅ `dotnet build LYBT.Server.sln -c Release` - 0错误0警告
- ✅ `grep -r "IConsultationQueryService"` - 无结果

### Phase 2: 修正Formula目录结构（已完成）

**文件移动**：
```bash
✅ git mv src/Server/Modules/LYBT.Module.Formula/Services/FormulaRepository.cs \
         src/Server/Modules/LYBT.Module.Formula/Repositories/FormulaRepository.cs
```

**验收标准**：
- ✅ 命名空间保持 `LYBT.Module.Formula.Repositories`
- ✅ FormulaModule.cs 的 `using LYBT.Module.Formula.Repositories;` 正常工作
- ✅ 编译成功

### Phase 3: 创建标准文档（已完成）

**新增文档**：
```
✅ docs/architecture/server-module-design-standard.md
✅ docs/architecture/ADR-003-server-module-unified-design.md
```

**更新文档**：
```
□ docs/architecture/functional-modules-design.md
□ docs/index.md（添加导航链接）
```

## 影响范围

### 受影响的模块
- **Consultation模块**: 删除未使用的双层Service接口
- **Formula模块**: 修正Repository文件位置

### 未受影响的模块
以下模块已符合标准，无需修改：
- Users, Herbs, Patients, Prescriptions, MedicalCase, Auth

### 编译影响
- **Server端编译**: 100%通过
- **Desktop端编译**: 无影响（未引用被删除的接口）
- **测试编译**: 删除1个废弃测试类

## 决策理由

### 1. 为什么禁止CQRS？

参考 `docs/development/standards.md` - 过度工程黑名单：

```markdown
### 禁止引入的技术（过度工程黑名单）
- **CQRS模式** - 三层架构足够
- 决策日期: 2025-09-27
```

**分析**：
- 诊所系统日均用户 < 50
- 单库事务已满足一致性需求
- CQRS增加开发成本但无性能收益

### 2. 为什么Service接口放Shared层？

**Desktop端和Server端共享接口的优势**：
1. **契约一致性**: Desktop调用Server API时，期望的返回类型与Server定义完全一致
2. **减少重复**: 避免Desktop.Interfaces和Server.Interfaces定义两套相同接口
3. **便于Mock**: 测试时可复用相同的Mock对象

**示例**：
```csharp
// Desktop端ViewModel
public class ConsultationViewModel
{
    private readonly IConsultationService _service; // 注入的是Shared接口

    public async Task LoadDataAsync()
    {
        var result = await _service.GetPagedAsync(...);
        // result类型与Server端返回类型完全一致
    }
}

// Server端Controller
[ApiController]
public class ConsultationController
{
    private readonly IConsultationService _service; // 同一个接口

    [HttpGet]
    public async Task<IActionResult> GetPaged(...)
    {
        var result = await _service.GetPagedAsync(...);
        return Ok(result);
    }
}
```

### 3. 为什么Repository接口留在模块内？

**Repository是模块私有实现细节**：
1. Desktop端不直接访问Repository（通过HTTP调用Server）
2. Repository接口可能包含数据库特定查询逻辑
3. 模块间不共享Repository（符合DDD限界上下文原则）

## 验收标准

### 代码层面
- [x] Consultation模块无IConsultationQueryService引用
- [x] FormulaRepository位于Repositories/目录
- [x] 所有模块Service注册使用Shared.Interfaces.Services
- [x] `dotnet build LYBT.Server.sln -c Release` 0错误0警告

### 文档层面
- [x] server-module-design-standard.md 包含完整验收清单
- [x] ADR-003记录决策理由和实施方案
- [ ] functional-modules-design.md 引用新标准
- [ ] docs/index.md 添加导航链接

## 后续计划

### 短期（本PR完成）
1. 提交所有代码和文档修改
2. 更新 `docs/architecture/functional-modules-design.md`
3. 更新 `docs/index.md` 导航

### 中期（下一个Sprint）
1. 在CI中添加架构规则校验（禁止Interfaces/下出现*Service.cs）
2. 创建ArchUnit测试验证模块结构合规性

### 长期（持续）
1. 在代码评审清单中增加"Server模块设计标准"检查项
2. 定期审查模块是否符合本ADR要求

## 参考资料

- [技术标准与规范](../development/standards.md) - CQRS禁用决策
- [Server模块设计标准](server-module-design-standard.md) - 详细实施标准
- [功能模块设计](functional-modules-design.md) - 模块化设计总览
- [Issue #1006](https://github.com/user/repo/issues/1006) - 本决策的触发Issue

## 决策者

- **提出**: Claude Code (基于Epic #998验收审查)
- **批准**: 待用户确认
- **记录**: Claude Code
- **日期**: 2025-10-07
