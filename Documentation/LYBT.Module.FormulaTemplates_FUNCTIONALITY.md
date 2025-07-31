# LYBT.Module.FormulaTemplates 功能说明文档

## 模块概述

药方模板模块是中医诊疗系统的经验方管理核心，负责经典方剂和医生个人经验方的模板化管理。本模块支持经验方的创建、编辑、查询和模板化应用，为医生提供快速配方工具，提高诊疗效率并确保方剂的标准化和规范化。通过模板库的积累，形成医院或医生个人的经验方知识库。

## 业务价值

- **经验积累**: 将优秀的治疗方剂模板化，形成可复用的知识资产
- **提升效率**: 医生可快速选用成熟方剂，减少重复配方工作
- **标准化管理**: 确保常用方剂的用药规范和剂量准确性
- **知识传承**: 促进医生间的经验交流和中医传统方剂的传承
- **质量控制**: 通过模板化减少配方错误，提升医疗质量
- **数据分析**: 为方剂效果统计和优化提供数据支持

## 数据模型

### FormulaTemplateModel (药方模板主实体)

**文件位置**: `LYBT.Module.FormulaTemplates/Models/FormulaTemplateModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Id | Guid | 模板ID（主键） | 自动生成，唯一标识 | 模板记录唯一标识 |
| Name | string | 模板名称 | 必填，最大长度200字符，唯一性 | 方剂标识和检索关键字 |
| Herbs | List&lt;FormulaTemplateHerbItem&gt; | 药材组成列表 | 至少包含一味药材 | 方剂的药材组成配置 |
| Remark | string? | 备注说明 | 可选，最大长度1000字符 | 用法说明、适应症等补充信息 |

### FormulaTemplateHerbItem (模板药材项实体)

**文件位置**: `LYBT.Module.FormulaTemplates/Models/FormulaTemplateModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| HerbId | Guid | 药材ID | 必填，关联药材主数据 | 建立与药材库的关联 |
| HerbName | string | 药材名称 | 必填，冗余存储便于显示 | 药材标识和快速显示 |
| Quantity | decimal | 标准剂量 | 正数，支持小数点后2位 | 方剂中该药材的标准用量 |
| Unit | string | 计量单位 | 必填，如"g"、"ml"、"钱" | 剂量的计量单位 |
| Usage | string? | 特殊用法 | 可选，如"先煎"、"后下" | 该药材的特殊煎煮或使用方法 |

## DTO 数据传输对象

### FormulaTemplateCreateDto (新增模板)

**使用场景**: 创建新的药方模板
**特点**: 包含完整的方剂信息和药材组成配置

```csharp
- Name: 模板名称（必填，string类型，最大200字符）
- Herbs: 药材组成（List<HerbDto>，至少一味药材）
- Remark: 备注说明（可选，string类型，用法和适应症）
```

**验证规则**:
- 模板名称不能为空且在系统中唯一
- 药材列表不能为空
- 每个药材必须有有效的剂量和单位

### FormulaTemplateDetailDto (模板详情)

**使用场景**: 查看完整的模板信息和药材组成
**特点**: 包含关联的药材详细信息和计算字段

```csharp
- Id: 模板ID
- Name: 模板名称
- Herbs: 药材组成详情（包含价格信息）
- Remark: 备注说明
```

### FormulaTemplateDto (模板列表)

**使用场景**: 模板列表展示和快速检索
**特点**: 精简信息，适合列表显示和搜索

### FormulaTemplateEditDto (编辑模板)

**使用场景**: 修改现有模板信息
**特点**: 包含ID标识和所有可修改的字段

```csharp
- Id: 模板ID（必填，用于定位）
- Name: 模板名称（必填，可修改）
- Herbs: 药材组成（可增删改）
- Remark: 备注说明（可修改）
```

### FormulaTemplateImportDto (批量导入)

**使用场景**: Excel或其他格式的批量导入功能
**特点**: 简化的数据结构，便于批量处理

## 服务层 (IFormulaTemplateService & FormulaTemplateService)

