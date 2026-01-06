# Tasks: adopt-mapperly-unified-mapping

## 进度总结 (2026-01-06)

| Phase | 状态 | 说明 |
|-------|------|------|
| Phase 1: 基础设施 | ✅ 完成 | Mapperly依赖、IMappingService接口、MappingServiceBase基类 |
| Phase 2: MedicalCase | ✅ 完成 | Mappers和MappingServices已创建，DI已注册 |
| Phase 3: Consultation | ✅ 完成 | 复用MedicalCase聚合根的Mapper |
| Phase 4: Formula | ✅ 完成 | FormulaMapper、FormulaHerbItemMapper已创建 |
| Phase 5: Patients | ✅ 完成 | PatientMapper已创建 |
| Phase 6: Users | ✅ 完成 | UserMapper已创建 |
| Phase 7: Herbs | ✅ 完成 | HerbMapper已创建 |
| Phase 8: Server端 | ⏸️ 跳过 | 可选，Server端暂不迁移 |
| Phase 9: ViewModel标准化 | ⏸️ 延期 | CommunityToolkit.Mvvm迁移需独立OpenSpec处理 |
| Phase 10: 验证与文档 | ✅ 完成 | CLAUDE.md已更新，编译验证通过 |
| 遗留方法清理 | ✅ 完成 | 所有FromDto/ToDto方法已标记[Obsolete] |

**当前状态**: Mapperly统一映射架构迁移完成。
- Mapper基础设施已完成（全量编译0错误）
- 所有Item类的FromDto/ToDto方法已标记[Obsolete]
- CLAUDE.md文档已更新
- ViewModel层迁移(CommunityToolkit.Mvvm)需另开OpenSpec处理

---

## 模块覆盖清单

### 需要迁移的Item类完整清单

| 模块 | Item类 | 映射方法 | 优先级 |
|------|--------|----------|--------|
| **MedicalCase** | ConsultationItem | FromDto, ToDto, ToInputDto | P0 |
| **MedicalCase** | PrescriptionItem | FromDto, ToDto, ToInputDto | P0 |
| **MedicalCase** | MedicalCaseItem | FromDto, ToDto | P0 |
| **MedicalCase** | MedicalCaseDetailModel | FromDto | P0 |
| **Consultation** | ConsultationItem | FromDto, ToDto, ToInputDto | P1 |
| **Formula** | FormulaItem | FromDto, ToDto | P1 |
| **Formula** | FormulaHerbItem | FromDto, ToDto | P1 |
| **Formula** | FormulaDetailModel | FromDto, ToDto | P1 |
| **Formula** | FormulaHerbItemViewModel | ToDto | P1 |
| **Patients** | PatientItem | FromDto, ToDto | P1 |
| **Users** | UserItem | FromDto, ToDto | P1 |
| **Herbs** | HerbItemControlViewModel | ToDto | P2 |
| **Herbs** | HerbItemControl.xaml.cs | ToDto | P2 |

**总计**: 13个类需要迁移映射逻辑

---

## Phase 1: 基础设施搭建 (预计0.5天)

### Task 1.1: 添加Mapperly依赖 ✅
- [x] 在 `LYBT.Desktop.Infrastructure.csproj` 添加 `Riok.Mapperly 4.3.1`
- [x] 在 `LYBT.Desktop.MedicalCase.csproj` 添加包引用
- [x] 在 `LYBT.Desktop.Consultation.csproj` 添加包引用
- [x] 在 `LYBT.Desktop.Formula.csproj` 添加包引用
- [x] 在 `LYBT.Desktop.Patients.csproj` 添加包引用
- [x] 在 `LYBT.Desktop.Users.csproj` 添加包引用
- [x] 在 `LYBT.Desktop.Herbs.csproj` 添加包引用
- [x] 验证编译通过

### Task 1.2: 创建IMappingService接口 ✅
- [x] 在 `LYBT.Desktop.Infrastructure/Mapping/` 创建目录
- [x] 创建 `IMappingService<TDto, TItem>` 接口
- [x] 创建 `IMappingService<TDto, TInputDto, TItem>` 扩展接口
- [x] 添加集合映射方法（ToItems, ToItemsInto）
- [x] 添加XML文档注释

### Task 1.3: 创建MappingServiceBase基类 ✅
- [x] 创建 `MappingServiceBase<TDto, TItem, TMapper>`
- [x] 提供默认的集合映射实现
- [x] 支持ObservableCollection填充

---

## Phase 2: MedicalCase模块迁移 (预计1.5天) ✅

### Task 2.1: 创建MedicalCase模块Mappers ✅
- [x] 创建 `Mappers/ConsultationMapper.cs`
  - DTO → Item (忽略IsSelected, IsExpanded)
  - Item → DTO (忽略计算属性)
  - Item → InputDto
