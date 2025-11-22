# FluentValidation统一设计 - 任务分解清单

> **项目**: FluentValidation统一设计重构
> **设计文档**: [fluentvalidation-unified-design.md](../explanation/fluentvalidation-unified-design.md)
> **需求文档**: [fluentvalidation-unified-design-requirements.md](../explanation/fluentvalidation-unified-design-requirements.md)
> **预计总工时**: 11-18天（含20%缓冲为14-22天）
> **创建日期**: 2025-11-09

---

## 📋 任务总览

| Phase | 任务数 | 预计工时 | 状态 |
|-------|-------|---------|------|
| Phase 1: MedicalCase模块重构 | 12 | 3-5天 | ⏳ 待开始 |
| Phase 2: Prescriptions模块重构 | 12 | 3-5天 | ⏳ 待开始 |
| Phase 3: 验证常量统一管理 | 4 | 2-3天 | ⏳ 待开始 |
| Phase 4: 测试补充与文档更新 | 5 | 2-3天 | ⏳ 待开始 |
| Phase 5: 代码审查与优化 | 4 | 1-2天 | ⏳ 待开始 |
| **总计** | **37** | **11-18天** | **0/37完成** |

---

## Phase 1: MedicalCase模块重构（3-5天）

### 验收标准
- ✅ MedicalCase模块只有1个InputDto和1个验证器
- ✅ 创建和更新功能正常工作
- ✅ 所有测试通过（单元测试 + 集成测试）
- ✅ 编译无错误和警告

### 任务清单

#### 1.1 创建ValidationConstants.cs ⭐ 核心
- **文件**: `src/Shared/LYBT.Shared.Validators/Common/ValidationConstants.cs`
- **描述**: 创建验证常量类，定义通用验证规则
- **关键常量**:
  - 长度限制: `NameMaxLength=100`, `RemarkMaxLength=1000`, `LongRemarkMaxLength=2000`
  - 数值范围: `DosageCountMinValue=1`, `DosageCountMaxValue=100`, `HerbDosageMinValue=0.1m`, `HerbDosageMaxValue=1000m`
  - 正则表达式: `IdCardRegex`, `PhoneRegex`
- **优先级**: 🔴 P0（阻塞后续所有验证器）
- **依赖**: 无
- **状态**: ⏳ 待开始

#### 1.2 创建MedicalCaseInputDto.cs
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseInputDto.cs`
- **描述**: 创建统一的病案输入DTO，替代Create/Update两个DTO
- **关键字段**:
  - `Guid? Id` - 创建时为null，更新时有值（核心区分字段）
  - 必填: `PatientId`, `DoctorId`, `VisitDate`
  - 可选: `ChiefComplaint`, `PresentIllnessHistory`, `PastMedicalHistory`, 等12个字段
- **优先级**: 🔴 P0
- **依赖**: 无
- **状态**: ⏳ 待开始

#### 1.3 创建MedicalCaseInputDtoValidator.cs ⭐ 核心
- **文件**: `src/Shared/LYBT.Shared.Validators/MedicalCase/MedicalCaseInputDtoValidator.cs`
- **描述**: 创建验证器，使用条件验证模式
- **关键验证规则**:
  - 必填字段验证（PatientId, DoctorId）
  - 日期验证（VisitDate ≤ Today）
  - 可选字段长度验证（使用`.When()`条件）
  - 所有规则使用`ValidationConstants`
- **优先级**: 🔴 P0
- **依赖**: 1.1, 1.2
- **状态**: ⏳ 待开始

#### 1.4 更新MedicalCaseService.CreateAsync
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- **描述**: 调整创建方法使用MedicalCaseInputDto
- **关键变更**:
  - 参数从`MedicalCaseCreateDto`改为`MedicalCaseInputDto`
  - 添加Id验证（创建时Id必须为null）
  - 业务规则验证保持不变
- **优先级**: 🟡 P1
- **依赖**: 1.2, 1.3
- **状态**: ⏳ 待开始

#### 1.5 更新MedicalCaseService.UpdateAsync
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- **描述**: 调整更新方法使用MedicalCaseInputDto
- **关键变更**:
  - 参数从`MedicalCaseUpdateDto`改为`MedicalCaseInputDto`
  - 移除id参数（使用`input.Id.Value`）
  - 业务规则验证保持不变
- **优先级**: 🟡 P1
- **依赖**: 1.2, 1.3
- **状态**: ⏳ 待开始

#### 1.6 更新MedicalCaseController
- **文件**: `src/Server/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **描述**: 调整Controller方法使用MedicalCaseInputDto
- **关键变更**:
  - `CreateAsync`: `[FromBody] MedicalCaseInputDto input`
  - `UpdateAsync`: `[FromBody] MedicalCaseInputDto input`
  - API签名保持RESTful风格
