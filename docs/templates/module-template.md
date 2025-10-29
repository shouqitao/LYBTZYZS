# [模块名称]模块文档

## 📋 模块概述

**模块名称**: [英文模块名]（[中文模块名]）
**版本**: v1.0
**创建日期**: YYYY-MM-DD
**架构模式**: 三层对齐 + [特定架构模式，如聚合根模式、CQRS等]

### 业务价值

[模块名称]模块是凌隐宝堂中医诊所管理系统的[核心/辅助]业务模块，负责管理[业务领域]：

- **[功能点1]**: [详细说明]
- **[功能点2]**: [详细说明]
- **[功能点3]**: [详细说明]
- **[功能点4]**: [详细说明]

### 核心功能

| 功能分类 | 功能描述 | 业务规则 |
|---------|---------|---------|
| **[功能1]** | [描述] | BR-XXX: [规则编号] |
| **[功能2]** | [描述] | AR-XXX: [规则编号] |
| **[功能3]** | [描述] | BF-XXX: [规则编号] |
| **[功能4]** | [描述] | - |
| **[功能5]** | [描述] | - |

---

## 🏗️ 架构设计

### 三层对齐架构

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  [模块]Controller.cs (WebAPI)                        │  │
│  │  - Write Layer: X个端点（创建/更新/删除）            │  │
│  │  - Read Layer: X个端点（查询/列表）                  │  │
│  │  - Helper Layer: X个端点（验证/辅助）                │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓ DTO
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  I[模块]Service / [模块]Service                      │  │
│  │  - 业务规则验证 (BR-XXX, AR-XXX, BF-XXX)            │  │
│  │  - [特定职责，如聚合根协调、事务管理等]              │  │
│  │  - 事务管理                                          │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓ Entity
┌─────────────────────────────────────────────────────────────┐
│                    Domain/Data Layer                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  [模块]Repository.cs                                 │  │
│  │  - GetByIdWithDetailsAsync (预加载)                  │  │
│  │  - GetPagedWithDetailsAsync (分页查询)               │  │
│  │  - CRUD操作                                          │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  [实体].cs (实体或聚合根)                           │  │
│  │  - [关联实体1] (关系类型)                           │  │
│  │  - [关联实体2] (关系类型)                           │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### [架构特性说明]

**[如聚合根边界、CQRS分离、领域模型等]**

[详细说明架构特点，包括：]
- 实体关系
- 关键设计原则
- 事务边界
- 一致性保证

**示例**:
```
[实体名] (聚合根/实体)
  ├── [关联实体1] (关系类型) - 描述
  │   └── 关键属性
  └── [关联实体2] (关系类型) - 描述
      └── 关键属性
```

---

## 📡 API端点列表

**完整API文档**: [`docs/api/[module-name]-api.md`](../../api/[module-name]-api.md)

### Write Layer - 写操作（X个端点）

| 端点 | 方法 | 说明 | 业务规则 |
|-----|------|------|---------|
| `/api/v1/[resource]` | POST | 创建[资源] | BR-XXX |
| `/api/v1/[resource]/{id}` | PUT | 更新[资源] | AR-XXX |
| `/api/v1/[resource]/{id}` | DELETE | 删除[资源] | BF-XXX |
| `/api/v1/[resource]/{id}/[action]` | PUT/POST | [特定操作] | - |

### Read Layer - 读操作（X个端点）

| 端点 | 方法 | 说明 | 特性 |
|-----|------|------|------|
| `/api/v1/[resource]/{id}` | GET | 获取[资源]详情 | 预加载关联数据 |
| `/api/v1/[resource]` | GET | 查询[资源]列表（分页） | 支持过滤/排序 |
| `/api/v1/[resource]/search` | GET | 搜索[资源] | 模糊匹配 |

### Helper Layer - 辅助操作（X个端点）

| 端点 | 方法 | 说明 |
|-----|------|------|
| `/api/v1/[resource]/{id}/can-[action]` | GET | 验证是否可执行[操作] |
| `/api/v1/[resource]/validate` | POST | 验证[资源]数据 |

---

## 🗄️ 数据模型

### 实体关系图

```
┌─────────────────────────────────┐
│      [主实体名] (聚合根/实体)     │
├─────────────────────────────────┤
│ Id: Guid                        │
│ [字段1]: [类型]                 │
│ [字段2]: [类型]                 │
│ [字段3]: [类型]                 │
│ Status: [枚举类型]              │
│ CreatedAt: DateTime             │
│ CreatedBy: Guid                 │
│ UpdatedAt: DateTime?            │
│ UpdatedBy: Guid?                │
│ RowVersion: byte[]              │
└─────────────────────────────────┘
           │ 1
           │
           ├──────────────┐
           │              │
       [关系] │      [关系]│
           │              │
           ↓              ↓
┌──────────────────┐  ┌────────────────────┐
│   [关联实体1]     │  │   [关联实体2]      │
├──────────────────┤  ├────────────────────┤
│ Id: Guid         │  │ Id: Guid           │
│ [主实体]Id       │  │ [主实体]Id         │
│ [字段1]: [类型]  │  │ [字段1]: [类型]    │
│ [字段2]: [类型]  │  │ [字段2]: [类型]    │
└──────────────────┘  └────────────────────┘
```