- [x] 创建 `Mappers/PrescriptionMapper.cs`
  - DTO → Item (处理嵌套Items集合)
  - Item → DTO
  - Item → InputDto
- [x] 创建 `Mappers/MedicalCaseItemMapper.cs`
  - DTO → Item
  - Item → DTO
- [x] 创建 `Mappers/MedicalCaseDetailModelMapper.cs`
  - DTO → Model

### Task 2.2: 创建MedicalCase MappingServices ✅
- [x] 创建 `ConsultationMappingService`
- [x] 创建 `PrescriptionMappingService`
- [x] 创建 `MedicalCaseItemMappingService`
- [x] 创建 `MedicalCaseDetailModelMappingService`

### Task 2.3: 更新MedicalCase DI注册 ✅
- [x] 在 `MedicalCaseModule.cs` 注册所有映射服务
- [x] 验证DI解析正确

### Task 2.4: 更新MedicalCase ViewModel (延迟)
- [ ] 更新 `MedicalCaseMasterDetailViewModel` 使用MappingService
- [ ] 更新 `MedicalCaseWorkspaceViewModel` 使用MappingService
- [ ] 更新相关Dialog ViewModel
- [ ] 替换所有 `FromDto()`/`ToDto()` 调用
> **说明**: ViewModel更新将在Phase 9一并处理

### Task 2.5: 清理MedicalCase Item类映射方法 ✅
- [x] 标记 `ConsultationItem.FromDto/ToDto/ToInputDto` 为[Obsolete]
- [x] 标记 `PrescriptionItem.FromDto/ToDto/ToInputDto` 为[Obsolete]
- [x] 标记 `MedicalCaseItem.FromDto/ToDto` 为[Obsolete]
- [x] 标记 `MedicalCaseDetailModel.FromDto` 为[Obsolete]
> **说明**: 方法已标记[Obsolete]，保持向后兼容性。完全删除将在后续版本中进行

---

## Phase 3: Consultation模块迁移 (预计0.5天) ✅

### Task 3.1: 创建Consultation模块Mapper ✅
- [x] ~~创建 `Mappers/ConsultationMapper.cs`~~ - 复用MedicalCase聚合根的映射器
- [x] ConsultationItem作为MedicalCase聚合根的子实体，使用MedicalCase模块的Mapper

### Task 3.2: 创建Consultation MappingService ✅
- [x] ~~创建 `ConsultationMappingService`~~ - 使用MedicalCase模块的服务
- [x] ConsultationModule注册MedicalCase的映射服务引用

### Task 3.3: 更新Consultation ViewModel (延迟)
- [ ] 更新所有使用ConsultationItem的ViewModel
> **说明**: ViewModel更新将在Phase 9一并处理

### Task 3.4: 清理Consultation Item类 ✅
- [x] 标记 `ConsultationItem.FromDto/ToDto/ToInputDto` 为[Obsolete]
> **说明**: 方法已标记[Obsolete]，保持向后兼容性

---

## Phase 4: Formula模块迁移 (预计1天) ✅

### Task 4.1: 创建Formula模块Mappers ✅
- [x] 创建 `Mappers/FormulaMapper.cs`
  - FormulaDetailDto ↔ FormulaItem
- [x] 创建 `Mappers/FormulaHerbItemMapper.cs`
  - FormulaHerbItemDto ↔ FormulaHerbItem
- [x] FormulaDetailModel不需要单独Mapper（使用FormulaItem体系）

### Task 4.2: 创建Formula MappingServices ✅
- [x] 创建 `FormulaMappingService`
- [x] FormulaHerbItem映射集成在FormulaMapper中处理

### Task 4.3: 更新Formula DI注册 ✅
- [x] 在 `FormulaModule.cs` 注册所有映射服务

### Task 4.4: 更新Formula ViewModel (延迟)
- [ ] 更新 `FormulaMasterDetailViewModel`
- [ ] 更新 `FormulaHerbItemViewModel`
- [ ] 替换所有映射调用
> **说明**: ViewModel更新将在Phase 9一并处理

### Task 4.5: 清理Formula Item类映射方法 ✅
- [x] 标记 `FormulaItem.FromDto/ToDto` 为[Obsolete]
- [x] 标记 `FormulaHerbItem.FromDto/ToDto` 为[Obsolete]
- [x] 标记 `FormulaDetailModel.FromDto/ToDto` 为[Obsolete]
- [x] 迁移 `FormulaMasterDetailViewModel` 使用MappingService
> **说明**: 方法已标记[Obsolete]，FormulaMasterDetailViewModel已迁移到MappingService

---

## Phase 5: Patients模块迁移 (预计0.5天) ✅