- **优先级**: 🟡 P1
- **依赖**: 1.4, 1.5
- **状态**: ⏳ 待开始

#### 1.7 更新MedicalCaseMappingProfile.cs
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/MedicalCaseMappingProfile.cs`
- **描述**: 添加AutoMapper配置
- **关键配置**:
  - `CreateMap<MedicalCaseInputDto, MedicalCaseEntity>()`
  - `.ForMember(dest => dest.Id, opt => opt.Ignore())`
  - `.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())`
- **优先级**: 🟡 P1
- **依赖**: 1.2
- **状态**: ⏳ 待开始

#### 1.8 删除旧DTO和验证器
- **文件**:
  - `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseCreateDto.cs`
  - `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseUpdateDto.cs`
  - `src/Shared/LYBT.Shared.Validators/MedicalCase/MedicalCaseCreateDtoValidator.cs`
  - `src/Shared/LYBT.Shared.Validators/MedicalCase/MedicalCaseUpdateDtoValidator.cs`
- **描述**: 删除旧的Create/Update DTO和验证器
- **优先级**: ⚪ P2
- **依赖**: 1.4, 1.5, 1.6, 1.7
- **状态**: ⏳ 待开始

#### 1.9 编写MedicalCaseInputDtoValidatorTests.cs
- **文件**: `tests/UnitTests/Shared/LYBT.Shared.Validators.Tests/MedicalCase/MedicalCaseInputDtoValidatorTests.cs`
- **描述**: 编写验证器单元测试
- **测试覆盖**:
  - ✅ 有效输入验证通过
  - ✅ 必填字段空值验证失败
  - ✅ 字符串长度超限验证失败
  - ✅ 日期验证（未来日期失败）
  - ✅ 可选字段空值验证通过
- **目标覆盖率**: ≥ 80%
- **优先级**: 🟡 P1
- **依赖**: 1.3
- **状态**: ⏳ 待开始

#### 1.10 编写Service层集成测试
- **文件**: `tests/IntegrationTests/Server/LYBT.Module.MedicalCase.Tests/MedicalCaseServiceTests.cs`
- **描述**: 编写Service层集成测试
- **测试场景**:
  - ✅ 创建病案（Id=null）- 成功
  - ✅ 创建病案（Id有值）- 失败
  - ✅ 更新病案（Id有值）- 成功
  - ✅ 更新病案（不存在的Id）- 失败
- **优先级**: 🟡 P1
- **依赖**: 1.4, 1.5
- **状态**: ⏳ 待开始

#### 1.11 编译验证
- **描述**: 编译整个解决方案，确保无错误和警告
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **验收标准**: 0 errors, 0 warnings
- **优先级**: 🔴 P0
- **依赖**: 1.1-1.8
- **状态**: ⏳ 待开始

#### 1.12 功能测试
- **描述**: 启动应用，测试创建/更新病案功能
- **测试步骤**:
  1. 启动Server和Client
  2. 创建新病案（验证拼音码、年龄自动生成）
  3. 更新病案（验证数据正确保存）
  4. 验证数据库记录
- **验收标准**: 创建/更新功能完全正常
- **优先级**: 🔴 P0
- **依赖**: 1.11
- **状态**: ⏳ 待开始

---

## Phase 2: Prescriptions模块重构（3-5天）

### 验收标准
- ✅ Prescriptions模块只有1个InputDto和2个验证器（Input + Item）
- ✅ 创建和更新功能正常工作
- ✅ 嵌套集合验证正常
- ✅ 所有测试通过

### 任务清单

#### 2.1 创建PrescriptionInputDto.cs
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionInputDto.cs`
- **描述**: 创建统一的处方输入DTO
- **关键字段**:
  - `Guid? Id` - 创建时为null，更新时有值
  - 必填: `MedicalCaseId`, `PrescriptionDate`, `DosageCount`, `List<PrescriptionItemInputDto> Items`
  - 可选: `Usage`, `Precautions`, `Remark`
- **优先级**: 🔴 P0
- **依赖**: Phase 1完成
- **状态**: ⏳ 待开始

