# simplify-desktop-data-layer 设计文档

## 概述

基于 [proposal.md](./proposal.md) 的详细技术设计，简化Desktop客户端数据访问层。

**设计目标**:
- MedicalCaseService从701行精简到~450行
- 统一数据访问路径（仅通过Repository）
- 使用Mapperly替代手写克隆代码
- 清理过期分步工作流属性
- 合并简单模块的Service到Repository

---

## 架构决策记录

### ADR-001: MedicalCaseService数据访问统一

**状态**: 已采纳

**背景**:
MedicalCaseService当前同时注入`IMedicalCaseRepository`和`IMedicalCaseApi`，导致数据访问路径混乱。6个方法直接调用_api绕过Repository。

**决策**:
- 移除`_api`字段，所有数据访问统一通过Repository
- Repository新增4个API封装方法
- 删除Service中110行简单CRUD转发代码

**后果**:
- 正面: 单一数据访问入口，便于未来支持本地存储
- 正面: Repository层统一处理错误和日志
- 负面: Repository代码量增加约50行

### ADR-002: HerbService合并到Repository

**状态**: 已采纳

**背景**:
HerbService共137行，5个方法全部是CRUD转发，无业务逻辑。

**决策**:
- 删除HerbService类
- Repository增强错误处理和日志
- ViewModel直接注入IHerbRepository

**后果**:
- 正面: 简化调用链：ViewModel → Repository
- 正面: 减少137行冗余代码
- 负面: 需要更新2个ViewModel的DI注入

### ADR-003: Mapperly替代手写克隆

**状态**: 已采纳

**背景**:
MedicalCaseService包含50行手写深拷贝代码用于变更检测：
- CloneMedicalCaseDetail
- CloneConsultation
- ClonePrescription

**决策**:
新增MedicalCaseCloneMapper使用Mapperly源生成器自动生成克隆代码。

**后果**:
- 正面: 自动生成克隆代码，减少维护负担
- 正面: 字段变更自动同步
- 负面: 需添加新Mapper文件

### ADR-004: 删除过期分步工作流属性

**状态**: 已采纳

**背景**:
MedicalCaseItem中的CanStartConsultation和CanCreatePrescription属性已过期，无XAML绑定引用。

**决策**:
- 删除这两个属性及相关PropertyChanged通知
- 更新MedicalCaseItemMapper忽略配置

**后果**:
- 正面: 清理过期代码约15行
- 负面: 无

---

## 实现策略

### Phase依赖关系

```
Phase 1: Repository扩展
    │
    ├──> Phase 2: 过期属性清理
    │
    └──> Phase 3: Service数据访问统一
              │
              └──> Phase 4: Mapperly克隆
                        │
                        └──> Phase 5: HerbService合并
                                  │
                                  └──> Phase 6: FormulaService精简
```

### 关键实现点

1. **Repository方法签名**: 返回`Task<T?>`，null表示失败
2. **错误处理**: 使用ClientErrorMessageMapper统一错误消息
3. **日志格式**: `[REPO]`前缀用于Repository，`[SVC]`前缀用于Service
4. **Mapperly配置**: 使用默认深拷贝，无需特殊配置

---

## 变更清单

### Phase 1: MedicalCaseRepository扩展

#### 新增方法

| 文件 | 方法 | 说明 |
|------|------|------|
| `IMedicalCaseRepository.cs` | `SetPrescriptionFlagAsync` | 设置处方标志 |
| `IMedicalCaseRepository.cs` | `UpdateStatusAsync` | 更新医案状态 |
| `IMedicalCaseRepository.cs` | `CancelMedicalCaseAsync` | 取消医案 |
| `IMedicalCaseRepository.cs` | `SaveDraftAsync` | 暂存医案 |

#### 修改文件

| 文件路径 | 修改内容 | 行数变化 |
|----------|----------|----------|
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseRepository.cs` | 新增4个方法签名 | +25行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs` | 实现4个方法 | +60行 |

### Phase 2: 过期属性清理

#### 修改文件

| 文件路径 | 修改内容 | 行数变化 |
|----------|----------|----------|
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/MedicalCaseItem.cs` | 删除CanStartConsultation、CanCreatePrescription属性及RaisePropertyChanged | -12行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseItemMapper.cs` | 删除4个MapperIgnore配置 | -8行 |

### Phase 3: Service数据访问统一

#### 修改文件