### 关键字段说明

**[主实体]实体**:
- `[字段1]`: [说明和业务含义]
- `[字段2]`: [说明和业务含义]
- `Status`: 枚举值（[状态1], [状态2], [状态3]）
- `RowVersion`: 乐观并发控制

**[关联实体1]实体**:
- `[字段1]`: [说明]
- `[字段2]`: [说明]

**[关联实体2]实体**:
- `[字段1]`: [说明]
- `[字段2]`: [说明]

---

## 📜 业务规则

### BR-XXX: [业务规则名称]

**规则描述**: [清晰描述业务规则]

**实施细节**:
- ✅ [实施要点1]
- ✅ [实施要点2]
- ❌ 禁止[不允许的操作]

**代码示例**:
```csharp
// ✅ 正确: [正确做法说明]
[代码示例]

// ❌ 错误: [错误做法说明]
[代码示例]
```

---

### AR-XXX: [架构规则名称]

**规则描述**: [清晰描述架构规则]

**实施细节**:
- ✅ [实施要点1]
- ✅ [实施要点2]
- ❌ 禁止[不允许的操作]

**验证逻辑**:
```csharp
// [Service/Repository].cs
[验证代码示例]
```

**错误场景**:
```http
[HTTP方法] [端点]
→ [状态码] [状态描述]
{
  "error": "[错误信息]（规则编号违规）"
}
```

---

### BF-XXX: [业务流程规则名称]

**规则描述**: [清晰描述业务流程规则]

**流程图**:
```
┌─────────────────┐
│ Step 1: [步骤]   │  [方法名]()
│ ([操作])         │  → 设置 [状态/标志]
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│ Step 2: [步骤]   │  [方法名]()
│ ([操作])         │  → 设置 [状态/标志]
└────────┬────────┘
         │
         ├───── [条件1] ──→ Step 3a: [操作] ([方法名])
         │
         └───── [条件2] ─→ Step 3b: [操作] ([方法名])
```

**验证条件**:
```csharp
// [方法名]() 验证逻辑
if ([条件1])
    throw new InvalidOperationException("[错误信息]");

if ([条件2])
    throw new InvalidOperationException("[错误信息]");
```

---

## 🔧 开发指南

### 如何扩展[模块名称]模块

#### 1. 添加新的业务字段

**步骤**:
1. 在`[实体].cs`实体中添加新属性
2. 创建EF Core迁移：`dotnet ef migrations add [MigrationName]`
3. 更新DTO: `[模块]Dtos.cs`
4. 更新AutoMapper映射: `[模块]MappingProfile.cs`
5. 更新Service接口和实现
6. 更新Controller端点
7. 更新API文档: `docs/api/[module-name]-api.md`

**示例**:
```csharp
// 1. 实体层
public class [实体] : BaseEntity
{
    [StringLength(200)]
    public string? [NewField] { get; set; } // 新增字段
}

// 2. DTO层
public class [Request/Response]Dto
{
    public string? [NewField] { get; set; } // 新增字段
}

// 3. Mapping
CreateMap<[Request]Dto, [实体]>()
    .ForMember(dest => dest.[NewField],
               opt => opt.MapFrom(src => src.[NewField]));

// 4. Service
public async Task<[实体]?> [Method]Async(...)
{
    // ... 映射[NewField]
}
```

---

#### 2. 添加新的业务规则验证

**步骤**:
1. 在`[模块]Rules.cs`中添加验证方法
2. 在Service层调用验证
3. 更新单元测试验证覆盖
4. 更新业务规则文档: `docs/business-rules.md`

**示例**:
```csharp
// [模块]Rules.cs
public static class [模块]Rules
{
    public static void Validate[RuleName]([参数])
    {
        if ([条件])
            throw new ValidationException("[错误信息]");
    }
}

// [模块]Service.cs
public async Task<[实体]?> [Method]Async(...)
{
    // 调用验证
    [模块]Rules.Validate[RuleName]([参数]);

    // 保存
    await _repository.UpdateAsync([实体]);
}
```

---

#### 3. 添加新的查询端点

**步骤**:
1. 在`I[模块]Service`接口添加方法签名
2. 在`[模块]Service`实现查询逻辑
3. 在`[模块]Repository`添加Repository方法（如需）
4. 在`[模块]Controller`添加API端点
5. 编写集成测试验证端点
6. 更新API文档

**示例**:
```csharp
// I[模块]Service.cs
Task<List<[实体]>> Get[FilterName]Async([参数]);

// [模块]Service.cs
public async Task<List<[实体]>> Get[FilterName]Async([参数])
{
    return await _repository
        .GetQueryable()
        .Where([条件])
        .ToListAsync();
}

// [模块]Controller.cs (Read Layer)
[HttpGet("[route]")]
public async Task<ActionResult<List<[Dto]>>> Get[FilterName]([参数])
{
    var entities = await _service.Get[FilterName]Async([参数]);
    return Ok(_mapper.Map<List<[Dto]>>(entities));
}
```