#### 2.2 创建PrescriptionInputDtoValidator.cs ⭐ 核心
- **文件**: `src/Shared/LYBT.Shared.Validators/Prescriptions/PrescriptionInputDtoValidator.cs`
- **描述**: 创建验证器，使用条件验证模式
- **关键验证规则**:
  - 必填字段验证（MedicalCaseId, DosageCount）
  - 数值范围验证（DosageCount: 1-100）
  - 嵌套集合验证（Items不为空，至少一项）
  - `RuleForEach(x => x.Items).SetValidator(new PrescriptionItemInputDtoValidator())`
- **优先级**: 🔴 P0
- **依赖**: 2.1, ValidationConstants（Phase 1）
- **状态**: ⏳ 待开始

#### 2.3 保留PrescriptionItemInputDtoValidator.cs
- **文件**: `src/Shared/LYBT.Shared.Validators/Prescriptions/PrescriptionItemInputDtoValidator.cs`
- **描述**: 保留嵌套验证器，用于验证处方项
- **关键验证规则**:
  - HerbId必填
  - Dosage范围验证（0.1-1000）
  - Unit长度验证
- **优先级**: ⚪ P2（已存在，仅需确认）
- **依赖**: ValidationConstants（Phase 1）
- **状态**: ⏳ 待开始

#### 2.4 更新PrescriptionService.CreateAsync
- **文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
- **描述**: 调整创建方法使用PrescriptionInputDto
- **关键变更**:
  - 参数从`PrescriptionCreateDto`改为`PrescriptionInputDto`
  - 添加Id验证（创建时Id必须为null）
  - 嵌套Items映射验证
- **优先级**: 🟡 P1
- **依赖**: 2.1, 2.2
- **状态**: ⏳ 待开始

#### 2.5 更新PrescriptionService.UpdateAsync
- **文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
- **描述**: 调整更新方法使用PrescriptionInputDto
- **关键变更**:
  - 参数从`PrescriptionEditDto`改为`PrescriptionInputDto`
  - 移除id参数（使用`input.Id.Value`）
  - 处理嵌套Items的增删改
- **优先级**: 🟡 P1
- **依赖**: 2.1, 2.2
- **状态**: ⏳ 待开始

#### 2.6 更新PrescriptionController
- **文件**: `src/Server/LYBT.WebAPI/Controllers/PrescriptionController.cs`
- **描述**: 调整Controller方法使用PrescriptionInputDto
- **关键变更**:
  - `CreateAsync`: `[FromBody] PrescriptionInputDto input`
  - `UpdateAsync`: `[FromBody] PrescriptionInputDto input`
- **优先级**: 🟡 P1
- **依赖**: 2.4, 2.5
- **状态**: ⏳ 待开始

#### 2.7 更新PrescriptionMappingProfile.cs
- **文件**: `src/Server/Modules/LYBT.Module.Prescriptions/PrescriptionMappingProfile.cs`
- **描述**: 添加AutoMapper配置
- **关键配置**:
  - `CreateMap<PrescriptionInputDto, PrescriptionEntity>()`
  - `CreateMap<PrescriptionItemInputDto, PrescriptionItemEntity>()`
  - Ignore Id, CreatedAt, UpdatedAt
- **优先级**: 🟡 P1
- **依赖**: 2.1
- **状态**: ⏳ 待开始

#### 2.8 删除旧DTO和验证器
- **文件**:
  - `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionCreateDto.cs`
  - `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionEditDto.cs`
  - `src/Shared/LYBT.Shared.Validators/Prescriptions/PrescriptionCreateDtoValidator.cs`
  - `src/Shared/LYBT.Shared.Validators/Prescriptions/PrescriptionEditDtoValidator.cs`
- **描述**: 删除旧的Create/Edit DTO和验证器
- **优先级**: ⚪ P2
- **依赖**: 2.4, 2.5, 2.6, 2.7
- **状态**: ⏳ 待开始

#### 2.9 编写PrescriptionInputDtoValidatorTests.cs
- **文件**: `tests/UnitTests/Shared/LYBT.Shared.Validators.Tests/Prescriptions/PrescriptionInputDtoValidatorTests.cs`
- **描述**: 编写验证器单元测试
- **测试覆盖**:
  - ✅ 有效输入验证通过
  - ✅ 必填字段空值验证失败
  - ✅ DosageCount范围验证
  - ✅ Items为空验证失败
  - ✅ 嵌套Items验证
- **目标覆盖率**: ≥ 80%
- **优先级**: 🟡 P1
- **依赖**: 2.2
- **状态**: ⏳ 待开始

#### 2.10 编写Service层集成测试
- **文件**: `tests/IntegrationTests/Server/LYBT.Module.Prescriptions.Tests/PrescriptionServiceTests.cs`
- **描述**: 编写Service层集成测试
- **测试场景**:
  - ✅ 创建处方（Id=null）- 成功
  - ✅ 创建处方（Id有值）- 失败
  - ✅ 更新处方（Id有值）- 成功
  - ✅ 更新处方（Items变更）- 成功