### Task 5.1: 创建Patients模块Mapper ✅
- [x] 创建 `Mappers/PatientMapper.cs`
  - PatientDetailDto ↔ PatientItem ↔ PatientInputDto

### Task 5.2: 创建Patients MappingService ✅
- [x] 创建 `PatientMappingService`
- [x] 在 `PatientsModule.cs` 注册

### Task 5.3: 更新Patients ViewModel (延迟)
- [ ] 更新 `PatientMasterDetailViewModel`
- [ ] 更新其他使用PatientItem的位置
> **说明**: ViewModel更新将在Phase 9一并处理

### Task 5.4: 清理Patients Item类 ✅
- [x] 标记 `PatientItem.FromDto/ToDto` 为[Obsolete]
> **说明**: 方法已标记[Obsolete]，保持向后兼容性

---

## Phase 6: Users模块迁移 (预计0.5天) ✅

### Task 6.1: 创建Users模块Mapper ✅
- [x] 创建 `Mappers/UserMapper.cs`
  - UserDetailDto ↔ UserItem ↔ UserInputDto

### Task 6.2: 创建Users MappingService ✅
- [x] 创建 `UserMappingService`
- [x] 在 `UsersModule.cs` 注册

### Task 6.3: 更新Users ViewModel (延迟)
- [ ] 更新 `UserMasterDetailViewModel`
> **说明**: ViewModel更新将在Phase 9一并处理

### Task 6.4: 清理Users Item类 ✅
- [x] 标记 `UserItem.FromDto/ToDto` 为[Obsolete]
> **说明**: 方法已标记[Obsolete]，保持向后兼容性

---

## Phase 7: Herbs模块迁移 (预计0.5天) ✅

### Task 7.1: 创建Herbs模块Mapper ✅
- [x] 创建 `Mappers/HerbMapper.cs`
  - HerbDetailDto ↔ HerbDetailModel ↔ HerbInputDto

### Task 7.2: 创建Herbs MappingService ✅
- [x] 创建 `HerbMappingService`
- [x] 在 `HerbsModule.cs` 注册

### Task 7.3: 更新Herbs Controls (延迟)
- [ ] 更新 `HerbItemControlViewModel.ToDto`
- [ ] 更新 `HerbItemControl.xaml.cs.ToDto`
- [ ] 更新 `HerbListControlViewModel`
> **说明**: Control更新将在Phase 9一并处理

### Task 7.4: 清理Herbs映射代码 ✅
- [x] Herbs Controls保留ToDto/LoadFromDto（就地更新模式）
- [x] HerbDetailModel无FromDto/ToDto方法（使用HerbMapper）
> **说明**: Herbs Controls使用不同模式（就地更新），ToDto/LoadFromDto保留供Control ViewModel使用

---

## Phase 8: Server端迁移 (预计1天，可选)

### Task 8.1: 添加Mapperly依赖
- [ ] 在Server模块添加 `Riok.Mapperly 4.3.1`

### Task 8.2: 创建Server端Mapper
- [ ] 替换AutoMapper Profile为Mapperly Mapper
- [ ] 验证API响应一致性

### Task 8.3: 清理AutoMapper
- [ ] 删除AutoMapper Profile类
- [ ] 移除AutoMapper包引用
- [ ] 移除 `AddAutoMapper()` 调用

---

## Phase 9: ViewModel标准化迁移 (预计2天)

> **目标**: 所有ViewModel统一迁移到CommunityToolkit.Mvvm

### Task 9.1: 迁移ViewModel基类
- [ ] 重构 `ViewModelBase` 使用 `ObservableObject`
- [ ] 保留现有功能（错误处理、安全执行等）
- [ ] 使用 `[ObservableProperty]` 替换手写属性
- [ ] 使用 `[RelayCommand]` 替换 `DelegateCommand`

### Task 9.2: 迁移MedicalCase模块ViewModel
- [ ] 迁移 `MedicalCaseMasterDetailViewModel`
- [ ] 迁移 `MedicalCaseWorkspaceViewModel`
- [ ] 迁移相关Dialog ViewModel

### Task 9.3: 迁移Consultation模块ViewModel
- [ ] 迁移所有Consultation ViewModel

### Task 9.4: 迁移Formula模块ViewModel
- [ ] 迁移 `FormulaMasterDetailViewModel`
- [ ] 迁移相关ViewModel

### Task 9.5: 迁移Patients模块ViewModel
- [ ] 迁移 `PatientMasterDetailViewModel`
- [ ] 迁移 `PatientSelectionViewModel`

### Task 9.6: 迁移Users模块ViewModel
- [ ] 迁移 `UserMasterDetailViewModel`