---

### 常见问题（FAQ）

#### Q1: [常见问题1]？

**A**: [详细回答]

**正确做法**: [代码示例或说明]

---

#### Q2: [常见问题2]？

**A**: [详细回答]

**处理方式**:
```csharp
[代码示例]
```

---

#### Q3: [常见问题3]？

**A**: [详细回答]

**业务价值**:
- [价值点1]
- [价值点2]
- [价值点3]

---

#### Q4: [常见问题4]？

**A**: [详细回答]

**API调用示例**:
```http
# 1. [步骤1]
[HTTP方法] [端点]
→ [状态码] [状态描述]

# 2. [步骤2]
[HTTP方法] [端点]
Content-Type: application/json
{
  "[字段]": "[值]",
  ...
}
→ [状态码]
```

**注意**: [特别注意事项]

---

#### Q5: [常见问题5]？

**A**: [详细回答]

**问题场景**:
```csharp
// ❌ 错误: [错误做法]
[代码示例]
```

**优化方案**:
```csharp
// ✅ 正确: [正确做法]
[代码示例]
```

**Repository实现**:
```csharp
[代码示例]
```

---

#### [X]. Desktop端Repository使用指南（可选）

**架构模式**: Desktop端直接使用`I[模块]Repository`，不经过Service层。

**核心组件**:
- `I[模块]Repository.cs` - Repository接口定义（Desktop/Modules/[模块]/Interfaces）
- `[模块]Repository.cs` - Repository实现（Desktop/Modules/[模块]/Repositories）
- Refit HTTP Client - 类型安全的REST API调用

**使用示例**:
```csharp
// ViewModel中注入Repository
public class [ViewModel] : BindableBase
{
    private readonly I[模块]Repository _[模块]Repository;

    public [ViewModel](I[模块]Repository [模块]Repository)
    {
        _[模块]Repository = [模块]Repository;
    }

    // 查询示例
    private async Task [Method]Async([参数])
    {
        var result = await _[模块]Repository.[Method]Async([参数]);
        // 处理结果
    }
}
```

**架构约束**:
- ✅ **统一Repository模式**: 所有ViewModel统一使用Repository，无例外
- ❌ **禁止临时Service**: 不再创建中间Service层
- ✅ **API能力对齐**: Desktop端Repository方法完全对应Server端API端点

---

## 🧪 测试覆盖

**详细测试报告**: [`docs/deep/testing-strategies.md#模块测试覆盖报告`](../../deep/testing-strategies.md#模块测试覆盖报告)

### 单元测试

- **测试文件**: `tests/UnitTests/Server/Modules/LYBT.Module.[模块].Tests/Services/[模块]ServiceTests.cs`
- **测试数量**: [X]个测试
- **行覆盖率**: [X]%
- **分支覆盖率**: [X]%
- **通过率**: [X]%

### 集成测试

- **测试文件**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/[模块]ControllerIntegrationTests.cs`
- **测试数量**: [X]个测试
- **通过率**: [X]%
- **覆盖范围**: [X]个API端点全覆盖

### E2E测试

- **场景数量**: [X]个完整业务流程
- **通过率**: [X]%
- **测试方式**: WebAPI集成测试（符合MVVM架构）

---

## 📚 相关文档

### 核心文档
- **API参考**: [`docs/api/[module-name]-api.md`](../../api/[module-name]-api.md) - [X]个API端点完整文档
- **架构指南**: [`docs/architecture/server/README.md`](../../architecture/server/README.md) - Server端三层架构
- **业务规则**: [`docs/business-rules.md`](../../business-rules.md) - 14条核心业务规则
- **测试策略**: [`docs/deep/testing-strategies.md`](../../deep/testing-strategies.md) - 测试金字塔和覆盖报告

### 快速参考
- **代码模式**: [`docs/quick-reference/code-patterns.md`](../../quick-reference/code-patterns.md) - Service/Repository模式示例
- **API速查**: [`docs/quick-reference/api-reference.md`](../../quick-reference/api-reference.md) - [模块] API速查表

### 报告文档
- **文档同步清单**: [`docs/reports/[module-name]-doc-sync-checklist.md`](../../reports/[module-name]-doc-sync-checklist.md)
- **E2E测试报告**: [`docs/reports/e2e-test-coverage-analysis.md`](../../reports/e2e-test-coverage-analysis.md)

---

## 🔄 变更历史

### v1.0 - YYYY-MM-DD

**初始版本**:
- ✅ [功能1]实现
- ✅ [功能2]实现
- ✅ [功能3]实现
- ✅ 完整API文档和架构文档

**业务规则实施**:
- ✅ BR-XXX: [规则名称]
- ✅ AR-XXX: [规则名称]
- ✅ BF-XXX: [规则名称]

**文档完善**:
- ✅ API文档: [X]行完整参考
- ✅ 架构文档: Service/Repository详述
- ✅ 测试文档: 模块测试覆盖报告
- ✅ 模块文档: 本README（开发指南 + FAQ）

---

**维护团队**: [团队名称]
**最后更新**: YYYY-MM-DD
**下次审查**: [下次审查时间或条件]