- **优先级**: 🟡 P1
- **依赖**: 2.4, 2.5
- **状态**: ⏳ 待开始

#### 2.11 编译验证
- **描述**: 编译整个解决方案
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **验收标准**: 0 errors, 0 warnings
- **优先级**: 🔴 P0
- **依赖**: 2.1-2.8
- **状态**: ⏳ 待开始

#### 2.12 功能测试
- **描述**: 启动应用，测试创建/更新处方功能
- **测试步骤**:
  1. 创建新处方（验证Items集合验证）
  2. 更新处方（验证Items增删改）
  3. 验证数据库记录
- **验收标准**: 创建/更新功能完全正常
- **优先级**: 🔴 P0
- **依赖**: 2.11
- **状态**: ⏳ 待开始

---

## Phase 3: 验证常量统一管理（2-3天）

### 验收标准
- ✅ 所有验证器使用`ValidationConstants`
- ✅ 无硬编码的魔法数字
- ✅ 所有测试通过

### 任务清单

#### 3.1 补充ValidationConstants.cs常量定义
- **文件**: `src/Shared/LYBT.Shared.Validators/Common/ValidationConstants.cs`
- **描述**: 补充其他模块需要的验证常量
- **需要补充的常量**:
  - Users模块: UserName, Password, Email
  - Patients模块: PhoneNumber, Address
  - Herbs/Formula模块: 各字段长度限制
- **优先级**: 🔴 P0
- **依赖**: Phase 2完成
- **状态**: ⏳ 待开始

#### 3.2 更新所有验证器使用常量
- **描述**: 替换所有硬编码的魔法数字为ValidationConstants
- **涉及验证器** (12个):
  1. UserInputDtoValidator
  2. PatientInputDtoValidator
  3. MedicalCaseInputDtoValidator
  4. PrescriptionInputDtoValidator
  5. PrescriptionItemInputDtoValidator
  6. HerbInputDtoValidator
  7. FormulaInputDtoValidator
  8. ConsultationInputDtoValidator
  9. 其他4个验证器
- **优先级**: 🟡 P1
- **依赖**: 3.1
- **状态**: ⏳ 待开始

#### 3.3 编译验证
- **描述**: 编译整个解决方案
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **验收标准**: 0 errors, 0 warnings
- **优先级**: 🔴 P0
- **依赖**: 3.2
- **状态**: ⏳ 待开始

#### 3.4 回归测试
- **描述**: 运行所有验证器测试
- **命令**: `dotnet test LYBT.All.sln -c Release --settings tests/.runsettings`
- **验收标准**: 所有测试通过
- **优先级**: 🔴 P0
- **依赖**: 3.3
- **状态**: ⏳ 待开始

---

## Phase 4: 测试补充与文档更新（2-3天）

### 验收标准
- ✅ 测试覆盖率 ≥ 80%
- ✅ 所有验证器有完整测试
- ✅ 文档完整更新

### 任务清单

#### 4.1 补充单元测试（测试覆盖率 ≥ 80%）
- **描述**: 补充遗漏的单元测试
- **覆盖范围**:
  - 所有验证器的所有验证规则
  - 边界条件测试
  - 嵌套验证测试
- **目标覆盖率**: ≥ 80%
- **优先级**: 🟡 P1
- **依赖**: Phase 3完成
- **状态**: ⏳ 待开始

#### 4.2 补充集成测试（Service层）
- **描述**: 补充Service层集成测试
- **覆盖范围**:
  - CreateAsync/UpdateAsync方法
  - 业务规则验证
  - AutoMapper映射验证
- **优先级**: 🟡 P1
- **依赖**: Phase 3完成
- **状态**: ⏳ 待开始

#### 4.3 更新validation-patterns.md文档
- **文件**: `docs/explanation/validation-patterns.md`
- **描述**: 更新验证模式文档
- **更新内容**:
  - 新增MedicalCase/Prescriptions示例
  - 更新嵌套集合验证说明
  - 更新ValidationConstants使用说明
- **优先级**: 🟡 P1
- **依赖**: Phase 3完成
- **状态**: ⏳ 待开始

#### 4.4 更新shared/README.md架构文档
- **文件**: `docs/explanation/architecture/shared/README.md`
- **描述**: 更新Shared层架构文档
- **更新内容**:
  - 更新Validators层说明
  - 更新InputDto统一模式说明
  - 更新ValidationConstants说明
