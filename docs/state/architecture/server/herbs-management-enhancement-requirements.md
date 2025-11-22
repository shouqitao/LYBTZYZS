# 药材管理功能完善需求分析

**文档版本**: v1.1
**创建日期**: 2025-11-09
**最后更新**: 2025-11-09（所有开放问题已确认）
**状态**: ✅ 需求已确认
**相关模块**: Herbs（药材管理）
**参考模块**: Patients（患者管理）、Users（用户管理）

---

## 📋 目录

- [1. 需求概述](#1-需求概述)
- [2. 功能性需求](#2-功能性需求)
- [3. 非功能性需求](#3-非功能性需求)
- [4. 业务规则](#4-业务规则)
- [5. 数据模型](#5-数据模型)
- [6. 技术约束](#6-技术约束)
- [7. 参考实现分析](#7-参考实现分析)
- [8. 开放问题](#8-开放问题)

---

## 1. 需求概述

### 1.1 业务目标

完善药材管理模块功能，参考患者管理和用户管理的最佳实践，提供完整的CRUD操作、批量导入导出功能，确保药材档案管理的完整性和高效性。

### 1.2 目标用户

- **管理员**: 批量导入药材数据、数据导出备份、管理药材档案
- **药师**: 维护药材信息、编辑价格和库存信息、管理药材分类
- **医生**: 查看药材信息（用于处方开具）

### 1.3 核心场景

1. **批量导入**: 从Excel文件批量导入药材基本信息（新开诊所初始化药材库）
2. **数据导出**: 导出药材档案到Excel文件（数据备份、价格更新、药材清单）
3. **完整CRUD**: 查看药材详情、编辑药材信息、删除药材档案
4. **分类筛选**: 按药材分类筛选和导出数据

### 1.4 当前状态与差距分析

| 功能模块 | 当前状态 | 目标状态 | 差距 |
|---------|---------|---------|------|
| **药材列表** | ✅ 已实现（支持分页和分类筛选） | ✅ 保持现状 | ✅ 无差距 |
| **新增药材** | ✅ 已实现 | ✅ 保持现状 | ✅ 无差距 |
| **查看药材** | ✅ 已实现 | ✅ 保持现状 | ✅ 无差距 |
| **编辑药材** | ✅ 已实现 | ✅ 保持现状 | ✅ 无差距 |
| **删除药材** | ✅ 已实现（软删除） | ✅ 需增强：检查处方引用 | 🟡 需增强 |
| **批量删除** | ✅ 已实现（Issue #1169） | ✅ 需增强：检查处方引用 | 🟡 需增强 |
| **批量导入** | ⚠️ 接口已定义（Issue #1166） | ✅ 需完整实现 | 🔴 需实现 |
| **批量导出** | ⚠️ 接口已定义（Issue #1166） | ✅ 需完整实现 | 🔴 需实现 |
| **导入模板** | ⚠️ 接口已定义（Issue #1166） | ✅ 需完整实现 | 🔴 需实现 |
| **分页功能** | ✅ 已实现 | ✅ 保持现状 | ✅ 无差距 |

---

## 2. 功能性需求

### FR-001: 批量导入药材数据

**描述**: 管理员/药师可以从Excel文件批量导入药材基本信息

**User Story**:
```
作为 管理员/药师
我想要 从Excel文件批量导入药材数据
以便 快速初始化药材库，减少手工录入工作量
```

**验收标准**:
- [x] 支持Excel文件格式（.xlsx）
- [x] Excel模板包含列：
  - **必填列**: 药材名称、单位、单价
  - **可选列**: 拼音码、产地、规格、成本价、功效说明、用法用量、备注
- [x] 数据验证：
  - 药材名称非空（1-100字符）
  - 单位非空（如：克、两、斤）
  - 单价必须大于0，小于999999.99
  - 成本价（如果填写）必须大于0
  - 拼音码长度不超过50字符
  - 产地、规格、功效、用法、备注长度验证
- [x] 重复性检查：
  - 药材名称已存在时提示（选项：跳过/更新/报错）
  - 拼音码冲突检查（可选）
- [x] 拼音码自动生成：
  - 如果Excel中未填写拼音码，系统根据药材名称自动生成
- [x] 导入结果统计：成功X条、失败Y条、跳过Z条
- [x] **失败数据快速修复机制**（⭐ 核心体验，参考患者模块）：
  - **自动导出失败数据Excel**：
    - 文件名：`药材导入失败数据_YYYYMMDD_HHmmss.xlsx`
    - 包含列：原始行号 + 所有原始数据列 + 失败原因列
    - 示例：第5行 | 当归 | 克 | 12.5 | ❌ 单价格式错误（需为数字）
    - 导出后自动打开文件所在目录
  - **失败原因详细说明**：
    - 每条失败记录显示具体错误（如"第5行：单价格式错误，当前值'十二元'不是有效数字"）
    - 提供修复建议（如"请修改为数字格式，如：12.5"）
  - **增量导入支持**：
    - 用户修复失败的Excel后，可以直接导入
    - 系统通过药材名称识别已存在记录
    - 提供选项：跳过 or 更新现有记录
  - **导入历史记录**（导入结果对话框）：
    - 显示本次导入的详细结果列表（可滚动查看所有失败记录）
    - 每条失败记录可点击查看完整信息
    - 提供"导出失败数据"按钮一键导出
- [x] 导入进度条显示（导入过程中禁止关闭窗口）
- [x] 支持最大10MB文件，最多10000条记录

**技术实现要点**:
- **Excel处理库**: EPPlus（MIT许可，.NET生态成熟）
- **架构层分配**:
  - Server端: 提供批量添加API `POST /api/herbs/import`（已有接口定义）
  - Desktop端: Excel读取、数据转换、文件对话框、进度显示
- **API兼容性**: 使用现有导入API，保持现有单个添加API不变
- **拼音码生成**: 使用TinyPinyin.NET或自定义拼音库

---

### FR-002: 批量导出药材数据

**描述**: 管理员/药师可以将药材数据导出到Excel文件

**User Story**:
```
作为 管理员/药师
我想要 将药材数据导出到Excel文件
以便 进行数据备份、价格更新、药材清单打印
```

**验收标准**:
- [x] 支持导出当前筛选条件下的所有药材数据（如：按分类筛选）
- [x] 支持导出全部药材数据（忽略筛选）
- [x] Excel文件包含列：
  - **基本信息**: 药材名称、拼音码、单位、单价、成本价
  - **扩展信息**: 产地、规格、功效说明、用法用量、备注
  - **统计信息**: 状态、创建时间、最后更新时间
- [x] 文件命名规范：
  - 全部导出：`药材档案_YYYYMMDD_HHmmss.xlsx`
  - 分类导出：`药材档案_{分类名称}_YYYYMMDD_HHmmss.xlsx`
- [x] 导出进度条显示（大数据量场景）
- [x] 导出成功后自动打开文件所在目录
- [x] 支持最大导出10000条记录（超过时提示分批导出）

**技术实现要点**:
- **Excel生成**: EPPlus（Desktop端生成）
- **数据获取**: 通过现有API `GET /api/herbs/export?category={分类}`（已有接口定义）
- **导出策略**: Desktop端处理（不在Server端生成Excel）

---

### FR-003: 导出导入模板

**描述**: 管理员/药师可以下载带示例数据的导入模板

**User Story**:
```
作为 管理员/药师
我想要 下载包含示例数据的Excel导入模板
以便 了解导入格式要求，减少导入错误
```

**验收标准**:
- [x] 模板包含所有必填列和可选列
- [x] 模板第一行为列标题（中文）
- [x] 模板包含3行示例数据：
  - 示例1：当归 | DG | 克 | 12.50 | 10.00 | 甘肃 | 特级 | 补血活血 | 煎服6-12克 |
  - 示例2：黄芪 | HQ | 克 | 18.00 | 15.00 | 内蒙古 | 一级 | 补气升阳 | 煎服9-30克 |
  - 示例3：党参 | DS | 克 | 25.00 | 20.00 | 山西 | 特级 | 健脾益肺 | 煎服9-15克 |
- [x] 模板包含数据格式说明（作为批注或单独的说明页）
- [x] 文件命名：`药材导入模板_YYYYMMDD.xlsx`
- [x] 下载后自动打开文件所在目录

**技术实现要点**:
- **模板生成**: Desktop端生成（使用EPPlus）
- **API调用**: 使用现有API `GET /api/herbs/import-template`（已有接口定义）

---

### FR-004: 删除药材前的关联检查（增强现有功能）

**描述**: 删除药材前检查是否被处方引用，防止数据不一致

**User Story**:
```
作为 管理员/药师
我想要 在删除药材前得到引用检查提示
以便 避免删除正在使用的药材，保证数据一致性
```

**验收标准**:
- [x] 删除药材前查询是否被处方引用
- [x] 如果被引用：
  - 显示引用统计（"该药材被X个处方引用，共Y次"）
  - 显示最近的5个引用处方（处方编号、患者姓名、开方日期）
  - 提供选项：
    - "仅软删除"（保留数据，标记为已删除，处方仍可查看历史）
    - "取消删除"（不删除）
  - 禁止物理删除
- [x] 如果未被引用：
  - 允许软删除
  - 确认对话框："确认删除药材'{药材名称}'？"
- [x] 批量删除时：
  - 逐个检查引用关系
  - 显示统计：X个可删除，Y个被引用无法删除
  - 提供详细列表供用户确认

**技术实现要点**:
- **关联查询**: 查询Prescription表和PrescriptionItem表
- **API接口**: 需在Server端Service层实现引用检查逻辑
- **UI交互**: Desktop端显示引用详情对话框

---

### FR-005: 优化药材列表UI（可选，低优先级）

**描述**: 优化药材列表的列宽和操作列对齐，参考验方列表实现

**验收标准**:
- [x] 操作列右对齐
- [x] 数据列自适应宽度
- [x] 列宽调整后记忆用户偏好

**技术实现要点**:
- **参考实现**: 验方列表（FormulaListView）
- **UI框架**: WPF DataGrid

---

## 3. 非功能性需求

### NFR-001: 性能

- 批量导入：1000条药材记录 < 10秒
- 批量导出：10000条药材记录 < 5秒
- 删除前引用检查：< 500ms
- 分页查询：< 300ms

### NFR-002: 安全

- 批量导入/导出功能仅限管理员和药师
- 删除药材需要管理员或药师权限
- 导入时验证数据完整性，防止SQL注入

### NFR-003: 可用性

- 导入失败时提供详细的错误信息和修复建议
- 导入/导出进度实时显示
- 操作成功后自动刷新列表
- 文件导出后自动打开所在目录

### NFR-004: 兼容性

- 支持Excel 2007及以上版本（.xlsx格式）
- 支持Windows 10及以上操作系统
- 兼容.NET 8.0运行时

---

## 4. 业务规则

### BR-001: 药材名称唯一性

- **规则**: 药材名称在系统中必须唯一（不区分大小写）
- **理由**: 防止重复录入，确保药材库的唯一性
- **实现**: Repository层唯一性验证，数据库添加唯一索引
- **导入时处理**:
  - 发现重复时提示用户选择：跳过/更新/报错
  - 默认策略：跳过

### BR-002: 拼音码自动生成

- **规则**: 如果未提供拼音码，系统根据药材名称自动生成
- **理由**: 提高录入效率，支持快速检索
- **实现**: Service层在保存前检查拼音码，如为空则自动生成
- **生成规则**: 取每个汉字的拼音首字母，转大写（如：当归 → DG）

### BR-003: 单价验证规则

- **规则**: 单价必须大于0，小于999999.99
- **理由**: 防止异常数据，确保价格合理性
- **实现**: FluentValidation验证器（参考Epic #1961）

### BR-004: 批量导入重复处理策略

- **规则**: 导入时发现药材名称重复，提供三种处理策略
  - **跳过**: 保留数据库现有记录，跳过导入（默认）
  - **更新**: 用Excel数据覆盖数据库现有记录
  - **报错**: 标记为失败，记录到失败数据Excel
- **理由**: 提供灵活的数据导入策略，满足不同场景需求
- **实现**: Desktop端导入对话框提供策略选择，Service层执行相应逻辑

### BR-005: 删除前处方引用检查

- **规则**: 删除药材前必须检查是否被处方引用
  - 如果被引用：仅允许软删除，禁止物理删除
  - 如果未被引用：允许软删除
- **理由**: 保证数据一致性，防止孤儿处方
- **实现**: Service层在删除前查询Prescription和PrescriptionItem表

### BR-006: 批量操作限制

- **规则**: 批量导入最多10000条，批量删除最多100条
- **理由**: 防止性能问题和误操作
- **实现**: Controller层参数验证

### BR-007: 软删除策略

- **规则**: 药材删除采用软删除（IsDeleted标记）
- **理由**: 保留历史数据，支持数据恢复，保证处方历史查询
- **实现**: Repository层Update操作，设置IsDeleted=true

### BR-008: 成本价可选性

- **规则**: 成本价为可选字段，未填写时不影响保存
- **理由**: 部分药材可能无成本价信息，不应阻止保存
- **实现**: DTO和Entity均定义为可空字段

---

## 5. 数据模型

### 5.1 Herb实体（现有）

```csharp
public class Herb : BaseEntity
{
    // Id继承自BaseEntity（Guid类型）

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;           // 药材名称（必填）

    [StringLength(50)]
    public string? PinYinCode { get; set; }                    // 拼音码（可选，自动生成）

    [StringLength(100)]
    public string? Origin { get; set; }                        // 产地（可选）

    [StringLength(100)]
    public string? Spec { get; set; }                          // 规格（可选）

    [Required]
    [StringLength(10)]
    public string Unit { get; set; } = "克";                   // 单位（必填，默认"克"）

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }                         // 单价（必填）

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CostPrice { get; set; }                    // 成本价（可选）

    [StringLength(500)]
    public string? Effect { get; set; }                        // 功效说明（可选）

    [StringLength(500)]
    public string? Usage { get; set; }                         // 用法用量（可选）

    [StringLength(500)]
    public string? Remark { get; set; }                        // 备注（可选）

    public CommonStatus Status { get; set; } = CommonStatus.Enabled;  // 状态（默认启用）

    // 审计字段继承自BaseEntity：
    // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion, IsDeleted
}
```

### 5.2 HerbInputDto（现有）

```csharp
public class HerbInputDto
{
    public Guid? Id { get; set; }                               // 更新时必填，创建时为null

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;            // 药材名称（必填）

    [StringLength(50)]
    public string? PinYinCode { get; set; }                     // 拼音码（可选）

    [StringLength(100)]
    public string? Origin { get; set; }                         // 产地（可选）

    [StringLength(50)]
    public string? Spec { get; set; }                           // 规格（可选）

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = "克";                    // 单位（必填）

    [Required]
    [Range(0, 999999.99)]
    public decimal Price { get; set; }                          // 单价（必填）

    [Range(0, 999999.99)]
    public decimal? CostPrice { get; set; }                     // 成本价（可选）

    [StringLength(1000)]
    public string? Effect { get; set; }                         // 功效说明（可选）

    [StringLength(500)]
    public string? Usage { get; set; }                          // 用法用量（可选）

    [StringLength(500)]
    public string? Remark { get; set; }                         // 备注（可选）

    public CommonStatus Status { get; set; } = CommonStatus.Enabled;  // 状态
}
```

### 5.3 Excel导入列映射

| Excel列名 | 对应字段 | 必填 | 验证规则 | 示例 |
|----------|---------|-----|---------|------|
| 药材名称 | Name | ✅ | 1-100字符，唯一 | 当归 |
| 拼音码 | PinYinCode | ❌ | 最多50字符 | DG |
| 单位 | Unit | ✅ | 1-20字符 | 克 |
| 单价 | Price | ✅ | 数字，0-999999.99 | 12.50 |
| 成本价 | CostPrice | ❌ | 数字，0-999999.99 | 10.00 |
| 产地 | Origin | ❌ | 最多100字符 | 甘肃 |
| 规格 | Spec | ❌ | 最多50字符 | 特级 |
| 功效说明 | Effect | ❌ | 最多1000字符 | 补血活血，调经止痛 |
| 用法用量 | Usage | ❌ | 最多500字符 | 煎服，6-12克 |
| 备注 | Remark | ❌ | 最多500字符 | 过敏体质慎用 |

---

## 6. 技术约束

### 6.1 技术栈限制（基于MVP Constitution）

- ✅ **数据库**: SQL Server（使用EF Core）
- ✅ **持久化**: Entity Framework Core 8.0
- ✅ **架构**: 三层架构（Repository → Service → Controller）
- ✅ **Excel处理**: EPPlus（MIT许可）
- ✅ **拼音库**: TinyPinyin.NET或自定义拼音库
- ❌ **禁止**: Redis缓存、事件驱动、CQRS

### 6.2 架构层分配

**Server端**:
- Repository: IHerbRepository, HerbRepository（已有）
- Service: IHerbService, HerbService（已有接口，需完善实现）
- Controller: HerbsController（已有接口定义）
- Entity: Herb（已有）
- Mapping: HerbMappingProfile（需确认）

**Shared层**:
- DTOs: HerbDto, HerbInputDto, HerbDetailDto（已有）
- Validators: HerbInputDtoValidator（需新增，参考Epic #1961）
- Common: BatchImportResultDto, ImportResultDto（已有）

**Client端（Desktop）**:
- ViewModels: HerbListViewModel（需确认）, HerbDetailViewModel（需确认）
- Views: HerbListView, HerbDetailView（需确认）
- Services: HerbService（Client端业务逻辑）
- Excel处理: HerbImportService, HerbExportService（需新增）

### 6.3 模块定位

- 属于: **Herbs模块**
- 依赖: Prescriptions模块（删除时检查引用）
- 被依赖: Prescriptions模块（开方时选择药材）

---

## 7. 参考实现分析

### 7.1 患者模块批量导入导出（Epic #1934）

**可复用的设计模式**:
1. ✅ 失败数据快速修复机制
2. ✅ 导入进度条显示
3. ✅ 增量导入支持
4. ✅ Desktop端Excel处理
5. ✅ Server端批量API设计
6. ✅ EPPlus库使用

**参考代码文件**:
- `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs:BatchImportAsync()`
- `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:Import()`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/` (Excel处理逻辑)

### 7.2 药材模块现有实现（Issue #1166）

**已有基础**:
- ✅ IHerbService接口定义（包含导入导出接口）
- ✅ HerbsController API端点定义
- ✅ Herb实体和DTO定义
- ⚠️ 实现可能不完整，需要验证和完善

**需要验证的部分**:
1. HerbService的BatchImportAsync实现是否完整
2. Desktop端的Excel导入导出UI是否实现
3. 失败数据恢复机制是否实现
4. 删除前引用检查是否实现

---

## 8. 开放问题

### Q1: 药材分类体系设计 ✅ 已确认

**问题**: 药材分类是否需要多级分类？

**决策**: **选择 A - 单级分类**（2025-11-09确认）

**实施方案**:
- 在Herb实体中增加Category字段（string类型，可选）
- 分类值：解表药、清热药、补益药、理气药、活血化瘀药、止咳化痰药等
- 支持自由输入分类名称（不限制枚举值）
- Excel导入时支持分类列
- 后续如需升级为多级分类，可通过数据迁移实现

---

### Q2: 拼音码生成规则 ✅ 已确认

**问题**: 拼音码是手动输入还是自动生成？

**决策**: **选择 A - 自动生成**（2025-11-09确认）

**实施方案**:
- 使用TinyPinyin.NET库进行拼音转换
- 生成规则：取每个汉字的拼音首字母，转大写（如：当归 → DG，黄芪 → HQ）
- Service层在保存前检查PinYinCode字段：
  - 如果为空或null，自动生成
  - 如果已填写，保留用户输入
- Excel导入时支持拼音码列（可选）：
  - 如果填写，使用Excel中的值
  - 如果未填写，自动生成
- Desktop端创建/编辑界面提供"自动生成拼音码"按钮

---

### Q3: 库存管理功能 ✅ 已确认

**问题**: 是否需要实现库存管理功能？

**决策**: **选择 A - 不实现**（2025-11-09确认）

**实施方案**:
- MVP阶段不实现库存管理功能
- Herb实体不增加库存相关字段（StockQuantity、SafetyStock等）
- 仅记录药材基本信息、价格、功效等静态数据
- 满足基本的处方开具需求（选择药材、计算价格）
- 后续根据业务需求单独规划"库存管理Epic"（包含入库、出库、盘点、预警等功能）

---

### Q4: 成本价管理 ✅ 已确认

**问题**: 成本价是否需要历史记录？

**决策**: **选择 A - 不记录历史**（2025-11-09确认）

**实施方案**:
- Herb实体中CostPrice字段仅保存当前成本价（decimal?类型，可选）
- 成本价变更时直接更新覆盖，不保留历史
- UpdatedAt和UpdatedBy字段记录最后修改时间和人员（审计用途）
- MVP阶段满足基本的成本价记录需求
- 后续如需价格历史分析功能，可新增HerbPriceHistory表实现

---

### Q5: 删除策略 ✅ 已确认

**问题**: 被处方引用的药材是否允许删除？

**决策**: **选择 A - 仅允许软删除**（2025-11-09确认）

**实施方案**:
- 删除药材前查询Prescription和PrescriptionItem表检查引用关系
- **被引用的药材**：
  - 仅允许软删除（设置IsDeleted=true）
  - 保留数据库记录，保证处方历史查询完整性
  - 列表查询时过滤已删除记录（WHERE IsDeleted = 0）
  - 新开处方时不显示已删除药材
  - 删除对话框显示引用统计和最近5个引用处方
- **未被引用的药材**：
  - 允许软删除
  - 确认对话框提示用户
- **批量删除**：
  - 逐个检查引用关系
  - 显示统计：X个可删除，Y个被引用
  - 提供详细列表供用户确认
- **物理删除**：MVP阶段不提供，所有删除均为软删除

---

## 📎 参考资料

- [患者管理功能完善需求文档](../../../explanation/patient-management-enhancement-requirements.md)
- [患者管理功能完善设计文档](../../../explanation/patient-management-enhancement-design.md)
- [Epic #1934: 患者管理功能完善](https://github.com/shouqitao/LYBTZYZS/issues/1934)
- [Issue #1166: 药材批量导入导出](https://github.com/shouqitao/LYBTZYZS/issues/1166)
- [Issue #1169: 药材批量删除](https://github.com/shouqitao/LYBTZYZS/issues/1169)
- [Epic #1961: FluentValidation统一设计](https://github.com/shouqitao/LYBTZYZS/issues/1961)
- [ADR-008: Repository模式设计](../../../adr/ADR-008-repository-pattern.md)
- [三层架构指南](../README.md)

---

**下一步**:
1. ✅ 用户确认需求和开放问题（2025-11-09完成）
   - Q1-Q5所有开放问题已按推荐方案确认
   - 需求文档状态更新为"需求已确认"
2. 🔄 生成设计文档（调用 `lybtzyzs-design-generator`）
3. ⏭️ 任务拆分（调用 `lybtzyzs-task-breakdown`）
4. ⏭️ 创建GitHub Issues（调用 `lybtzyzs-issue-template`）
5. ⏭️ 任务执行（调用 `lybtzyzs-task-executor`）

---

**文档维护**:
- 本文档随需求变更持续更新
- 开放问题确认后更新对应章节
- 设计文档生成后添加链接引用