### 基础CRUD方法

#### GetByIdAsync

```csharp
Task<FormulaTemplateDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 获取指定模板的详细信息
**业务逻辑**: 
- 根据ID查询模板记录
- 包含关联的药材详细信息
- 使用AutoMapper进行实体到DTO转换
- 处理数据不存在的情况

**使用场景**: 模板详情页面、编辑前数据加载、处方引用模板

#### GetListAsync

```csharp
Task<List<FormulaTemplateDto>> GetListAsync()
```

**功能**: 获取所有模板列表
**业务逻辑**: 
- 查询所有可用的模板记录
- 按名称排序便于查找
- 返回精简的列表信息

**使用场景**: 模板选择列表、检索功能、统计分析

#### AddAsync

```csharp
Task<bool> AddAsync(FormulaTemplateCreateDto dto)
```

**功能**: 创建新的药方模板
**业务逻辑**: 
- 验证模板名称的唯一性
- 验证药材信息的有效性
- 生成新的模板ID
- 建立药材关联关系
- 使用AutoMapper进行DTO到实体转换

**特殊处理**:
- 模板名称重复检查
- 药材ID有效性验证
- 药材剂量合理性检查
- 事务性操作确保数据一致性

**使用场景**: 经验方录入、方剂标准化、知识库建设

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(FormulaTemplateEditDto dto)
```

**功能**: 更新现有模板信息
**业务逻辑**: 
- 验证模板的存在性
- 检查名称修改后的唯一性
- 更新药材组成配置
- 保持模板ID不变

**特殊处理**:
- 增量更新策略
- 药材列表的增删改处理
- 关联关系的重建
- 版本控制和变更历史

**使用场景**: 模板优化、用量调整、方剂改进

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除指定的药方模板
**业务逻辑**: 
- 验证模板的存在性
- 检查是否被处方引用
- 执行软删除或硬删除
- 记录删除操作日志

**安全考虑**:
- 引用检查防止数据不一致
- 权限验证确保操作合法性
- 重要模板的删除确认
- 删除操作的审计跟踪

**使用场景**: 过时模板清理、错误数据修正

### 扩展功能方法

#### ImportAsync

```csharp
Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos)
```

**功能**: 批量导入药方模板
**业务逻辑**: 
- 批量验证导入数据的格式和内容
- 检查重复模板和药材信息
- 执行批量插入操作
- 返回成功导入的记录数

**特殊处理**:
- 大批量数据的性能优化
- 错误数据的跳过和日志记录
- 事务性批量操作
- 导入进度的实时反馈

**使用场景**: 经典方剂的批量录入、系统初始化、数据迁移

#### ExportAsync

```csharp
Task<List<FormulaTemplateDetailDto>> ExportAsync()
```

**功能**: 导出所有模板数据
**业务逻辑**: 
- 查询所有模板的完整信息
- 包含药材的详细配置
- 格式化为便于导出的结构
- 支持多种导出格式

**使用场景**: 数据备份、知识库分享、系统迁移、统计分析

## 仓储层 (IFormulaTemplateRepository & FormulaTemplateRepository)

### 基础数据操作

#### GetByIdAsync

```csharp
Task<FormulaTemplateModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取模板实体
**实现细节**: 
- 使用EF Core的FindAsync方法
- 支持Include加载药材关联数据
- 处理数据不存在的情况

#### GetListAsync

```csharp
Task<List<FormulaTemplateModel>> GetListAsync()
```

**功能**: 获取所有模板记录
**实现细节**: 
- 返回完整的模板列表
- 包含药材组成信息
- 支持排序和筛选扩展

#### AddAsync

```csharp
Task<bool> AddAsync(FormulaTemplateModel model)
```

**功能**: 新增模板到数据库
**实现细节**: 
- 使用EF Core的Add方法
- 级联保存药材关联数据
- 事务性操作确保数据一致性

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(FormulaTemplateModel model)
```

