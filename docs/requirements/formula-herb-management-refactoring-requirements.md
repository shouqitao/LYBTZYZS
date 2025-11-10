# 验方药材管理重构需求文档

**文档编号**: REQ-Formula-Herb-Management-001
**创建日期**: 2025-11-10
**优先级**: P0（核心功能缺失）
**预估工作量**: 2-3小时
**关联决策**: Formula-Design-Decision-002

---

## 📋 目录

1. [执行摘要](#执行摘要)
2. [现状分析](#现状分析)
3. [架构决策](#架构决策)
4. [详细需求](#详细需求)
5. [实施计划](#实施计划)
6. [验证清单](#验证清单)
7. [风险与依赖](#风险与依赖)

---

## 执行摘要

### 问题概述

通过全面代码检查发现，验方药材管理功能**核心实现缺失**：

1. **Entity层缺失字段**: `Indication`（主治）字段不存在
2. **DTO层字段不完整**: `FormulaHerbItemInputDto`缺少必要字段
3. **Service层逻辑缺失**: `UpdateAsync`完全不处理药材列表
4. **AutoMapper配置错误**: `Herbs`字段被Ignore，导致药材永远不会被更新

### 影响范围

- ❌ **创建验方**: 药材可以保存（通过其他逻辑）
- ❌ **编辑验方**: 药材无法更新（UpdateAsync忽略Herbs）
- ❌ **主治字段**: 完全无法保存和显示

### 解决方案

采用**粗粒度（全量替换）**方案，通过`UpdateAsync`一次性更新完整的药材列表。

---

## 现状分析

### 1. Entity层检查结果

#### ✅ FormulaModel（部分完整）

**文件路径**: `src/Server/Core/LYBT.Entities/Formula/FormulaModel.cs`

```csharp
public class Formula : BaseEntity
{
    public string Name { get; set; } = string.Empty;         // ✅ 存在
    public string? Effect { get; set; }                      // ✅ 存在（功用）
    public string? Usage { get; set; }                       // ✅ 存在
    public string? Remark { get; set; }                      // ✅ 存在
    public string? Property { get; set; }                    // ✅ 存在（性味归经）
    public CommonStatus Status { get; set; }                 // ✅ 存在
    public bool IsShared { get; set; }                       // ✅ 存在
    public FormulaValidationStatus ValidationStatus { get; set; } // ✅ 存在
    public string? Category { get; set; }                    // ✅ 存在
    public FormulaType FormulaType { get; set; }             // ✅ 存在
    public Guid? UserId { get; set; }                        // ✅ 存在
    public List<FormulaHerbItem> Herbs { get; set; } = new(); // ✅ 存在

    // ❌ 缺失字段
    // public string? Indication { get; set; }  // 主治（关键字段！）
}
```

**问题**: 缺少`Indication`（主治）字段，这是验方三大核心要素之一（名称、功用、主治）。

#### ✅ FormulaHerbItem（完整）

**文件路径**: `src/Server/Core/LYBT.Entities/Formula/FormulaHerbItem.cs`

```csharp
public class FormulaHerbItem
{
    public Guid Id { get; set; }                      // ✅ 主键
    public Guid FormulaId { get; set; }               // ✅ 外键
    public Guid? HerbId { get; set; }                 // ✅ 药材ID（可空，支持延迟绑定）
    public string? OriginalHerbName { get; set; }     // ✅ 原始名称
    public bool IsValidated { get; set; }             // ✅ 验证状态
    public string HerbName { get; set; }              // ✅ 药材名称
    public int Quantity { get; set; }                 // ✅ 剂量
    public string Unit { get; set; } = "g";           // ✅ 单位
    public string? Usage { get; set; }                // ✅ 用法
    public string? Remark { get; set; }               // ✅ 备注
    public string? ProcessingMethod { get; set; }     // ✅ 炮制方法
}
```

**结论**: FormulaHerbItem字段完整，无需修改。

---

### 2. DTO层检查结果

#### ⚠️ FormulaDto（字段名不一致）

**文件路径**: `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaDtos.cs`

```csharp
public class FormulaDto : StatusDto, IRemarkable
{
    public string Name { get; set; }          // ✅ 对应Entity.Name
    public string? Effect { get; set; }       // ✅ 对应Entity.Effect
    public string? Indications { get; set; }  // ⚠️ 注意！DTO是Indications，Entity应为Indication
    public string? Usage { get; set; }        // ✅ 对应Entity.Usage
    public string? Property { get; set; }     // ✅ 对应Entity.Property
    public List<FormulaHerbItemDto> Herbs { get; set; } // ✅ 对应Entity.Herbs
}
```

**问题**: DTO使用`Indications`（复数），但根据业务需求应该是单个字段`Indication`。

#### ❌ FormulaInputDto（缺少关键字段映射）

```csharp
public class FormulaInputDto : IRemarkable
{
    public string Name { get; set; }
    public string Effect { get; set; }
    public string? Indications { get; set; }  // ⚠️ 字段名不一致
    public List<FormulaHerbItemInputDto> Herbs { get; set; }  // ✅ 存在，但子DTO有问题
}
```

#### ❌ FormulaHerbItemInputDto（严重缺失）

```csharp
public class FormulaHerbItemInputDto
{
    public Guid? Id { get; set; }
    public Guid? HerbId { get; set; }         // ✅ 存在
    public decimal Quantity { get; set; }     // ✅ 存在
    public string? Preparation { get; set; }  // ✅ 存在
    public string? Usage { get; set; }        // ✅ 存在
    public int SortOrder { get; set; }        // ✅ 存在

    // ❌ 缺失字段（关键！）
    // public string HerbName { get; set; }        // 药材名称 - 必需！
    // public string Unit { get; set; }            // 单位 - 必需！
    // public string? ProcessingMethod { get; set; } // 炮制方法 - 业务需要！
}
```

**问题**:
1. 缺少`HerbName`（药材名称） - 用户在Excel表格中输入的核心字段
2. 缺少`Unit`（单位） - 默认"g"，但可能是"钱"、"两"等
3. 缺少`ProcessingMethod`（炮制方法） - 业务需求：生、炙、炒等

---

### 3. Service层检查结果

#### ❌ FormulaService.UpdateAsync（核心逻辑缺失）

**文件路径**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`

```csharp
public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaInputDto dto)
{
    try
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<FormulaDto>.Failure("验方不存在");

        _mapper.Map(dto, entity);  // ❌ 问题：AutoMapper配置中Herbs被Ignore！
        var result = await _repository.UpdateAsync(entity);
        var resultDto = _mapper.Map<FormulaDto>(result);
        return ServiceResult<FormulaDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新验方失败");
        return ServiceResult<FormulaDto>.Failure("更新验方失败");
    }
}
```

**问题**:
- 只调用AutoMapper映射
- 没有手动处理`Herbs`集合
- 导致药材列表永远不会被更新

---

### 4. AutoMapper配置检查结果

#### ❌ FormulaMappingProfile（关键字段被忽略）

**文件路径**: `src/Server/Modules/LYBT.Module.Formula/Mapping/FormulaMappingProfile.cs`

```csharp
CreateMap<FormulaInputDto, LYBT.Entities.Formula.Formula>()
    .ForMember(dest => dest.Status, opt => opt.Ignore())
    .ForMember(dest => dest.Property, opt => opt.Ignore())
    .ForMember(dest => dest.Herbs, opt => opt.Ignore())  // ❌ 问题！Herbs被忽略
    .ForSourceMember(src => src.Instructions, opt => opt.DoNotValidate())
    .ForSourceMember(src => src.Indications, opt => opt.DoNotValidate())  // ❌ 不映射
    .ForSourceMember(src => src.Contraindications, opt => opt.DoNotValidate())
    .ForSourceMember(src => src.Preparation, opt => opt.DoNotValidate());
```

**问题**:
1. `Herbs`被`Ignore` - 导致UpdateAsync完全不更新药材
2. `Indications`被`DoNotValidate` - 即使Entity有字段也不会映射

---

### 5. Controller层检查结果

#### ✅ FormulasController（API完整）

**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`

**现有端点**:
- `GET /api/v1/formulas` - ✅ 分页查询
- `GET /api/v1/formulas/{id}` - ✅ 详情查询
- `POST /api/v1/formulas` - ✅ 新增
- `PUT /api/v1/formulas/{id}` - ✅ 更新（但Service层逻辑缺失）
- `DELETE /api/v1/formulas/{id}` - ✅ 删除
- 其他批量操作端点 - ✅ 完整

**结论**: Controller层无需修改，问题在Service层。

---

## 架构决策

### 决策编号: Formula-Design-Decision-002

**决策日期**: 2025-11-10
**决策方式**: 基于业务场景分析 + 业界实践 + MVP原则

### 业务场景确认（通过Q&A）

| 场景 | 结论 | 影响决策 |
|-----|------|---------|
| **药材独立性** | 药材不会被单独查询或操作 | 支持粗粒度方案 |
| **编辑流程** | Excel表格式，一次性批量保存 | 支持粗粒度方案 |
| **离线编辑** | 纯在线操作，掉线即作废 | 无需Delta更新 |
| **并发编辑** | 单人编辑，创建者独占编辑权 | 无需冲突处理 |

### 最终决策: 粗粒度（全量替换）

**理由**:
1. ✅ **业务匹配**: 用户操作"验方"而非"药材"
2. ✅ **MVP原则**: 够用即好，简单直接
3. ✅ **业界实践**: DDD聚合根整体更新模式
4. ✅ **性能可接受**: 5-15个药材，约1.5KB传输，<10ms数据库操作

**API设计**:
```http
PUT /api/v1/formulas/{id}
Body: { name, effect, indication, herbs: [...] }
```

**Service层实现**:
```csharp
formula.Herbs.Clear();  // 清空旧数据
formula.Herbs.AddRange(newHerbs);  // 添加新数据
await _context.SaveChangesAsync();  // EF Core批量操作
```

---

## 详细需求

### 需求1: 新增Entity字段

**优先级**: P0
**工作量**: 10分钟

#### 1.1 新增Indication字段

**文件**: `src/Server/Core/LYBT.Entities/Formula/FormulaModel.cs`

**修改内容**:
```csharp
public class Formula : BaseEntity
{
    // ... 现有字段 ...

    /// <summary>功用</summary>
    [StringLength(500)]
    [DisplayName("功用")]
    public string? Effect { get; set; }

    // 🆕 新增字段
    /// <summary>主治</summary>
    [StringLength(1000)]
    [DisplayName("主治")]
    public string? Indication { get; set; }

    // ... 其他字段 ...
}
```

**理由**:
- 主治是验方三大核心要素之一（名称、功用、主治）
- 用户需求明确：例如"阳明气分热盛。症见壮热面赤，烦渴饮引，大汗恶热，脉洪大有力或滑数"
- 长度1000字符足够容纳详细的主治描述

---

### 需求2: 修正DTO层

**优先级**: P0
**工作量**: 20分钟

#### 2.1 统一字段名称

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaDtos.cs`

**FormulaDto修改**:
```csharp
public class FormulaDto : StatusDto, IRemarkable
{
    // ... 现有字段 ...

    // 修改前
    // [DisplayName("主治")]
    // public string? Indications { get; set; }

    // 修改后（单数形式）
    [DisplayName("主治")]
    public string? Indication { get; set; }

    // ... 其他字段 ...
}
```

**FormulaInputDto修改**:
```csharp
public class FormulaInputDto : IRemarkable
{
    // ... 现有字段 ...

    // 修改前
    // [StringLength(500)]
    // public string? Indications { get; set; }

    // 修改后（单数形式，长度1000）
    [StringLength(1000, ErrorMessage = "主治描述不能超过1000个字符")]
    [DisplayName("主治")]
    public string? Indication { get; set; }

    // ... 其他字段 ...
}
```

#### 2.2 完善FormulaHerbItemInputDto

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaDtos.cs`

**修改内容**:
```csharp
public class FormulaHerbItemInputDto
{
    /// <summary>项ID（更新时可填，创建时为null）</summary>
    public Guid? Id { get; set; }

    /// <summary>药材ID（可空，支持延迟绑定）</summary>
    public Guid? HerbId { get; set; }

    // 🆕 新增字段1：药材名称（必需）
    [Required(ErrorMessage = "药材名称不能为空")]
    [StringLength(100, ErrorMessage = "药材名称不能超过100个字符")]
    [DisplayName("药材名称")]
    public string HerbName { get; set; } = string.Empty;

    [Required(ErrorMessage = "剂量必须大于0")]
    [Range(0.1, 1000, ErrorMessage = "剂量必须在0.1-1000之间")]
    [DisplayName("剂量")]
    public decimal Quantity { get; set; }

    // 🆕 新增字段2：单位（必需）
    [Required(ErrorMessage = "单位不能为空")]
    [StringLength(10, ErrorMessage = "单位不能超过10个字符")]
    [DisplayName("单位")]
    public string Unit { get; set; } = "g";

    [StringLength(50)]
    [DisplayName("炮制方法")]
    public string? Preparation { get; set; }

    // 🆕 新增字段3：炮制方法别名（与Preparation同义）
    [StringLength(100)]
    [DisplayName("加工方法")]
    public string? ProcessingMethod { get; set; }

    [StringLength(100)]
    [DisplayName("用法")]
    public string? Usage { get; set; }

    [DisplayName("排序")]
    public int SortOrder { get; set; } = 0;
}
```

**新增字段理由**:
1. `HerbName`: 用户在Excel表格中输入的核心字段（支持中文名/拼音码自动匹配）
2. `Unit`: 剂量单位，默认"g"，但可能是"钱"、"两"、"ml"等
3. `ProcessingMethod`: 炮制方法，业务需求：生石膏、炙甘草、炒知母等

---

### 需求3: 重构Service层UpdateAsync

**优先级**: P0
**工作量**: 30分钟

#### 3.1 完整实现UpdateAsync

**文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`

**修改内容**:
```csharp
public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaInputDto dto)
{
    try
    {
        // 1. 查询验方（需包含Herbs，使用GetByIdWithHerbsAsync）
        var entity = await _repository.GetByIdWithHerbsAsync(id);
        if (entity == null)
            return ServiceResult<FormulaDto>.Failure("验方不存在");

        // 2. 更新基本信息（使用AutoMapper）
        _mapper.Map(dto, entity);

        // 3. 🆕 手动处理Herbs集合（全量替换）
        entity.Herbs.Clear();  // 清空现有药材

        // 4. 🆕 添加新药材列表
        foreach (var herbDto in dto.Herbs)
        {
            entity.Herbs.Add(new FormulaHerbItem
            {
                Id = Guid.NewGuid(),
                HerbId = herbDto.HerbId,  // 若已匹配到药材库
                HerbName = herbDto.HerbName,
                Quantity = (int)herbDto.Quantity,  // decimal转int
                Unit = herbDto.Unit ?? "g",
                ProcessingMethod = herbDto.ProcessingMethod ?? herbDto.Preparation,
                Usage = herbDto.Usage,
                IsValidated = herbDto.HerbId.HasValue,  // HerbId存在即为已验证
                OriginalHerbName = herbDto.HerbId.HasValue ? null : herbDto.HerbName
            });
        }

        // 5. 保存更新（EF Core自动处理事务）
        var result = await _repository.UpdateAsync(entity);
        var resultDto = _mapper.Map<FormulaDto>(result);
        return ServiceResult<FormulaDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新验方失败，验方ID：{FormulaId}", id);
        return ServiceResult<FormulaDto>.Failure($"更新验方失败：{ex.Message}");
    }
}
```

**关键点**:
1. 使用`GetByIdWithHerbsAsync`确保加载Herbs集合（EF Core延迟加载问题）
2. `Herbs.Clear()`标记现有药材为删除
3. `Herbs.Add()`标记新药材为添加
4. EF Core在`SaveChangesAsync`时自动生成批量SQL

**EF Core生成的SQL示例**:
```sql
BEGIN TRANSACTION;

-- 删除旧药材
DELETE FROM FormulaHerbItems WHERE FormulaId = @formulaId;

-- 批量插入新药材
INSERT INTO FormulaHerbItems (Id, FormulaId, HerbName, Quantity, Unit, ProcessingMethod, ...)
VALUES
  (@id1, @formulaId, '石膏', 30, 'g', '生', ...),
  (@id2, @formulaId, '知母', 9, 'g', '肥', ...),
  (@id3, @formulaId, '粳米', 6, 'g', NULL, ...),
  (@id4, @formulaId, '甘草', 3, 'g', '炙', ...);

COMMIT TRANSACTION;
```

---

### 需求4: 修正AutoMapper配置

**优先级**: P0
**工作量**: 15分钟

#### 4.1 移除Herbs的Ignore配置

**文件**: `src/Server/Modules/LYBT.Module.Formula/Mapping/FormulaMappingProfile.cs`

**修改内容**:
```csharp
public FormulaMappingProfile()
{
    // Formula -> FormulaDto
    CreateMap<LYBT.Entities.Formula.Formula, FormulaDto>()
        .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
        .ForMember(dest => dest.Indication, opt => opt.MapFrom(src => src.Indication)); // 🆕 映射主治字段

    // FormulaHerbItem -> FormulaHerbItemDto
    CreateMap<LYBT.Entities.Formula.FormulaHerbItem, FormulaHerbItemDto>();

    // FormulaInputDto -> Formula
    CreateMap<FormulaInputDto, LYBT.Entities.Formula.Formula>()
        .ForMember(dest => dest.Status, opt => opt.Ignore())
        .ForMember(dest => dest.Property, opt => opt.Ignore())
        // 🔧 修改前：.ForMember(dest => dest.Herbs, opt => opt.Ignore())
        // 🔧 修改后：移除Ignore（但在UpdateAsync中手动处理，不依赖AutoMapper）
        .ForMember(dest => dest.Herbs, opt => opt.Ignore())  // 保留Ignore，因为手动处理
        .ForMember(dest => dest.Indication, opt => opt.MapFrom(src => src.Indication))  // 🆕 映射主治
        .ForSourceMember(src => src.Instructions, opt => opt.DoNotValidate())
        .ForSourceMember(src => src.Contraindications, opt => opt.DoNotValidate())
        .ForSourceMember(src => src.Preparation, opt => opt.DoNotValidate())
        // BaseEntity 审计字段
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
        .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
        .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
        .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
        .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
        .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
}
```

**说明**:
- `Herbs`保留Ignore，因为在UpdateAsync中手动处理（更清晰）
- 新增`Indication`字段映射

---

### 需求5: 数据库迁移

**优先级**: P0
**工作量**: 10分钟

#### 5.1 创建EF Core迁移

**命令**:
```bash
cd src/Server/Services/LYBT.WebAPI
dotnet ef migrations add AddIndicationToFormula --project ../../Infrastructure/LYBT.Infrastructure
```

**生成的迁移文件**:
```csharp
public partial class AddIndicationToFormula : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Indication",
            table: "Formulas",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Indication",
            table: "Formulas");
    }
}
```

#### 5.2 应用迁移

**命令**:
```bash
dotnet ef database update --project ../../Infrastructure/LYBT.Infrastructure
```

---

## 实施计划

### Phase 1: 准备阶段（15分钟）

| 任务 | 负责人 | 预计时间 |
|-----|-------|---------|
| 创建GitHub Issue | - | 5分钟 |
| 创建功能分支 | - | 2分钟 |
| 备份当前代码 | - | 3分钟 |
| 检查测试环境 | - | 5分钟 |

**命令**:
```bash
git checkout -b feature/formula-herb-management-refactoring
git push -u origin feature/formula-herb-management-refactoring
```

---

### Phase 2: Entity层修改（10分钟）

**步骤**:
1. 打开`FormulaModel.cs`
2. 在`Effect`字段后添加`Indication`字段
3. 编译验证：`dotnet build`

**验证**:
```bash
dotnet build src/Server/Core/LYBT.Entities/LYBT.Entities.csproj
```

---

### Phase 3: DTO层修改（20分钟）

**步骤**:
1. 打开`FormulaDtos.cs`
2. 修改`FormulaDto.Indications` → `Indication`
3. 修改`FormulaInputDto.Indications` → `Indication`
4. 完善`FormulaHerbItemInputDto`（添加HerbName、Unit、ProcessingMethod）
5. 编译验证

**验证**:
```bash
dotnet build src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj
```

---

### Phase 4: Service层重构（30分钟）

**步骤**:
1. 打开`FormulaService.cs`
2. 重构`UpdateAsync`方法（参考需求3.1）
3. 添加注释说明逻辑
4. 编译验证

**验证**:
```bash
dotnet build src/Server/Modules/LYBT.Module.Formula/LYBT.Module.Formula.csproj
```

---

### Phase 5: AutoMapper配置（15分钟）

**步骤**:
1. 打开`FormulaMappingProfile.cs`
2. 添加`Indication`字段映射
3. 确认`Herbs`保留Ignore（因为手动处理）
4. 编译验证

**验证**:
```bash
dotnet build src/Server/Modules/LYBT.Module.Formula/LYBT.Module.Formula.csproj
```

---

### Phase 6: 数据库迁移（10分钟）

**步骤**:
1. 创建迁移：`dotnet ef migrations add AddIndicationToFormula`
2. 检查生成的迁移文件
3. 应用迁移：`dotnet ef database update`
4. 验证数据库表结构

**验证**:
```sql
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Formulas' AND COLUMN_NAME = 'Indication';
```

---

### Phase 7: 集成测试（30分钟）

#### 7.1 启动服务

```bash
cd src/Server/Services/LYBT.WebAPI
dotnet run
```

#### 7.2 测试场景1：创建验方

**请求**:
```http
POST /api/v1/formulas
Content-Type: application/json

{
  "name": "白虎汤测试",
  "effect": "清热生津",
  "indication": "阳明气分热盛。症见壮热面赤，烦渴饮引，大汗恶热，脉洪大有力或滑数",
  "herbs": [
    {
      "herbName": "石膏",
      "quantity": 30,
      "unit": "g",
      "processingMethod": "生"
    },
    {
      "herbName": "知母",
      "quantity": 9,
      "unit": "g",
      "processingMethod": "肥"
    },
    {
      "herbName": "粳米",
      "quantity": 6,
      "unit": "g"
    },
    {
      "herbName": "甘草",
      "quantity": 3,
      "unit": "g",
      "processingMethod": "炙"
    }
  ]
}
```

**预期结果**:
- ✅ 返回201 Created
- ✅ 返回完整的FormulaDto（包含Indication和4个Herbs）

#### 7.3 测试场景2：查询验方详情

**请求**:
```http
GET /api/v1/formulas/{id}
```

**预期结果**:
- ✅ 返回200 OK
- ✅ `indication`字段有值："阳明气分热盛..."
- ✅ `herbs`数组包含4个药材
- ✅ 每个药材包含：herbName, quantity, unit, processingMethod

#### 7.4 测试场景3：更新验方（核心场景）

**请求**:
```http
PUT /api/v1/formulas/{id}
Content-Type: application/json

{
  "name": "白虎汤测试",
  "effect": "清热生津",
  "indication": "阳明气分热盛（已修改）",
  "herbs": [
    {
      "herbName": "石膏",
      "quantity": 21,
      "unit": "g",
      "processingMethod": "生"
    },
    {
      "herbName": "知母",
      "quantity": 9,
      "unit": "g",
      "processingMethod": "肥"
    },
    {
      "herbName": "麦冬",
      "quantity": 15,
      "unit": "g"
    }
  ]
}
```

**预期结果**:
- ✅ 返回200 OK
- ✅ `indication`已更新
- ✅ 药材从4个变为3个（删除粳米和甘草，添加麦冬）
- ✅ 石膏剂量从30g更新为21g

#### 7.5 验证数据库状态

**SQL查询**:
```sql
-- 查询验方
SELECT Id, Name, Effect, Indication FROM Formulas WHERE Name = '白虎汤测试';

-- 查询药材（应该只有3条）
SELECT HerbName, Quantity, Unit, ProcessingMethod
FROM FormulaHerbItems
WHERE FormulaId = @formulaId
ORDER BY HerbName;
```

**预期结果**:
```
HerbName | Quantity | Unit | ProcessingMethod
---------|----------|------|------------------
知母     | 9        | g    | 肥
石膏     | 21       | g    | 生
麦冬     | 15       | g    | NULL
```

---

### Phase 8: 代码提交与PR（15分钟）

#### 8.1 提交代码

```bash
git add .
git commit -m "feat(formula): 完成验方药材管理重构

实现内容：
1. Entity层新增Indication字段
2. 完善FormulaHerbItemInputDto（HerbName、Unit、ProcessingMethod）
3. 重构UpdateAsync实现药材全量替换逻辑
4. 修正AutoMapper配置
5. 数据库迁移

验证：
- ✅ 创建验方（包含4个药材）
- ✅ 更新验方（修改主治、删除2个药材、添加1个药材、修改剂量）
- ✅ 数据库状态正确

Fixes #<issue-number>

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"

git push
```

#### 8.2 创建Pull Request

**PR标题**: `feat(formula): 完成验方药材管理重构`

**PR描述**:
```markdown
## 📋 变更概述

实现验方药材管理核心功能，采用粗粒度（全量替换）方案。

## ✅ 实现内容

### 1. Entity层
- 新增 `Indication` 字段（主治，1000字符）

### 2. DTO层
- 统一字段名：`Indications` → `Indication`
- 完善 `FormulaHerbItemInputDto`：新增 HerbName、Unit、ProcessingMethod

### 3. Service层
- 重构 `UpdateAsync` 方法
- 实现药材全量替换逻辑（Clear + AddRange）
- EF Core自动优化SQL（批量DELETE + 批量INSERT）

### 4. AutoMapper
- 新增 `Indication` 字段映射
- `Herbs` 保留 Ignore（在Service中手动处理）

### 5. 数据库
- 迁移：`AddIndicationToFormula`

## 🧪 测试结果

- ✅ 创建验方：包含主治和4个药材
- ✅ 更新验方：修改主治、删除2个药材、添加1个、修改剂量
- ✅ 查询验方：返回完整数据
- ✅ 数据库状态：药材列表正确更新

## 📐 架构决策

- **决策编号**: Formula-Design-Decision-002
- **方案**: 粗粒度（全量替换）
- **理由**:
  - 符合用户操作模式（Excel表格，一次性保存）
  - 符合业界实践（DDD聚合根整体更新）
  - 性能可接受（1.5KB传输，<10ms数据库操作）

## 📝 相关文档

- 架构决策：`docs/explanation/architecture/server/formula-herb-management-design.md`
- 重构需求：`docs/requirements/formula-herb-management-refactoring-requirements.md`

## 🔗 关联Issue

Fixes #<issue-number>
```

---

## 验证清单

### 功能验证

- [ ] **创建验方**
  - [ ] 可以保存Indication字段
  - [ ] 可以保存Herbs列表（4个药材）
  - [ ] 每个药材包含：HerbName, Quantity, Unit, ProcessingMethod

- [ ] **查询验方**
  - [ ] 返回完整的Indication
  - [ ] 返回完整的Herbs列表
  - [ ] 药材字段完整

- [ ] **更新验方（核心）**
  - [ ] 可以修改Indication
  - [ ] 可以删除药材（4个→3个）
  - [ ] 可以添加药材（3个→5个）
  - [ ] 可以修改药材剂量（21g→30g）
  - [ ] 可以修改炮制方法（生→炙）

- [ ] **删除验方**
  - [ ] 验方被删除后，关联的药材也被删除（级联删除）

### 数据库验证

- [ ] **表结构**
  - [ ] `Formulas`表有`Indication`字段（nvarchar(1000), nullable）
  - [ ] `FormulaHerbItems`表字段完整

- [ ] **数据完整性**
  - [ ] 更新验方后，旧药材被删除
  - [ ] 更新验方后，新药材被插入
  - [ ] 无孤儿记录（FormulaId指向不存在的Formula）

### 性能验证

- [ ] **响应时间**
  - [ ] 创建验方：<200ms
  - [ ] 更新验方：<200ms
  - [ ] 查询验方：<100ms

- [ ] **SQL优化**
  - [ ] 更新验方生成批量SQL（1 DELETE + 1 INSERT）
  - [ ] 无N+1查询问题

### 代码质量

- [ ] **编译**
  - [ ] 0 errors
  - [ ] 0 warnings

- [ ] **代码规范**
  - [ ] 中文注释
  - [ ] 符合项目命名规范
  - [ ] 无硬编码

---

## 风险与依赖

### 已知风险

#### 1. 数据迁移风险

**风险**: 现有数据库中已有验方数据，`Indication`字段为NULL

**缓解措施**:
- `Indication`设为可空（nullable）
- 旧数据保持NULL，不影响现有功能
- 新创建的验方要求填写Indication

**回滚计划**:
```sql
-- 如需回滚迁移
ALTER TABLE Formulas DROP COLUMN Indication;
```

#### 2. DTO字段名变更风险

**风险**: 前端可能使用旧字段名`Indications`（复数）

**缓解措施**:
- 先检查Desktop端代码是否使用`Indications`
- 如果使用，同步修改Desktop端代码
- 使用全局搜索：`Grep -r "Indications" src/Client/`

**影响范围**: Desktop端可能需要同步修改

#### 3. EF Core批量操作性能

**风险**: 验方包含大量药材（>50个）时，性能可能下降

**缓解措施**:
- 业务场景：验方通常5-15个药材，极少超过30个
- 性能测试：验证50个药材的更新时间
- 监控SQL执行时间

**触发条件**: 药材数量>50个（极罕见）

### 依赖项

#### 1. EF Core版本

- **最低版本**: EF Core 8.0
- **当前版本**: ✅ 8.0.0
- **状态**: 满足要求

#### 2. Desktop端同步

- **影响**: Desktop端需要同步修改ViewModel和DTO
- **依赖**: 前端开发团队
- **优先级**: P0（必须同步）

#### 3. 数据库权限

- **要求**: 需要ALTER TABLE权限
- **验证**:
  ```sql
  SELECT HAS_PERMS_BY_NAME('dbo.Formulas', 'OBJECT', 'ALTER');
  ```

---

## 附录

### 附录A: 相关文档

- [验方药材管理设计讨论](../explanation/architecture/server/formula-herb-management-design.md)
- [验方Server端设计文档](../explanation/architecture/server/formula-design.md)
- [验方方法级分析报告](../reports/formula-module-method-level-analysis-2025-11-10.md)

### 附录B: SQL脚本

#### 手动创建Indication字段（如果迁移失败）

```sql
-- 添加字段
ALTER TABLE Formulas
ADD Indication NVARCHAR(1000) NULL;

-- 验证
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Formulas' AND COLUMN_NAME = 'Indication';
```

#### 验证药材更新逻辑

```sql
-- 查询某个验方的所有药材
SELECT
    f.Name AS FormulaName,
    fh.HerbName,
    fh.Quantity,
    fh.Unit,
    fh.ProcessingMethod,
    fh.IsValidated
FROM Formulas f
LEFT JOIN FormulaHerbItems fh ON f.Id = fh.FormulaId
WHERE f.Name = '白虎汤测试'
ORDER BY fh.HerbName;
```

#### 清理测试数据

```sql
-- 删除测试验方（级联删除药材）
DELETE FROM Formulas WHERE Name = '白虎汤测试';
```

### 附录C: 错误码

| 错误码 | 错误信息 | HTTP状态 | 原因 |
|-------|---------|---------|------|
| FORMULA_NOT_FOUND | 验方不存在 | 404 | 指定ID的验方不存在 |
| INVALID_HERBS | 药材列表不能为空 | 400 | Herbs数组为空或null |
| INVALID_HERB_NAME | 药材名称不能为空 | 400 | HerbName为空 |
| INVALID_QUANTITY | 剂量必须大于0 | 400 | Quantity≤0 |

---

**文档版本**: v1.0
**最后更新**: 2025-11-10
**审核状态**: 待审核