- **优先级**: 🟡 P1
- **依赖**: Phase 3完成
- **状态**: ⏳ 待开始

#### 4.5 创建迁移指南（如需要）
- **文件**: `docs/how-to/validation-migration-guide.md`
- **描述**: 创建从旧模式迁移到新模式的指南
- **内容**:
  - 迁移步骤
  - 常见问题
  - 示例代码
- **优先级**: ⚪ P2（可选）
- **依赖**: Phase 3完成
- **状态**: ⏳ 待开始

---

## Phase 5: 代码审查与优化（1-2天）

### 验收标准
- ✅ 代码审查通过
- ✅ 性能测试通过
- ✅ 所有验收标准满足

### 任务清单

#### 5.1 代码审查（命名、规范、注释）
- **描述**: 全面代码审查
- **检查项**:
  - ✅ 命名规范（PascalCase, _camelCase）
  - ✅ 注释完整性（中文注释）
  - ✅ 代码风格一致性
  - ✅ 依赖注入规范（仅构造函数注入）
- **工具**: 自动调用 `lybtzyzs-code-review` skill
- **优先级**: 🔴 P0
- **依赖**: Phase 4完成
- **状态**: ⏳ 待开始

#### 5.2 架构合规性检查
- **描述**: 验证是否符合三层架构规范
- **检查项**:
  - ✅ 依赖方向正确（Presentation → Application → Infrastructure）
  - ✅ Repository可见性（internal）
  - ✅ DDD聚合根边界
  - ✅ MVP原则（无过度设计）
- **工具**: 自动调用 `lybtzyzs-arch-compliance` skill
- **优先级**: 🔴 P0
- **依赖**: Phase 4完成
- **状态**: ⏳ 待开始

#### 5.3 性能测试（验证器性能）
- **描述**: 验证器性能测试
- **测试场景**:
  - 验证器性能基准测试
  - API响应时间对比（重构前后）
- **验收标准**: API响应时间±5%
- **优先级**: 🟡 P1
- **依赖**: Phase 4完成
- **状态**: ⏳ 待开始

#### 5.4 最终验收
- **描述**: 最终验收检查
- **验收清单**:
  - ✅ 编译：0 errors, 0 warnings
  - ✅ 测试覆盖率：≥ 80%
  - ✅ 所有单元测试通过
  - ✅ 所有集成测试通过
  - ✅ 功能测试通过
  - ✅ 性能测试通过
  - ✅ 代码审查通过
  - ✅ 架构合规检查通过
  - ✅ 文档完整更新
- **优先级**: 🔴 P0
- **依赖**: 5.1, 5.2, 5.3
- **状态**: ⏳ 待开始

---

## 📊 进度追踪

### 任务完成情况

| Phase | 已完成 | 总计 | 百分比 | 状态 |
|-------|-------|------|--------|------|
| Phase 1 | 0 | 12 | 0% | ⏳ 待开始 |
| Phase 2 | 0 | 12 | 0% | ⏳ 待开始 |
| Phase 3 | 0 | 4 | 0% | ⏳ 待开始 |
| Phase 4 | 0 | 5 | 0% | ⏳ 待开始 |
| Phase 5 | 0 | 4 | 0% | ⏳ 待开始 |
| **总计** | **0** | **37** | **0%** | **⏳ 待开始** |

### 时间线

```
Week 1 (Day 1-5): Phase 1 - MedicalCase模块重构
Week 2 (Day 6-10): Phase 2 - Prescriptions模块重构
Week 3 (Day 11-13): Phase 3 - 验证常量统一管理
Week 3 (Day 14-16): Phase 4 - 测试补充与文档更新
Week 3 (Day 17-18): Phase 5 - 代码审查与优化

预计完成日期: 2025-11-30（含20%缓冲）
```

---

## 🎯 优先级说明

- 🔴 **P0（Critical）**: 阻塞性任务，必须优先完成
- 🟡 **P1（High）**: 高优先级，依赖于P0任务
- ⚪ **P2（Medium）**: 中等优先级，可在P1后完成
- 🔵 **P3（Low）**: 低优先级，可选任务

---

## 📝 任务更新日志

| 日期 | 任务ID | 操作 | 备注 |
|-----|--------|------|------|
| 2025-11-09 | - | 创建任务分解清单 | 初始版本，37个任务 |

---

**文档版本**: v1.0
**创建日期**: 2025-11-09
**下一步**: 创建GitHub Issue #1960

---

**🤖 Generated with [Claude Code](https://claude.com/claude-code)**