**功能**: 更新模板信息
**实现细节**: 
- 使用EF Core的Update方法
- 处理实体状态跟踪
- 更新药材关联的子实体

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除模板记录
**实现细节**: 
- 先查询再删除的安全模式
- 级联删除关联的药材数据
- 返回操作结果状态

### 扩展数据操作

#### ImportAsync

```csharp
Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos)
```

**功能**: 批量导入模板数据
**实现细节**: 
- 循环处理每个导入项
- 创建实体对象和关联关系
- 逐条保存并统计成功数量
- 处理导入过程中的异常

**性能优化**:
- 批量操作减少数据库往返
- 事务管理确保数据一致性
- 错误处理不影响其他记录

#### ExportAsync

```csharp
Task<List<FormulaTemplateDetailDto>> ExportAsync()
```

**功能**: 导出所有模板数据
**实现细节**: 
- 查询所有模板及关联数据
- 转换为DTO对象便于序列化
- 支持大数据量的分页导出

## 权限控制策略

### 操作权限

- **查看权限**: 所有医生可查看公共模板，个人模板仅创建者可见
- **创建权限**: 执业医生可创建个人模板，管理员可创建公共模板
- **修改权限**: 模板创建者可修改自己的模板，管理员可修改所有模板
- **删除权限**: 需要管理员权限，且只能删除未被引用的模板

### 模板分类

- **公共模板**: 系统预置的经典方剂，所有医生可见可用
- **个人模板**: 医生个人创建的经验方，仅自己可见
- **科室模板**: 科室内共享的方剂模板，科室内医生可见
- **医院模板**: 医院级别的标准方剂，全院医生可见

### 数据安全

- **访问控制**: 根据用户角色和模板属性控制访问权限
- **操作审计**: 记录所有模板操作的用户、时间和内容
- **数据备份**: 定期备份重要的方剂模板数据

## 日志审计机制

### 操作日志

所有模板相关操作都会记录详细日志：

- **模板创建**: 记录创建者、模板信息和药材组成
- **模板修改**: 记录修改前后的变更内容和操作者
- **模板删除**: 记录删除原因和完整的模板信息
- **模板使用**: 记录模板在处方中的使用情况

### 业务日志

- **导入操作**: 记录批量导入的文件信息和处理结果
- **导出操作**: 记录数据导出的范围和操作者
- **查询统计**: 记录模板的查询和使用频次
- **错误处理**: 记录业务异常和错误处理过程

### 审计内容

- 操作时间和操作者信息
- 操作类型和操作对象
- 关键数据的变更前后状态
- 业务上下文和关联信息

## 集成依赖

### 外部模块依赖

- **LYBT.Module.Herbs**: 药材基础数据查询和验证
- **LYBT.Module.DiagnosisTreatment**: 诊疗中的方剂应用
- **LYBT.Module.Prescriptions**: 处方生成时的模板引用
- **LYBT.Module.Doctors**: 医生信息和权限验证

### 基础服务依赖

- **IUnifiedLogService**: 统一日志服务
- **IMapper**: AutoMapper对象映射服务
- **FormulaTemplateDbContext**: 专用数据库上下文
- **ICacheService**: 缓存服务（用于频繁查询的模板）

## 使用示例

### 创建药方模板

```csharp
var createDto = new FormulaTemplateCreateDto
{
    Name = "银翘散",
    Remark = "辛凉解表，清热解毒。主治风热感冒，发热头痛，咳嗽，口渴，咽红。",
    Herbs = new List<HerbDto>
    {
        new HerbDto 
        { 
            Id = honeysuckleId, 
            Name = "金银花", 
            // Note: In template, we store quantity and unit separately
        },
        new HerbDto 
        { 
            Id = forsythiaId, 
            Name = "连翘"
        },
        new HerbDto 
        { 
            Id = platycodonId, 
            Name = "桔梗"
        },
        new HerbDto 
        { 
            Id = peppermintId, 
            Name = "薄荷"
        }
    }
};

var success = await templateService.AddAsync(createDto);
if (success)
{
    logger.LogInformation("药方模板创建成功: {TemplateName}", createDto.Name);
}
```