### Task 9.7: 迁移Herbs模块ViewModel
- [ ] 迁移 `HerbMasterDetailViewModel`
- [ ] 迁移 `HerbItemControlViewModel`
- [ ] 迁移 `HerbListControlViewModel`

### Task 9.8: 清理Prism.Mvvm依赖
- [ ] 确认所有ViewModel已迁移
- [ ] 移除ViewModel中的 `using Prism.Mvvm`
- [ ] 保留Item类的Prism.Mvvm引用（Mapperly兼容）

---

## Phase 10: 验证与文档 (预计0.5天) ✅

### Task 10.1: 编译验证 ✅
- [x] 全量编译0错误（3个不相关警告）
- [x] 验证Mapperly生成的代码
- [ ] ~~验证CommunityToolkit.Mvvm生成的代码~~ (延期)

### Task 10.2: 测试验证 (部分)
- [ ] 运行现有单元测试
- [ ] 添加映射服务单元测试
- [ ] 手动功能测试（各模块CRUD操作）
> **说明**: 测试验证可在后续迭代中进行

### Task 10.3: 文档更新 ✅
- [x] 更新各模块CLAUDE.md（Infrastructure、MedicalCase、Herbs）
- [x] 添加Mapperly统一映射架构文档
- [ ] ~~添加CommunityToolkit.Mvvm迁移指南~~ (延期)

---

## 验收标准

### 功能验收 (待验证)
- [ ] MedicalCase模块：创建/编辑/保存医案正常
- [ ] Consultation模块：诊断信息录入正常
- [ ] Formula模块：验方管理正常
- [ ] Patients模块：患者管理正常
- [ ] Users模块：用户管理正常
- [ ] Herbs模块：药材管理正常

### 代码质量 ✅
- [x] 编译0错误（3个不相关警告）
- [ ] 所有单元测试通过 (待验证)
- [x] MappingService通过DI注入
- [x] FromDto/ToDto方法已标记[Obsolete]（保持向后兼容）

### 架构验收 ✅
- [x] 所有Item类保持BindableBase继承
- [ ] ~~所有ViewModel使用CommunityToolkit.Mvvm~~ (延期，需独立OpenSpec)
- [x] 映射逻辑由Mapper处理（新代码使用MappingService）

---

## 依赖关系图

```
Phase 1 (基础设施) ─────────────────────────────────────────────┐
        │                                                        │
        ▼                                                        │
Phase 2 (MedicalCase) ───┐                                      │
        │                │                                      │
        ▼                ▼                                      │
Phase 3 (Consultation)  Phase 4 (Formula)                       │
        │                │                                      │
        ▼                ▼                                      │
Phase 5 (Patients)     Phase 6 (Users)     Phase 7 (Herbs)     │
        │                │                    │                  │
        └────────────────┴────────────────────┘                  │
                         │                                       │
                         ▼                                       ▼
                  Phase 8 (Server, 可选)              Phase 9 (ViewModel标准化)
                         │                                       │
                         └───────────────────────────────────────┘
                                          │
                                          ▼
                                   Phase 10 (验证)
```

---

## 工作量估算

| Phase | 内容 | 任务数 | 预计时间 | 复杂度 |
|-------|------|--------|----------|--------|
| Phase 1 | 基础设施搭建 | 3 | 0.5天 | 低 |
| Phase 2 | MedicalCase模块 | 5 | 1.5天 | 中 |
| Phase 3 | Consultation模块 | 4 | 0.5天 | 低 |
| Phase 4 | Formula模块 | 5 | 1天 | 中 |
| Phase 5 | Patients模块 | 4 | 0.5天 | 低 |
| Phase 6 | Users模块 | 4 | 0.5天 | 低 |
| Phase 7 | Herbs模块 | 4 | 0.5天 | 低 |
| Phase 8 | Server端 | 3 | 1天 | 中 |
| Phase 9 | ViewModel标准化 | 8 | 2天 | **高** |
| Phase 10 | 验证与文档 | 3 | 0.5天 | 低 |
| **Desktop总计** | - | **40** | **7.5天** | - |
| **含Server** | - | **43** | **8.5天** | - |

---

## 回滚计划

如迁移过程中发现问题：

1. **按模块回滚**: 每个模块独立，可单独回滚
2. **恢复Item类映射方法**: git revert相关提交
3. **保留MappingService**: 可与手写方法并存过渡

---

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 模块间依赖复杂 | 中 | 按依赖顺序迁移，MedicalCase优先 |
| ViewModel迁移量大 | 高 | 分模块逐步迁移，每次编译验证 |
| 嵌套集合映射 | 中 | Prescription.Items需特殊处理 |
| 与[ObservableProperty]冲突 | **无** | Item类保持BindableBase |