| 文件路径 | 修改内容 | 行数变化 |
|----------|----------|----------|
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs` | 移除_api字段、删除CRUD转发、重构API方法 | -240行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseService.cs` | 删除冗余方法签名 | -30行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs` | 更新DI注册 | ~5行 |

### Phase 4: Mapperly克隆

#### 新增文件

| 文件路径 | 说明 |
|----------|------|
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseCloneMapper.cs` | Mapperly克隆映射器 |

#### 修改文件

| 文件路径 | 修改内容 | 行数变化 |
|----------|----------|----------|
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs` | 删除手写Clone方法、使用Mapper | -50行 |

### Phase 5: HerbService合并

#### 删除文件

| 文件路径 | 原因 |
|----------|------|
| `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbService.cs` | 纯CRUD转发，无业务逻辑 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Interfaces/IHerbService.cs` | 随Service删除 |

#### 修改文件

| 文件路径 | 修改内容 | 行数变化 |
|----------|----------|----------|
| `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs` | 新增CreateWithResultAsync等包装方法 | +40行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Interfaces/IHerbRepository.cs` | 新增方法签名 | +15行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs` | 改用IHerbRepository | ~20行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs` | 改用IHerbRepository | ~10行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/HerbsModule.cs` | 移除Service注册 | -1行 |

### Phase 6: FormulaService精简

#### 修改文件

| 文件路径 | 修改内容 | 行数变化 |
|----------|----------|----------|
| `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaService.cs` | 删除CRUD转发方法 | -100行 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Interfaces/IFormulaService.cs` | 删除对应签名 | -30行 |

---

## 依赖关系

### 模块依赖图

```
MedicalCase模块
├── IMedicalCaseRepository (数据访问)
├── IMedicalCaseService (聚合根业务逻辑)
├── MedicalCaseCloneMapper (变更检测)
└── MedicalCaseItemMapper (DTO映射)

Herbs模块
├── IHerbRepository (数据访问+错误处理)
└── [删除] IHerbService

Formula模块
├── IFormulaRepository (数据访问)
└── IFormulaService (业务逻辑: Save/Copy)
```

### 变更顺序约束

- Phase 1必须先于Phase 3完成（Repository需先有方法才能被Service调用）
- Phase 3必须先于Phase 4完成（移除_api后才能删除Clone方法）
- Phase 4必须先于Phase 5完成（确保核心模块稳定后再处理边缘模块）

---

## 测试策略

### 单元测试

| 测试类 | 覆盖范围 |
|--------|----------|
| `MedicalCaseCloneMapperTests` | 验证Mapperly克隆所有字段完整性 |
| `MedicalCaseRepositoryTests` | 验证新增Repository方法 |
| `HerbRepositoryTests` | 验证增强的错误处理方法 |

### 集成测试

| 测试场景 | 验证点 |
|----------|--------|
| 医案创建流程 | CreateMedicalCaseAsync → InitializeAsync → SaveAsync |
| 医案暂存流程 | SaveDraftAsync → Repository.SaveDraftAsync |
| 医案完成流程 | CompleteMedicalCaseAsync → Repository.UpdateStatusAsync |
| 药材CRUD | Repository直接调用 |
| 验方保存 | SaveFormulaAsync业务逻辑 |

### 编译验证

每个Phase完成后执行:
```bash
dotnet build LYBT.Desktop.sln -c Release --no-restore
```

---

## 风险缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| Mapperly克隆遗漏字段 | 中 | 高 | 添加单元测试验证所有字段 |
| ViewModel依赖断裂 | 低 | 高 | 每Phase编译验证 |
| Repository膨胀 | 低 | 中 | 使用基类封装通用逻辑 |
| 运行时异常 | 低 | 高 | 保留日志，添加防御性检查 |

---

## 回滚计划

如果变更失败:

1. **Phase级回滚**: 使用git revert回滚特定Phase的commit
2. **紧急回滚**: 回滚到变更开始前的commit

```bash
# 回滚到Phase开始前
git revert --no-commit HEAD~N..HEAD
git commit -m "revert: simplify-desktop-data-layer Phase X"
```

---

## 成功标准

1. **代码精简**: MedicalCaseService从701行减少到~450行
2. **统一数据访问**: Service全部通过Repository访问数据
3. **自动生成**: 变更检测用克隆逻辑使用Mapperly
4. **过期清理**: 删除分步工作流相关属性
5. **编译通过**: 全量编译0错误0警告
6. **功能正常**: 所有业务功能回归测试通过

---

**设计者**: Claude Code
**日期**: 2026-01-08
**状态**: 待审批