### 查询和使用模板

```csharp
// 获取所有可用模板
var templates = await templateService.GetListAsync();

// 根据名称查找特定模板
var yinqiaoTemplate = templates.FirstOrDefault(t => 
    t.Name.Contains("银翘散"));

if (yinqiaoTemplate != null)
{
    // 获取模板详情
    var templateDetail = await templateService.GetByIdAsync(yinqiaoTemplate.Id);
    
    // 在诊疗中应用模板
    var formulaFromTemplate = new FormulaDto
    {
        Name = templateDetail.Name,
        Herbs = templateDetail.Herbs.Select(h => new HerbItemDto
        {
            HerbId = h.Id,
            Name = h.Name,
            Amount = GetStandardQuantity(h), // 从模板获取标准剂量
            UnitPrice = h.Price
        }).ToList()
    };
}
```

### 批量导入模板

```csharp
var importList = new List<FormulaTemplateImportDto>
{
    new FormulaTemplateImportDto
    {
        Name = "麻黄汤",
        Remark = "发汗解表，宣肺平喘。主治外感风寒表实证。",
        Herbs = GetMahuangTangHerbs()
    },
    new FormulaTemplateImportDto
    {
        Name = "桂枝汤",
        Remark = "解肌发表，调和营卫。主治外感风寒表虚证。",
        Herbs = GetGuizhiTangHerbs()
    }
};

var importedCount = await templateService.ImportAsync(importList);
logger.LogInformation("成功导入 {Count} 个药方模板", importedCount);
```

### 更新模板信息

```csharp
var editDto = new FormulaTemplateEditDto
{
    Id = existingTemplateId,
    Name = "银翘散（改良方）",
    Remark = "在原方基础上加减，增强清热解毒功效",
    Herbs = updatedHerbList // 调整后的药材组成
};

var updateSuccess = await templateService.UpdateAsync(editDto);
if (updateSuccess)
{
    logger.LogInformation("模板更新成功: {TemplateId}", editDto.Id);
}
```

### 导出模板数据

```csharp
// 导出所有模板
var exportData = await templateService.ExportAsync();

// 转换为Excel格式
var excelData = exportData.Select(template => new
{
    模板名称 = template.Name,
    药材数量 = template.Herbs.Count,
    主要药材 = string.Join("、", template.Herbs.Take(3).Select(h => h.Name)),
    备注说明 = template.Remark
}).ToList();

// 生成Excel文件
var excelBytes = ExcelHelper.GenerateExcel(excelData);
```

### 模板统计分析

```csharp
// 获取使用频次统计
public async Task<List<TemplateUsageStatsDto>> GetTemplateUsageStatsAsync()
{
    var templates = await templateService.GetListAsync();
    var usageStats = new List<TemplateUsageStatsDto>();
    
    foreach (var template in templates)
    {
        var usageCount = await GetTemplateUsageCountAsync(template.Id);
        usageStats.Add(new TemplateUsageStatsDto
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            UsageCount = usageCount,
            LastUsedTime = await GetLastUsedTimeAsync(template.Id)
        });
    }
    
    return usageStats.OrderByDescending(s => s.UsageCount).ToList();
}
```

## 业务扩展建议

### 功能增强

- **模板分类**: 按功效、病症、季节等维度对模板进行分类管理
- **智能推荐**: 基于症状描述智能推荐合适的方剂模板
- **版本管理**: 支持模板的版本控制和历史追溯
- **协作编辑**: 支持多医生协作完善模板内容

### 质量管理

- **专家审核**: 建立模板的专家审核机制确保质量
- **效果跟踪**: 跟踪基于模板开具处方的治疗效果
- **持续优化**: 基于使用反馈持续优化模板配方
- **标准化**: 建立模板创建和维护的标准化流程

### 知识管理

- **经典方库**: 集成传统经典方剂的标准化模板
- **医案关联**: 将模板与典型医案进行关联展示
- **学习辅助**: 为医学生和年轻医生提供学习参考
- **研究支持**: 为方剂研究提供数据支持和统计分析